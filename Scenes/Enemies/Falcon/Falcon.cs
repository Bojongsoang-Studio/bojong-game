using BojongGame.Scenes.UI;
using Godot;

namespace BojongGame.Scenes.Enemies.Falcon;

public partial class Falcon : Enemy
{
	private enum State
	{
		Patrol,
		Chase,
		Attack,
		Hurt,
		Dead
	}

	[ExportCategory("Movement")]
	[Export] public float PatrolSpeed = 70f;
	[Export] public float ChaseSpeed = 135f;
	[Export] public float Accel = 950f;
	[Export] public float Gravity = 1200f;

	[ExportCategory("Combat")]
	[Export] public int MaxHp = 5;
	[Export] public int Damage = 1;
	[Export] public float AttackCooldown = 0.85f;
	[Export] public float AttackActiveTime = 0.14f;
	[Export] public float AttackLockTime = 0.42f;
	[Export] public float HurtTime = 0.22f;
	[Export] public float KnockbackStrength = 320f;

	[Export] public float StopDistance = 18f;
	[Export] public float FacingDeadZone = 8f;

	private int _hp;
	private int _direction = 1;

	private State _state = State.Patrol;
	private float _stateTimer;
	private float _attackCd;

	private AnimatedSprite2D _anim;
	private Area2D _detectArea;
	private Area2D _attackArea;
	private RayCast2D _wallRay;
	private RayCast2D _edgeRay;

	private HealthBar _healthBar;

	private Player.Player _target;

	public override void _Ready()
	{
		_hp = MaxHp;
		Health = _hp;

		_anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_detectArea = GetNode<Area2D>("Facing/DetectionArea");
		_attackArea = GetNode<Area2D>("Facing/AttackArea");
		_wallRay = GetNodeOrNull<RayCast2D>("Facing/WallRay");
		_edgeRay = GetNodeOrNull<RayCast2D>("Facing/EdgeRay");

		_healthBar = GetNodeOrNull<HealthBar>("HealthBar");
		if (_healthBar != null) _healthBar.UpdateHealth(_hp, MaxHp);

		_detectArea.BodyEntered += OnDetectEntered;
		_detectArea.BodyExited += OnDetectExited;

		_attackArea.BodyEntered += OnAttackAreaBodyEntered;
		_attackArea.Monitoring = false;

		PlayAnimation("idle");
	}

	public override void _PhysicsProcess(double delta)
	{
		var dt = (float)delta;

		if (_state == State.Dead)
		{
			Velocity = new Vector2(0, Velocity.Y + Gravity * dt);
			MoveAndSlide();
			return;
		}

		_attackCd = Mathf.Max(0f, _attackCd - dt);
		Velocity = new Vector2(Velocity.X, Velocity.Y + Gravity * dt);

		switch (_state)
		{
			case State.Patrol:
				TickPatrol(dt);
				break;
			case State.Chase:
				TickChase(dt);
				break;
			case State.Attack:
				TickAttack(dt);
				break;
			case State.Hurt:
				TickHurt(dt);
				break;
		}

		MoveAndSlide();
	}

	private void TickPatrol(float dt)
	{
		if (_wallRay != null && _wallRay.IsColliding()) FlipDirection();
		if (_edgeRay != null && !_edgeRay.IsColliding()) FlipDirection();

		var vx = Mathf.MoveToward(Velocity.X, PatrolSpeed * _direction, Accel * dt);
		Velocity = new Vector2(vx, Velocity.Y);

		PlayAnimation("walk");

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

		var dx = _target.GlobalPosition.X - GlobalPosition.X;

		if (Mathf.Abs(dx) > FacingDeadZone)
		{
			_direction = dx > 0 ? 1 : -1;
			UpdateFacing();
		}

		if (Mathf.Abs(dx) <= StopDistance)
		{
			Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, Accel * dt), Velocity.Y);
			PlayAnimation("idle");

			if (_attackCd <= 0f)
				StartAttack();

			return;
		}

		var vx = Mathf.MoveToward(Velocity.X, ChaseSpeed * _direction, Accel * dt);
		Velocity = new Vector2(vx, Velocity.Y);

		PlayAnimation("walk");
	}

	private void TickAttack(float dt)
	{
		_stateTimer -= dt;

		Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, Accel * dt), Velocity.Y);

		if (_stateTimer <= AttackLockTime - AttackActiveTime)
			_attackArea.Monitoring = false;

		if (_stateTimer > 0f) return;

		_attackCd = AttackCooldown;
		_attackArea.Monitoring = false;
		SetState(IsTargetValid() ? State.Chase : State.Patrol);
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

		PlayAnimation("attack");
		_attackArea.Monitoring = true;
	}

	private void SetState(State s)
	{
		_state = s;

		if (_state == State.Patrol) PlayAnimation("walk");
		if (_state == State.Chase) PlayAnimation("walk");
		if (_state == State.Hurt) PlayAnimation("hurt");
	}

	private void OnDetectEntered(Node body)
	{
		if (body is not Player.Player player) return;

		_target = player;

		if (_state != State.Attack && _state != State.Hurt)
			SetState(State.Chase);
	}

	private void OnDetectExited(Node body)
	{
		if (body == _target) _target = null;
	}

	private bool IsTargetValid()
	{
		return _target != null && IsInstanceValid(_target);
	}

	private void OnAttackAreaBodyEntered(Node body)
	{
		if (_state != State.Attack) return;
		if (body is not Player.Player player) return;

		player.TakeHit(Damage, new Vector2(_direction * KnockbackStrength, -KnockbackStrength * 0.35f));
	}

	public override void TakeHit(int dmg, Vector2 attackerWorldPos)
	{
		if (_state == State.Dead) return;

		_hp -= dmg;
		Health = _hp;

		if (_healthBar != null) _healthBar.UpdateHealth(_hp, MaxHp);

		if (_hp <= 0)
		{
			Die();
			return;
		}

		_state = State.Hurt;
		_stateTimer = HurtTime;
		_attackArea.Monitoring = false;

		PlayAnimation("hurt");

		var knockDir = (GlobalPosition.X - attackerWorldPos.X) >= 0 ? 1f : -1f;
		Velocity = new Vector2(knockDir * KnockbackStrength, -KnockbackStrength * 0.3f);
	}

	private void Die()
	{
		_state = State.Dead;
		Velocity = Vector2.Zero;

		PlayAnimation("death");

		_detectArea.Monitoring = false;
		_attackArea.Monitoring = false;

		if (_healthBar != null) _healthBar.Visible = false;
	}

	private void FlipDirection()
	{
		_direction *= -1;
		UpdateFacing();
	}

	private void UpdateFacing()
	{
		_anim.FlipH = _direction < 0;

		var attackPos = _attackArea.Position;
		attackPos.X = Mathf.Abs(attackPos.X) * _direction;
		_attackArea.Position = attackPos;

		if (_wallRay != null)
		{
			var wallPos = _wallRay.Position;
			wallPos.X = Mathf.Abs(wallPos.X) * _direction;
			_wallRay.Position = wallPos;
		}

		if (_edgeRay != null)
		{
			var edgePos = _edgeRay.Position;
			edgePos.X = Mathf.Abs(edgePos.X) * _direction;
			_edgeRay.Position = edgePos;
		}
	}

	private void PlayAnimation(string name)
	{
		if (_state == State.Dead && name != "death") return;
		if (_anim.Animation == name && _anim.IsPlaying()) return;
		_anim.Play(name);
	}
}
