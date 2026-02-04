using BojongGame.Scenes.UI;
using Godot;

namespace BojongGame.Scenes.Enemies.BlueRobot;

public partial class BlueRobot : Enemy
{
	private enum State
	{
		Patrol,
		Chase,
		AttackProjectile,
		AttackMelee,
		Special,
		Hurt,
		Dead
	}

	[ExportCategory("Movement")]
	[Export] public float PatrolSpeed = 55f;
	[Export] public float ChaseSpeed = 95f;
	[Export] public float Accel = 900f;
	[Export] public float Gravity = 1200f;

	[ExportCategory("Combat")]
	[Export] public int MaxHp = 8;
	[Export] public int DamageMelee = 1;
	[Export] public int DamageProjectile = 1;

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

	[ExportCategory("Projectile")]
	[Export] public PackedScene ProjectileScene;
	[Export] public float ProjectileSpeed = 420f;

	private int _hp;
	private int _direction = 1;

	private State _state = State.Patrol;
	private float _stateTimer;

	private float _meleeCd;
	private float _projCd;
	private float _specialCd;

	private bool _shieldActive;
	private bool _projFired;

	private AnimatedSprite2D _anim;
	private Area2D _detectArea;
	private Area2D _meleeArea;
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
		_meleeArea = GetNode<Area2D>("Facing/AttackArea");
		_wallRay = GetNode<RayCast2D>("Facing/WallRay");
		_edgeRay = GetNodeOrNull<RayCast2D>("Facing/EdgeRay");
		_projectileSpawn = GetNodeOrNull<Node2D>("Facing/ProjectileSpawn");

		_healthBar = GetNodeOrNull<HealthBar>("HealthBar");
		if (_healthBar != null) _healthBar.UpdateHealth(_hp, MaxHp);

		_detectArea.BodyEntered += OnDetectEntered;
		_detectArea.BodyExited += OnDetectExited;

		_meleeArea.BodyEntered += OnMeleeAreaBodyEntered;
		_meleeArea.Monitoring = false;

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

		_meleeCd = Mathf.Max(0f, _meleeCd - dt);
		_projCd = Mathf.Max(0f, _projCd - dt);
		_specialCd = Mathf.Max(0f, _specialCd - dt);

		Velocity = new Vector2(Velocity.X, Velocity.Y + Gravity * dt);

		switch (_state)
		{
			case State.Patrol:
				TickPatrol(dt);
				break;
			case State.Chase:
				TickChase(dt);
				break;
			case State.AttackMelee:
				TickAttackMelee(dt);
				break;
			case State.AttackProjectile:
				TickAttackProjectile(dt);
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

	private void TickPatrol(float dt)
	{
		if (_wallRay != null && _wallRay.IsColliding()) FlipDirection();
		if (_edgeRay != null && !_edgeRay.IsColliding()) FlipDirection();

		var vx = Mathf.MoveToward(Velocity.X, PatrolSpeed * _direction, Accel * dt);
		Velocity = new Vector2(vx, Velocity.Y);

		PlayAnimation("walk");

		if (IsTargetValid()) SetState(State.Chase);
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

			if (_specialCd <= 0f && ShouldUseSpecial())
			{
				StartSpecial();
				return;
			}

			if (CanMelee())
			{
				StartMelee();
				return;
			}

			return;
		}

		var vx = Mathf.MoveToward(Velocity.X, ChaseSpeed * _direction, Accel * dt);
		Velocity = new Vector2(vx, Velocity.Y);

		PlayAnimation("walk");

		if (_specialCd <= 0f && ShouldUseSpecial())
		{
			StartSpecial();
			return;
		}

		if (CanMelee())
		{
			StartMelee();
			return;
		}

		if (CanProjectile())
		{
			StartProjectile();
			return;
		}
	}

