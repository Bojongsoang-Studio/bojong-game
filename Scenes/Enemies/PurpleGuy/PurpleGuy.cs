using BojongGame.Scenes.UI;
using Godot;

namespace BojongGame.Scenes.Enemies.PurpleGuy;

public partial class PurpleGuy : Enemy
{
	private enum State
	{
		Patrol,
		Shoot,
		Hurt,
		Dead
	}

	[ExportCategory("Movement")] [Export] public float PatrolSpeed = 45f;
	[Export] public float Acceleration = 900f;
	[Export] public float Gravity = 1200f;

	[ExportCategory("Combat")] [Export] public int MaxHealth = 8;
	[Export] public int ProjectileDamage = 1;

	[Export] public float ShootCooldown = 1.2f;
	[Export] public float ShootDelay = 0.12f;
	[Export] public float ShootLockTime = 0.45f;
	[Export] public float ProjectileRange = 520f;

	[Export] public float HurtTime = 0.22f;
	[Export] public float KnockbackStrength = 320f;

	[Export] public float FacingDeadZone = 8f;

	[ExportCategory("Projectile")] [Export]
	public PackedScene ProjectileScene;

	[Export] public float ProjectileSpeed = 520f;

	private int _direction = 1;

	private State _state = State.Patrol;
	private float _stateTimer;

	private float _shootCooldown;
	private bool _shotFired;

	private AnimatedSprite2D _sprite;
	private Node2D _facing;
	private Area2D _detectionArea;
	private RayCast2D _wallRay;
	private RayCast2D _edgeRay;
	private Node2D _projectileSpawn;

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
		_wallRay = GetNode<RayCast2D>("Facing/WallRay");
		_edgeRay = GetNode<RayCast2D>("Facing/EdgeRay");
		_projectileSpawn = GetNode<Node2D>("Facing/ProjectileSpawn");

		_healthBar = GetNodeOrNull<HealthBar>("HealthBar");
		_healthBar?.UpdateHealth(Health, MaxHealth);

		_sfxHit = GetNodeOrNull<AudioStreamPlayer>("SfxHit");
		_sfxHurt = GetNodeOrNull<AudioStreamPlayer>("SfxHurt");
		_sfxDeath = GetNodeOrNull<AudioStreamPlayer>("SfxDeath");

		_detectionArea.BodyEntered += OnDetectionBodyEntered;
		_detectionArea.BodyExited += OnDetectionBodyExited;

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

		_shootCooldown = Mathf.Max(0f, _shootCooldown - dt);
		Velocity = new Vector2(Velocity.X, Velocity.Y + Gravity * dt);

		switch (_state)
		{
			case State.Patrol:
				TickPatrol(dt);
				break;
			case State.Shoot:
				TickShoot(dt);
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

		if (CanShoot())
		{
			TriggerShoot();
			return;
		}

		if (!IsTargetValid()) return;
		var dx = _target.GlobalPosition.X - GlobalPosition.X;

		if (!(Mathf.Abs(dx) > FacingDeadZone)) return;
		_direction = dx > 0 ? 1 : -1;
		UpdateFacing();
	}

	private void TickShoot(float delta)
	{
		_stateTimer -= delta;

		Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, Acceleration * delta), Velocity.Y);

		var timeSpent = ShootLockTime - _stateTimer;
		if (!_shotFired && timeSpent >= ShootDelay)
		{
			_shotFired = true;
			ShootProjectile();
		}

		if (_stateTimer > 0f) return;

		_shootCooldown = ShootCooldown;
		SetState(State.Patrol);
	}

	private void TickHurt(float delta)
	{
		_stateTimer -= delta;
		if (_stateTimer <= 0f)
			SetState(State.Patrol);
	}

	private void TriggerShoot()
	{
		_state = State.Shoot;
		_stateTimer = ShootLockTime;
		_shotFired = false;

		PlayAnimation("attack");
	}

	private void SetState(State state)
	{
		_state = state;

		if (_state == State.Patrol) PlayAnimation("walk");
		if (_state == State.Hurt) PlayAnimation("hurt");
	}

	private void OnDetectionBodyEntered(Node body)
	{
		if (body is not Player.Player player) return;
		_target = player;
	}

	private void OnDetectionBodyExited(Node body)
	{
		if (body == _target) _target = null;
	}

	private bool IsTargetValid()
	{
		return _target != null && IsInstanceValid(_target);
	}

	private bool CanShoot()
	{
		if (!IsTargetValid()) return false;
		if (_shootCooldown > 0f) return false;

		var dx = Mathf.Abs(_target.GlobalPosition.X - GlobalPosition.X);
		return dx <= ProjectileRange;
	}

	private void ShootProjectile()
	{
		if (ProjectileScene == null) return;
		if (!IsTargetValid()) return;

		var dx = _target.GlobalPosition.X - GlobalPosition.X;

		if (Mathf.Abs(dx) > FacingDeadZone)
		{
			_direction = dx > 0 ? 1 : -1;
			UpdateFacing();
		}

		var proj = ProjectileScene.Instantiate<Node2D>();
		GetTree().CurrentScene.AddChild(proj);

		var spawnPos = _projectileSpawn?.GlobalPosition ?? GlobalPosition;
		proj.GlobalPosition = spawnPos;

		if (proj is PurpleGuyProjectile projectile) projectile.Setup(_direction, ProjectileSpeed, ProjectileDamage);

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

		// SFX hurt (ADDED)
		_sfxHurt?.Stop();
		_sfxHurt?.Play();

		_state = State.Hurt;
		_stateTimer = HurtTime;

		PlayAnimation("hurt");

		var knockDir = (GlobalPosition.X - attackerWorldPos.X) >= 0 ? 1f : -1f;
		Velocity = new Vector2(knockDir * KnockbackStrength, -KnockbackStrength * 0.3f);
	}

	private void Die()
	{
		_state = State.Dead;
		Velocity = Vector2.Zero;

		_sfxDeath?.Stop();
		_sfxDeath?.Play();

		PlayAnimation("death");

		_detectionArea.Monitoring = false;

		if (_healthBar != null)
			_healthBar.Visible = false;
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
