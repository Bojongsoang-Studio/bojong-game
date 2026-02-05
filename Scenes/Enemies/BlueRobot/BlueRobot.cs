using BojongGame.Scenes.UI;
using Godot;

namespace BojongGame.Scenes.Enemies.BlueRobot;

public partial class BlueRobot : Enemy
{
	private enum State
	{
		Patrol,
		Chase,
		ProjectileAttack,
		MeleeAttack,
		Special,
		Hurt,
		Dead
	}

	[ExportCategory("Movement")] [Export] public float PatrolSpeed = 55f;
	[Export] public float ChaseSpeed = 95f;
	[Export] public float Acceleration = 900f;
	[Export] public float Gravity = 1200f;

	[ExportCategory("Combat")] [Export] public int MaxHealth = 8;
	[Export] public int MeleeDamage = 1;
	[Export] public int ProjectileDamage = 1;

	[Export] public float MeleeCooldown = 0.9f;
	[Export] public float MeleeActiveTime = 0.14f;
	[Export] public float MeleeLockTime = 0.42f;

	[Export] public float ProjectileCooldown = 1.4f;
	[Export] public float ProjectileShootDelay = 0.18f;
	[Export] public float ProjectileLockTime = 0.55f;
	[Export] public float ProjectileRange = 420f;

	[Export] public float SpecialCooldown = 6.0f;
	[Export] public float SpecialDuration = 1.0f;

	[Export] public float HurtTime = 0.22f;
	[Export] public float KnockbackStrength = 320f;

	[Export] public float StopDistance = 18f;
	[Export] public float FacingDeadZone = 8f;

	[ExportCategory("Projectile")] [Export]
	public PackedScene ProjectileScene;

	[Export] public float ProjectileSpeed = 420f;

	private int _direction = 1;

	private State _state = State.Patrol;
	private float _stateTimer;

	private float _meleeCooldown;
	private float _projectileCooldown;
	private float _specialCooldown;

	private bool _shieldActive;
	private bool _projectileFired;

	private AnimatedSprite2D _sprite;
	private Node2D _facing;
	private Area2D _detectionArea;
	private Area2D _meleeAttackArea;
	private RayCast2D _wallRay;
	private RayCast2D _edgeRay;
	private Node2D _projectileSpawn;

	private HealthBar _healthBar;

	private Player.Player _target;

	// SFX
	private AudioStreamPlayer _sfxHit;
	private AudioStreamPlayer _sfxHurt;
	private AudioStreamPlayer _sfxDeath;

	public override void _Ready()
	{
		Health = MaxHealth;

		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_facing = GetNode<Node2D>("Facing");
		_detectionArea = GetNode<Area2D>("Facing/DetectionArea");
		_meleeAttackArea = GetNode<Area2D>("Facing/AttackArea");
		_wallRay = GetNode<RayCast2D>("Facing/WallRay");
		_edgeRay = GetNode<RayCast2D>("Facing/EdgeRay");
		_projectileSpawn = GetNode<Node2D>("Facing/ProjectileSpawn");

		_healthBar = GetNode<HealthBar>("HealthBar");
		_healthBar?.UpdateHealth(Health, MaxHealth);

		// SFX nodes (must exist in the scene)
		_sfxHit = GetNodeOrNull<AudioStreamPlayer>("SfxHit");
		_sfxHurt = GetNodeOrNull<AudioStreamPlayer>("SfxHurt");
		_sfxDeath = GetNodeOrNull<AudioStreamPlayer>("SfxDeath");

		_detectionArea.BodyEntered += OnDetectionBodyEntered;
		_detectionArea.BodyExited += OnDetectionBodyExited;

		_meleeAttackArea.BodyEntered += OnMeleeAttackBodyEntered;
		_meleeAttackArea.Monitoring = false;

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

		_meleeCooldown = Mathf.Max(0f, _meleeCooldown - dt);
		_projectileCooldown = Mathf.Max(0f, _projectileCooldown - dt);
		_specialCooldown = Mathf.Max(0f, _specialCooldown - dt);

		Velocity = new Vector2(Velocity.X, Velocity.Y + Gravity * dt);

		switch (_state)
		{
			case State.Patrol:
				TickPatrol(dt);
				break;
			case State.Chase:
				TickChase(dt);
				break;
			case State.MeleeAttack:
				TickMeleeAttack(dt);
				break;
			case State.ProjectileAttack:
				TickProjectileAttack(dt);
				break;
			case State.Special:
				TickSpecial(dt);
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

		if (IsTargetValid()) SetState(State.Chase);
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

			if (_specialCooldown <= 0f && ShouldUseSpecial())
			{
				TriggerSpecial();
				return;
			}

			if (CanMeleeAttack())
			{
				TriggerMeleeAttack();
			}

			return;
		}

		var vx = Mathf.MoveToward(Velocity.X, ChaseSpeed * _direction, Acceleration * delta);
		Velocity = new Vector2(vx, Velocity.Y);

		PlayAnimation("walk");

		if (_specialCooldown <= 0f && ShouldUseSpecial())
		{
			TriggerSpecial();
			return;
		}

		if (CanMeleeAttack())
		{
			TriggerMeleeAttack();
			return;
		}

		if (CanProjectileAttack())
		{
			TriggerProjectileAttack();
		}
	}

