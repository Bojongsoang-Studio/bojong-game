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
	[Export] public float Acceleration = 950f;
	[Export] public float Gravity = 1200f;

	[ExportCategory("Combat")]
	[Export] public int MaxHealth = 5;
	[Export] public int Damage = 1;
	[Export] public float AttackCooldown = 0.85f;
	[Export] public float AttackActiveTime = 0.14f;
	[Export] public float AttackLockTime = 0.42f;
	[Export] public float HurtTime = 0.22f;
	[Export] public float KnockbackStrength = 320f;

	[Export] public float StopDistance = 18f;
	[Export] public float FacingDeadZone = 8f;

	private int _direction = 1;

	private State _state = State.Patrol;
	private float _stateTimer;
	private float _attackCooldown;

	private AnimatedSprite2D _sprite;
	private Node2D _facing;
	private Area2D _detectionArea;
	private Area2D _attackArea;
	private RayCast2D _wallRay;
	private RayCast2D _edgeRay;

	private HealthBar _healthBar;

	private Player.Player _target;

	private AudioStreamPlayer _sfxHit;
	private AudioStreamPlayer _sfxHurt;
	private AudioStreamPlayer _sfxDeath;

	public override void _Ready()
	{
		Health = MaxHealth;

		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_facing = GetNode<Node2D>("Facing");
		_detectionArea = GetNode<Area2D>("Facing/DetectionArea");
		_attackArea = GetNode<Area2D>("Facing/AttackArea");
		_wallRay = GetNode<RayCast2D>("Facing/WallRay");
		_edgeRay = GetNode<RayCast2D>("Facing/EdgeRay");

		_healthBar = GetNode<HealthBar>("HealthBar");
		_healthBar?.UpdateHealth(Health, MaxHealth);

		_sfxHit = GetNode<AudioStreamPlayer>("SfxHit");
		_sfxHurt = GetNode<AudioStreamPlayer>("SfxHurt");
		_sfxDeath = GetNode<AudioStreamPlayer>("SfxDeath");

		_detectionArea.BodyEntered += OnDetectBodyEntered;
		_detectionArea.BodyExited += OnDetectBodyExited;

		_attackArea.BodyEntered += OnAttackBodyEntered;
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

		_attackCooldown = Mathf.Max(0f, _attackCooldown - dt);
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

	private void TickPatrol(float delta)
	{
		if (_wallRay != null && _wallRay.IsColliding()) FlipDirection();
		if (_edgeRay != null && !_edgeRay.IsColliding()) FlipDirection();

		var vx = Mathf.MoveToward(Velocity.X, PatrolSpeed * _direction, Acceleration * delta);
		Velocity = new Vector2(vx, Velocity.Y);

		PlayAnimation("walk");

		if (IsTargetValid())
			SetState(State.Chase);
	}

	private void TickChase(float delta)
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
			Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, Acceleration * delta), Velocity.Y);
			PlayAnimation("idle");

			if (_attackCooldown <= 0f)
				TriggerAttack();

			return;
		}

		var vx = Mathf.MoveToward(Velocity.X, ChaseSpeed * _direction, Acceleration * delta);
		Velocity = new Vector2(vx, Velocity.Y);

		PlayAnimation("walk");
	}

	private void TickAttack(float delta)
	{
		_stateTimer -= delta;

		Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, Acceleration * delta), Velocity.Y);

		if (_stateTimer <= AttackLockTime - AttackActiveTime)
			_attackArea.Monitoring = false;

		if (_stateTimer > 0f) return;

		_attackCooldown = AttackCooldown;
		_attackArea.Monitoring = false;
		SetState(IsTargetValid() ? State.Chase : State.Patrol);
	}

	private void TickHurt(float delta)
	{
		_stateTimer -= delta;
		if (_stateTimer <= 0f)
			SetState(IsTargetValid() ? State.Chase : State.Patrol);
	}

	private void TriggerAttack()
	{
		_state = State.Attack;
		_stateTimer = AttackLockTime;

		PlayAnimation("attack");
		_attackArea.Monitoring = true;
	}

	private void SetState(State state)
	{
		_state = state;

		if (_state == State.Patrol) PlayAnimation("walk");
		if (_state == State.Chase) PlayAnimation("walk");
		if (_state == State.Hurt) PlayAnimation("hurt");
	}

	private void OnDetectBodyEntered(Node body)
	{
		if (body is not Player.Player player) return;

		_target = player;

		if (_state != State.Attack && _state != State.Hurt)
			SetState(State.Chase);
	}

	private void OnDetectBodyExited(Node body)
	{
		if (body == _target) _target = null;
	}

	private bool IsTargetValid()
	{
		return _target != null && IsInstanceValid(_target);
	}

	private void OnAttackBodyEntered(Node body)
	{
		if (_state != State.Attack) return;
		if (body is not Player.Player player) return;

		player.TakeHit(Damage, new Vector2(_direction * KnockbackStrength, -KnockbackStrength * 0.35f));

		// SFX Hit (ADDED)
		_sfxHit?.Stop();
		_sfxHit?.Play();
	}

	public override void TakeHit(int dmg, Vector2 attackerWorldPos)
	{
		if (_state == State.Dead) return;

		Health -= dmg;

		_healthBar?.UpdateHealth(Health, MaxHealth);

		if (Health <= 0)
		{
			Die();
			return;
		}

		// SFX Hurt (ADDED)
		_sfxHurt?.Stop();
		_sfxHurt?.Play();

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

		// SFX Death (ADDED)
		_sfxDeath?.Stop();
		_sfxDeath?.Play();

		PlayAnimation("death");

		_detectionArea.Monitoring = false;
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
		_sprite.FlipH = _direction < 0;
		if (_direction != 0) _facing.Scale = new Vector2(_direction, 1f);
	}

	private void PlayAnimation(string name)
	{
		if (_state == State.Dead && name != "death") return;
		if (_sprite.Animation == name && _sprite.IsPlaying()) return;
		_sprite.Play(name);
	}
}
