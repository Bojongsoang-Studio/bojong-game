using BojongGame.Scenes.UI;
using Godot;

namespace BojongGame.Scenes.Enemies.PurpleGuyAlt;

public partial class PurpleGuyAlt : Enemy
{
	private enum State
	{
		Patrol,
		Shoot,
		Hurt,
		Dead
	}

	[ExportCategory("Movement")]
	[Export] public float PatrolSpeed = 45f;
	[Export] public float Accel = 900f;
	[Export] public float Gravity = 1200f;

	[ExportCategory("Combat")]
	[Export] public int MaxHp = 8;
	[Export] public int DamageProjectile = 1;

	[Export] public float ShootCooldown = 1.2f;
	[Export] public float ShootDelay = 0.12f;
	[Export] public float ShootLockTime = 0.45f;
	[Export] public float ProjectileRange = 520f;

	[Export] public float HurtTime = 0.22f;
	[Export] public float KnockbackStrength = 320f;

	[Export] public float FacingDeadZone = 8f;

	[ExportCategory("Projectile")]
	[Export] public PackedScene ProjectileScene;
	[Export] public float ProjectileSpeed = 520f;

	private int _hp;
	private int _direction = 1;

	private State _state = State.Patrol;
	private float _stateTimer;

	private float _shootCd;
	private bool _shotFired;

	private AnimatedSprite2D _anim;
	private Area2D _detectArea;
	private RayCast2D _wallRay;
	private RayCast2D _edgeRay;
	private Node2D _projectileSpawn;

	private HealthBar _healthBar;
	private Player.Player _target;

	public override void _Ready()
	{
		_hp = MaxHp;
		Health = _hp;

		_anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_detectArea = GetNode<Area2D>("Facing/DetectionArea");
		_wallRay = GetNodeOrNull<RayCast2D>("Facing/WallRay");
		_edgeRay = GetNodeOrNull<RayCast2D>("Facing/EdgeRay");
		_projectileSpawn = GetNodeOrNull<Node2D>("Facing/ProjectileSpawn");

		_healthBar = GetNodeOrNull<HealthBar>("HealthBar");
		if (_healthBar != null) _healthBar.UpdateHealth(_hp, MaxHp);

		_detectArea.BodyEntered += OnDetectEntered;
		_detectArea.BodyExited += OnDetectExited;

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

		_shootCd = Mathf.Max(0f, _shootCd - dt);
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

	private void TickPatrol(float dt)
	{
		if (_wallRay != null && _wallRay.IsColliding()) FlipDirection();
		if (_edgeRay != null && !_edgeRay.IsColliding()) FlipDirection();

		var vx = Mathf.MoveToward(Velocity.X, PatrolSpeed * _direction, Accel * dt);
		Velocity = new Vector2(vx, Velocity.Y);

		PlayAnimation("walk");

		if (CanShoot())
		{
			StartShoot();
			return;
		}

		if (IsTargetValid())
		{
			var dx = _target.GlobalPosition.X - GlobalPosition.X;

			if (Mathf.Abs(dx) > FacingDeadZone)
			{
				_direction = dx > 0 ? 1 : -1;
				UpdateFacing();
			}
		}
	}

	private void TickShoot(float dt)
	{
		_stateTimer -= dt;

		Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, Accel * dt), Velocity.Y);

		var timeSpent = ShootLockTime - _stateTimer;
		if (!_shotFired && timeSpent >= ShootDelay)
		{
			_shotFired = true;
			ShootProjectile();
		}

		if (_stateTimer > 0f) return;

		_shootCd = ShootCooldown;
		SetState(State.Patrol);
	}

	private void TickHurt(float dt)
	{
		_stateTimer -= dt;
		if (_stateTimer <= 0f)
			SetState(State.Patrol);
	}

	private void StartShoot()
	{
		_state = State.Shoot;
		_stateTimer = ShootLockTime;
		_shotFired = false;

		PlayAnimation("attack");
	}

	private void SetState(State s)
	{
		_state = s;

		if (_state == State.Patrol) PlayAnimation("walk");
		if (_state == State.Hurt) PlayAnimation("hurt");
	}

	private void OnDetectEntered(Node body)
	{
		if (body is not Player.Player player) return;
		_target = player;
	}

	private void OnDetectExited(Node body)
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
		if (_shootCd > 0f) return false;

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

		var spawnPos = _projectileSpawn != null
			? _projectileSpawn.GlobalPosition
			: GlobalPosition;

		proj.GlobalPosition = spawnPos;

		if (proj is PurpleGuyProjectile pp)
			pp.Setup(_direction, ProjectileSpeed, DamageProjectile);
	}

	public override void TakeHit(int dmg, Vector2 attackerWorldPos)
	{
		if (_state == State.Dead) return;

		_hp -= dmg;
		Health = _hp;

		if (_healthBar != null)
			_healthBar.UpdateHealth(_hp, MaxHp);

		if (_hp <= 0)
		{
			Die();
			return;
		}

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

		PlayAnimation("death");

		_detectArea.Monitoring = false;

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
		_anim.FlipH = _direction < 0;

		if (_projectileSpawn != null)
		{
			var sp = _projectileSpawn.Position;
			sp.X = Mathf.Abs(sp.X) * _direction;
			_projectileSpawn.Position = sp;
		}
	}

	private void PlayAnimation(string name)
	{
		if (_state == State.Dead && name != "death") return;
		if (_anim.Animation == name && _anim.IsPlaying()) return;
		_anim.Play(name);
	}
}
