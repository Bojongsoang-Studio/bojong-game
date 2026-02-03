using Godot;
using System;

public partial class DogEnemy : CharacterBody2D
{
	private enum State { Patrol, Chase, Attack, Hurt, Dead }

	[ExportCategory("Movement")]
	[Export] public float PatrolSpeed = 65f;
	[Export] public float ChaseSpeed = 120f;
	[Export] public float Accel = 900f;
	[Export] public float Gravity = 1200f;

	[ExportCategory("Combat")]
	[Export] public int MaxHp = 5; // takes 4-5 hits
	[Export] public int Damage = 1;
	[Export] public float AttackCooldown = 0.9f;
	[Export] public float AttackActiveTime = 0.12f; // bite hitbox ON time
	[Export] public float AttackLockTime = 0.35f;   // total attack state time
	[Export] public float HurtTime = 0.22f;
	[Export] public float KnockbackStrength = 140f;

	private int _hp;
	private int _dir = 1;

	private State _state = State.Patrol;
	private float _stateTimer = 0f;
	private float _attackCd = 0f;

	private AnimatedSprite2D _anim;
	private Area2D _detectArea;
	private Area2D _attackArea;
	private RayCast2D _wallRay;
	private RayCast2D _edgeRay;

	private Node2D _target;

	public override void _Ready()
	{
		_hp = MaxHp;

		_anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_detectArea = GetNode<Area2D>("DetectionArea");
		_attackArea = GetNode<Area2D>("AttackArea");
		_wallRay = GetNode<RayCast2D>("WallRay");
		_edgeRay = GetNodeOrNull<RayCast2D>("EdgeRay");

		// Detection uses signals (reliable)
		_detectArea.BodyEntered += OnDetectEntered;
		_detectArea.BodyExited += OnDetectExited;

		// Attack hit uses signals (reliable)
		_attackArea.BodyEntered += OnAttackAreaBodyEntered;

		// Attack area off by default, turned on briefly when attacking
		_attackArea.Monitoring = false;

		PlayAnim("run"); // patrol uses run since you don't have walk
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		if (_state == State.Dead)
		{
			Velocity = new Vector2(0, Velocity.Y + Gravity * dt);
			MoveAndSlide();
			return;
		}

		_attackCd = Mathf.Max(0f, _attackCd - dt);

		// gravity
		Velocity = new Vector2(Velocity.X, Velocity.Y + Gravity * dt);

		switch (_state)
		{
			case State.Patrol: TickPatrol(dt); break;
			case State.Chase:  TickChase(dt);  break;
			case State.Attack: TickAttack(dt); break;
			case State.Hurt:   TickHurt(dt);   break;
		}

		MoveAndSlide();
	}

	private void TickPatrol(float dt)
	{
		// wall flip
		if (_wallRay.IsColliding())
			FlipDir();

		// edge flip (optional)
		if (_edgeRay != null && !_edgeRay.IsColliding())
			FlipDir();

		float vx = Mathf.MoveToward(Velocity.X, PatrolSpeed * _dir, Accel * dt);
		Velocity = new Vector2(vx, Velocity.Y);

		PlayAnim("run");

		if (IsTargetValid())
			SetState(State.Chase);
	}

	private void TickChase(float dt)
	{
		if (!IsTargetValid())
		{
			SetState(State.Patrol);
			return;
		}

		float dx = _target.GlobalPosition.X - GlobalPosition.X;
		_dir = dx >= 0 ? 1 : -1;
		UpdateFacing();

		float vx = Mathf.MoveToward(Velocity.X, ChaseSpeed * _dir, Accel * dt);
		Velocity = new Vector2(vx, Velocity.Y);

		PlayAnim("run");

		// start attack if close enough (uses overlap bodies)
		if (_attackCd <= 0f && IsTargetInAttackArea())
			StartAttack();
	}