	private void TickAttackMelee(float dt)
	{
		_stateTimer -= dt;

		Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, Accel * dt), Velocity.Y);

		if (_stateTimer <= MeleeLockTime - MeleeActiveTime)
			_meleeArea.Monitoring = false;

		if (_stateTimer > 0f) return;

		_meleeCd = MeleeCooldown;
		_meleeArea.Monitoring = false;
		SetState(IsTargetValid() ? State.Chase : State.Patrol);
	}

	private void TickAttackProjectile(float dt)
	{
		_stateTimer -= dt;

		Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, Accel * dt), Velocity.Y);

		var timeSpent = ProjectileLockTime - _stateTimer;
		if (!_projFired && timeSpent >= ProjectileShootDelay)
		{
			_projFired = true;
			ShootProjectile();
		}

		if (_stateTimer > 0f) return;

		_projCd = ProjectileCooldown;
		SetState(IsTargetValid() ? State.Chase : State.Patrol);
	}

	private void TickSpecial(float dt)
	{
		_stateTimer -= dt;

		Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, Accel * dt), Velocity.Y);

		if (_stateTimer > 0f) return;

		_shieldActive = false;
		_specialCd = SpecialCooldown;
		SetState(IsTargetValid() ? State.Chase : State.Patrol);
	}

	private void TickHurt(float dt)
	{
		_stateTimer -= dt;
		if (_stateTimer <= 0f)
			SetState(IsTargetValid() ? State.Chase : State.Patrol);
	}

	private void StartMelee()
	{
		_state = State.AttackMelee;
		_stateTimer = MeleeLockTime;

		PlayAnimation("attack2");
		_meleeArea.Monitoring = true;
	}

	private void StartProjectile()
	{
		_state = State.AttackProjectile;
		_stateTimer = ProjectileLockTime;
		_projFired = false;

		PlayAnimation("attack1");
	}

	private void StartSpecial()
	{
		_state = State.Special;
		_stateTimer = SpecialDuration;
		_shieldActive = true;

		PlayAnimation("special");
	}

	private void SetState(State s)
	{
		_state = s;

		if (_state == State.Patrol) PlayAnimation("walk");
		if (_state == State.Chase) PlayAnimation("walk");
		if (_state == State.Hurt) PlayAnimation("hurt");
		if (_state == State.Special) PlayAnimation("special");
	}

	private void OnDetectEntered(Node body)
	{
		if (body is not Player.Player player) return;

		_target = player;

		if (_state != State.AttackMelee && _state != State.AttackProjectile && _state != State.Hurt && _state != State.Special)
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

	private bool CanMelee()
	{
		if (!IsTargetValid()) return false;
		if (_meleeCd > 0f) return false;
		return true;
	}

	private bool CanProjectile()
	{
		if (!IsTargetValid()) return false;
		if (_projCd > 0f) return false;

		var dx = Mathf.Abs(_target.GlobalPosition.X - GlobalPosition.X);
		return dx <= ProjectileRange;
	}

	private bool ShouldUseSpecial()
	{
		if (!IsTargetValid()) return false;
		var dx = Mathf.Abs(_target.GlobalPosition.X - GlobalPosition.X);
		return dx <= 280f;
	}

	private void OnMeleeAreaBodyEntered(Node body)
	{
		if (_state != State.AttackMelee) return;
		if (body is not Player.Player player) return;

		player.TakeHit(DamageMelee, new Vector2(_direction * KnockbackStrength, -KnockbackStrength * 0.35f));
	}

	private void ShootProjectile()
	{
		if (ProjectileScene == null) return;
		if (!IsTargetValid()) return;

		var proj = ProjectileScene.Instantiate<Node2D>();
		GetTree().CurrentScene.AddChild(proj);

		var spawnPos = _projectileSpawn != null ? _projectileSpawn.GlobalPosition : GlobalPosition;
		proj.GlobalPosition = spawnPos;

		if (proj is BlueRobotProjectile rp)
			rp.Setup(_direction, ProjectileSpeed, DamageProjectile);
	}

	public override void TakeHit(int dmg, Vector2 attackerWorldPos)
	{
		if (_state == State.Dead) return;
		if (_shieldActive) return;

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
		_meleeArea.Monitoring = false;

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
		_meleeArea.Monitoring = false;

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

		var meleePos = _meleeArea.Position;
		meleePos.X = Mathf.Abs(meleePos.X) * _direction;
		_meleeArea.Position = meleePos;

		var wallPos = _wallRay.Position;
		wallPos.X = Mathf.Abs(wallPos.X) * _direction;
		_wallRay.Position = wallPos;

		if (_edgeRay != null)
		{
			var edgePos = _edgeRay.Position;
			edgePos.X = Mathf.Abs(edgePos.X) * _direction;
			_edgeRay.Position = edgePos;
		}

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