	private void TickMeleeAttack(float delta)
	{
		_stateTimer -= delta;

		Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, Acceleration * delta), Velocity.Y);

		if (_stateTimer <= MeleeLockTime - MeleeActiveTime)
			_meleeAttackArea.Monitoring = false;

		if (_stateTimer > 0f) return;

		_meleeCooldown = MeleeCooldown;
		_meleeAttackArea.Monitoring = false;
		SetState(IsTargetValid() ? State.Chase : State.Patrol);
	}

	private void TickProjectileAttack(float delta)
	{
		_stateTimer -= delta;

		Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, Acceleration * delta), Velocity.Y);

		var timeSpent = ProjectileLockTime - _stateTimer;
		if (!_projectileFired && timeSpent >= ProjectileShootDelay)
		{
			_projectileFired = true;
			ShootProjectile();
		}

		if (_stateTimer > 0f) return;

		_projectileCooldown = ProjectileCooldown;
		SetState(IsTargetValid() ? State.Chase : State.Patrol);
	}

	private void TickSpecial(float delta)
	{
		_stateTimer -= delta;

		Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, Acceleration * delta), Velocity.Y);

		if (_stateTimer > 0f) return;

		_shieldActive = false;
		_specialCooldown = SpecialCooldown;
		SetState(IsTargetValid() ? State.Chase : State.Patrol);
	}

	private void TickHurt(float delta)
	{
		_stateTimer -= delta;
		if (_stateTimer <= 0f)
			SetState(IsTargetValid() ? State.Chase : State.Patrol);
	}

	private void TriggerMeleeAttack()
	{
		_state = State.MeleeAttack;
		_stateTimer = MeleeLockTime;

		PlayAnimation("attack2");
		_meleeAttackArea.Monitoring = true;
	}

	private void TriggerProjectileAttack()
	{
		_state = State.ProjectileAttack;
		_stateTimer = ProjectileLockTime;
		_projectileFired = false;

		PlayAnimation("attack1");
	}

	private void TriggerSpecial()
	{
		_state = State.Special;
		_stateTimer = SpecialDuration;
		_shieldActive = true;

		PlayAnimation("special");
	}

	private void SetState(State state)
	{
		_state = state;

		if (_state == State.Patrol) PlayAnimation("walk");
		if (_state == State.Chase) PlayAnimation("walk");
		if (_state == State.Hurt) PlayAnimation("hurt");
		if (_state == State.Special) PlayAnimation("special");
	}

	private void OnDetectionBodyEntered(Node body)
	{
		if (body is not Player.Player player) return;

		_target = player;

		if (_state != State.MeleeAttack && _state != State.ProjectileAttack && _state != State.Hurt &&
			_state != State.Special)
			SetState(State.Chase);
	}

	private void OnDetectionBodyExited(Node body)
	{
		if (body == _target) _target = null;
	}

	private bool IsTargetValid()
	{
		return _target != null && IsInstanceValid(_target);
	}

	private bool CanMeleeAttack()
	{
		if (!IsTargetValid()) return false;
		return !(_meleeCooldown > 0f);
	}

	private bool CanProjectileAttack()
	{
		if (!IsTargetValid()) return false;
		if (_projectileCooldown > 0f) return false;

		var dx = Mathf.Abs(_target.GlobalPosition.X - GlobalPosition.X);
		return dx <= ProjectileRange;
	}

	private bool ShouldUseSpecial()
	{
		if (!IsTargetValid()) return false;
		var dx = Mathf.Abs(_target.GlobalPosition.X - GlobalPosition.X);
		return dx <= 280f;
	}

	private void OnMeleeAttackBodyEntered(Node body)
	{
		if (_state != State.MeleeAttack) return;
		if (body is not Player.Player player) return;

		player.TakeHit(MeleeDamage, new Vector2(_direction * KnockbackStrength, -KnockbackStrength * 0.35f));

		_sfxHit?.Stop();
		_sfxHit?.Play();
	}

	private void ShootProjectile()
	{
		if (ProjectileScene == null) return;
		if (!IsTargetValid()) return;

		var proj = ProjectileScene.Instantiate<Node2D>();
		GetTree().CurrentScene.AddChild(proj);

		var spawnPos = _projectileSpawn?.GlobalPosition ?? GlobalPosition;
		proj.GlobalPosition = spawnPos;

		if (proj is BlueRobotProjectile projectile) projectile.Setup(_direction, ProjectileSpeed, ProjectileDamage);

		_sfxHit?.Stop();
		_sfxHit?.Play();
	}

	public override void TakeHit(int dmg, Vector2 attackerWorldPos)
	{
		if (_state == State.Dead) return;
		if (_shieldActive) return;

		Health -= dmg;

		_healthBar?.UpdateHealth(Health, MaxHealth);

		if (Health <= 0)
		{
			Die();
			return;
		}

		_sfxHurt?.Stop();
		_sfxHurt?.Play();

		_state = State.Hurt;
		_stateTimer = HurtTime;
		_meleeAttackArea.Monitoring = false;

		PlayAnimation("hurt");

		var knockDir = (GlobalPosition.X - attackerWorldPos.X) >= 0 ? 1f : -1f;
		Velocity = new Vector2(knockDir * KnockbackStrength, -KnockbackStrength * 0.3f);
	}

	private void Die()
	{
		if (_state == State.Dead) return;

		_state = State.Dead;
		Velocity = Vector2.Zero;

		_sfxDeath?.Stop();
		_sfxDeath?.Play();

		PlayAnimation("death");

		_detectionArea.Monitoring = false;
		_meleeAttackArea.Monitoring = false;

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