	private void TickAttack(float dt)
	{
		_stateTimer -= dt;

		// keep moving minimal during attack
		Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, Accel * dt), Velocity.Y);

		// turn off hitbox after active time
		// AttackActiveTime is only a small window inside AttackLockTime
		if (_stateTimer <= (AttackLockTime - AttackActiveTime))
			_attackArea.Monitoring = false;

		if (_stateTimer <= 0f)
		{
			_attackCd = AttackCooldown;
			_attackArea.Monitoring = false;
			SetState(IsTargetValid() ? State.Chase : State.Patrol);
		}
	}

	private void TickHurt(float dt)
	{
		_stateTimer -= dt;
		if (_stateTimer <= 0f)
			SetState(IsTargetValid() ? State.Chase : State.Patrol);
	}

	private void StartAttack()
	{
		_state = State.Attack;
		_stateTimer = AttackLockTime;

		PlayAnim("attack");

		// Turn on bite hitbox briefly
		_attackArea.Monitoring = true;

		// Also ensure player already inside gets hit (optional)
		TryDamageTargetInAttackArea();
	}

	private void SetState(State s)
	{
		_state = s;

		if (_state == State.Patrol) PlayAnim("run");
		if (_state == State.Chase)  PlayAnim("run");
		if (_state == State.Hurt)   PlayAnim("hurt");
	}

	private void OnDetectEntered(Node body)
	{
		if (body.IsInGroup("player"))
		{
			_target = body as Node2D;
			if (_state != State.Attack && _state != State.Hurt)
				SetState(State.Chase);
		}
	}

	private void OnDetectExited(Node body)
	{
		if (body == _target)
			_target = null;
	}

	private bool IsTargetValid()
	{
		return _target != null && IsInstanceValid(_target);
	}

	private bool IsTargetInAttackArea()
	{
		foreach (var body in _attackArea.GetOverlappingBodies())
		{
			if (body is Node n && n.IsInGroup("player"))
				return true;
		}
		return false;
	}

	private void OnAttackAreaBodyEntered(Node body)
	{
		// only deal damage while actively attacking
		if (_state != State.Attack) return;

		if (body is Node n && n.IsInGroup("player"))
		{
			Vector2 kb = new Vector2(_dir * KnockbackStrength, -KnockbackStrength * 0.35f);
			if (n.HasMethod("TakeDamage"))
				n.Call("TakeDamage", Damage, kb);
		}
	}

	private void TryDamageTargetInAttackArea()
	{
		foreach (var body in _attackArea.GetOverlappingBodies())
		{
			if (body is Node n && n.IsInGroup("player"))
			{
				Vector2 kb = new Vector2(_dir * KnockbackStrength, -KnockbackStrength * 0.35f);
				if (n.HasMethod("TakeDamage"))
					n.Call("TakeDamage", Damage, kb);
				return;
			}
		}
	}

	public void TakeHit(int dmg, Vector2 attackerWorldPos)
	{
		if (_state == State.Dead) return;

		_hp -= dmg;

		if (_hp <= 0)
		{
			Die();
			return;
		}

		// Hurt knockback
		_state = State.Hurt;
		_stateTimer = HurtTime;

		float kdir = (GlobalPosition.X - attackerWorldPos.X) >= 0 ? 1f : -1f;
		Velocity = new Vector2(kdir * KnockbackStrength, -KnockbackStrength * 0.3f);

		PlayAnim("hurt");
	}

	private void Die()
	{
		_state = State.Dead;
		Velocity = Vector2.Zero;

		PlayAnim("death");

		// Disable combat/detection
		_detectArea.Monitoring = false;
		_attackArea.Monitoring = false;
	}

	private void FlipDir()
	{
		_dir *= -1;
		UpdateFacing();
	}

	private void UpdateFacing()
	{
		_anim.FlipH = _dir < 0;

		// keep attack area in front
		var ap = _attackArea.Position;
		ap.X = Mathf.Abs(ap.X) * _dir;
		_attackArea.Position = ap;

		// move ray origins so they stay in front
		var wp = _wallRay.Position;
		wp.X = Mathf.Abs(wp.X) * _dir;
		_wallRay.Position = wp;

		if (_edgeRay != null)
		{
			var ep = _edgeRay.Position;
			ep.X = Mathf.Abs(ep.X) * _dir;
			_edgeRay.Position = ep;
		}
	}

	private void PlayAnim(string name)
	{
		if (_state == State.Dead && name != "death") return;

		if (_anim.Animation == name && _anim.IsPlaying())
			return;

		_anim.Play(name);
	}
}
