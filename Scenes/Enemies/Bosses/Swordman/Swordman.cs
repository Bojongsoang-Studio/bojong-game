using System.Threading.Tasks;
using BojongGame.Scenes.UI;
using Godot;

namespace BojongGame.Scenes.Enemies.Bosses.Swordman;

public partial class Swordman : Enemy
{
	private enum State
	{
		Idle,
		Chase,
		Attack,
		Hurt,
		PhaseTransition,
		Blitz,
		Dead
	}

	[ExportCategory("Health / Phases")]
	[Export] public int MaxHealth = 30;
	[Export] public float Phase2HealthPercent = 0.66f;
	[Export] public float Phase3HealthPercent = 0.33f;

	[ExportCategory("Movement")]
	[Export] public float Gravity = 1200f;
	[Export] public float Accel = 900f;
	[Export] public float Phase1Speed = 130f;
	[Export] public float Phase2Speed = 200f;
	[Export] public float Phase3Speed = 260f;

	[ExportCategory("Melee")]
	[Export] public int Phase1Damage = 1;
	[Export] public int Phase2Damage = 2;
	[Export] public int Phase3Damage = 3;

	[Export] public float Phase1Cooldown = 1.10f;
	[Export] public float Phase2Cooldown = 0.85f;
	[Export] public float Phase3Cooldown = 0.70f;

	[Export] public float AttackLockTime = 0.50f;
	[Export] public float AttackActiveTime = 0.12f;

	[ExportCategory("Hurt / Anti-Stunlock")]
	[Export] public float HurtTime = 0.22f;
	[Export] public float KnockbackStrength = 320f;
	[Export] public int DamageStreakLimit = 6;
	[Export] public float EscapeJumpVelocity = -520f;
	
	[ExportCategory("New: Leap / Escape")]
	[Export] public bool LeapToPlayerWhenAirborne = true;
	[Export] public float LeapCooldown = 1.0f;
	[Export] public float LeapJumpVelocity = -520f;
	[Export] public float LeapHorizontalBoost = 520f;
	[Export] public float LeapMaxDistanceX = 520f;	
	
	[Export] public float LeapMinHeightDelta = 20f;   
	[Export] public float LeapMaxHeightDelta = 300f;  
	[Export] public float LeapMinDistanceX = 0f;
	
	[ExportCategory("Drop")]
	[Export] public bool DropToPlayerWhenBelow = true;
	[Export] public float DropCooldown = 1.0f;
	[Export] public float DropVelocity = 150f;
	[Export] public float DropHorizontalBoost = 320f;
	[Export] public float DropMaxDistanceX = 320f;	
	
	[Export] public float DropMinHeightDelta = 60f;   
	[Export] public float DropMaxHeightDelta = 300f;  
	[Export] public float DropMinDistanceX = 0f;     


	[Export] public int ConsecutiveHitsToEscape = 3; 
	[Export] public float ConsecutiveHitWindow = 1.2f; 
	[Export] public float EscapeHorizontalBoost = 520f;


	[ExportCategory("Phase 2: Combo")]
	[Export] public bool Phase2EnableCombo = true;
	[Export] public float ComboGapSeconds = 0.10f;
	[Export] public int ComboFirstAttack = 1;
	[Export] public int ComboSecondAttack = 2;
	[Export] public float ComboChance = 0.65f;

	[ExportCategory("Phase 3: Blitz")]
	[Export] public float BlitzTelegraph = 0.35f;
	[Export] public float BlitzSpeed = 1600f;
	[Export] public float BlitzDuration = 0.22f;
	[Export] public float BlitzChance = 0.35f;
	[Export] public float BlitzExtraCooldown = 1.2f;

	[ExportCategory("Phase 3: Projectile")]
	[Export] public PackedScene ProjectileScene;
	[Export] public NodePath ProjectileSpawnPath;
	[Export] public float ProjectileCooldown = 1.6f;
	[Export] public float ProjectileMinDistanceX = 220f;
	[Export] public float ProjectileChancePerCheck = 0.35f;
	[Export] public float ProjectileCheckInterval = 0.25f;
	[Export] public float ProjectileSpeed = 520f;
	[Export] public int ProjectileDamage = 1;

	[ExportCategory("Phase Transition")]
	[Export] public float TransitionFreezeSeconds = 0.25f;
	[Export] public float TransitionPostSeconds = 0.20f;

	private State _state = State.Idle;

	private int _phase = 1;
	private int _direction = 1;

	private float _stateTimer;
	private float _attackCooldown;
	private float _projectileCooldown;
	private float _projectileCheckTimer;
	
	private float _leapCooldown;
	private float _dropCooldown;
	private int _consecutiveHits;
	private float _consecutiveHitTimer;
	private bool _isPhasing = false;
	private float _phaseTimer = 0f;


	private int _damageStreak;
	private bool _inAttackRange;
	private bool _canHit;

	private bool _invincible;
	private bool _transitioning;

	private Player.Player _player;

	private AnimatedSprite2D _sprite;
	private Area2D _detector;
	private Area2D _attackRange;
	private Area2D _hitbox;
	private Node2D _healthBar;
	private Node2D _projectileSpawn;

	private readonly RandomNumberGenerator _rng = new();

	public override void _Ready()
	{
		_rng.Randomize();

		Health = MaxHealth;

		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_detector = GetNode<Area2D>("PlayerDetector");
		_attackRange = GetNode<Area2D>("AttackRange");
		_hitbox = GetNode<Area2D>("Hitbox");
		_healthBar = GetNode<Node2D>("HealthBar");

		_projectileSpawn = null;
		if (ProjectileSpawnPath != null && !ProjectileSpawnPath.IsEmpty)
			_projectileSpawn = GetNodeOrNull<Node2D>(ProjectileSpawnPath);

		_projectileSpawn ??= this;

		_detector.BodyEntered += OnPlayerDetected;
		_detector.BodyExited += OnPlayerLost;

		_attackRange.BodyEntered += OnRangeBodyEntered;
		_attackRange.BodyExited += OnRangeBodyExited;

		_hitbox.BodyEntered += OnHitboxBodyEntered;
		_hitbox.Monitoring = false;

		_sprite.Play("idle");
	}

	public override void _PhysicsProcess(double delta)
	{
		var dt = (float)delta;

		if (_state == State.Dead) return;

		_attackCooldown = Mathf.Max(0, _attackCooldown - dt);
		_projectileCooldown = Mathf.Max(0, _projectileCooldown - dt);
		_projectileCheckTimer = Mathf.Max(0, _projectileCheckTimer - dt);

		_leapCooldown = Mathf.Max(0, _leapCooldown - dt);
		_dropCooldown = Mathf.Max(0, _dropCooldown - dt);
		
		if (_isPhasing)
		{
			_phaseTimer -= (float)delta;
			if (_phaseTimer <= 0)
			{
				_isPhasing = false;
				SetCollisionMaskValue(5, true); // Turn it back on after clearing the floor
			}
		}

		_consecutiveHitTimer = Mathf.Max(0, _consecutiveHitTimer - dt);
		if (_consecutiveHitTimer <= 0f)
			_consecutiveHits = 0;
		
		if (_state != State.Blitz)
			Velocity += new Vector2(0, Gravity * dt);

		switch (_state)
		{
			case State.Idle:
				if (_player != null && !_transitioning)
					_state = State.Chase;
				else
					Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, Accel * dt), Velocity.Y);
				break;

			case State.Chase:
				Chase(dt);
				break;

			case State.Attack:
				TickAttack(dt);
				break;

			case State.Blitz:
				TickBlitz(dt);
				break;

			case State.Hurt:
				TickHurt(dt);
				break;

			case State.PhaseTransition:
				Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, Accel * dt), Velocity.Y);
				break;
		}

		MoveAndSlide();
	}

	private void Chase(float delta)
	{
		if (_player == null || _transitioning)
		{
			_state = State.Idle;
			return;
		}

		var dx = _player.GlobalPosition.X - GlobalPosition.X;
		_direction = dx >= 0 ? 1 : -1;
		_sprite.FlipH = _direction < 0;
		if (_direction != 0) _hitbox.Scale = new Vector2(_direction, 1f);
		
		if (LeapToPlayerWhenAirborne) TryLeapToPlayer(dx);
		if (DropToPlayerWhenBelow) TryDropToPlayer(dx);

		var speed = GetSpeed();
		var vx = Mathf.MoveToward(Velocity.X, speed * _direction, Accel * delta);
		Velocity = new Vector2(vx, Velocity.Y);

		if (!_sprite.IsPlaying() || _sprite.Animation != "move")
			_sprite.Play("move");

		if (_phase >= 3)
			ShootProjectile(Mathf.Abs(dx));

		if (!(_attackCooldown <= 0) || !_inAttackRange) return;
		switch (_phase)
		{
			case >= 3 when _rng.Randf() < BlitzChance:
				_ = TriggerBlitz();
				break;
			case >= 2 when Phase2EnableCombo && _rng.Randf() < ComboChance:
				_ = TriggerCombo();
				break;
			default:
				TriggerSingleAttack();
				break;
		}
	}
	
	private void TryLeapToPlayer(float dx)
	{
		if (_player == null) return;
		if (_transitioning || _invincible) return;
		if (_state != State.Chase) return;
		if (_leapCooldown > 0f) return;
	
		if (!IsOnFloor()) return;
	
		var absDx = Mathf.Abs(dx);
		if (absDx < LeapMinDistanceX) return;
		if (absDx > LeapMaxDistanceX) return;
	
		var dy = GlobalPosition.Y - _player.GlobalPosition.Y; 
		if (dy < LeapMinHeightDelta) return;                
		if (dy > LeapMaxHeightDelta) return;                 
	
		var dir = dx >= 0 ? 1 : -1;
	
		Velocity = new Vector2(dir * LeapHorizontalBoost, LeapJumpVelocity);
		_leapCooldown = LeapCooldown;
	}
	
	private void TryDropToPlayer(float dx)
	{
		if (_player == null) return;
		if (_transitioning || _invincible) return;
		if (_state != State.Chase) return;
		if (_dropCooldown > 0f) return;
	
		if (!IsOnFloor()) return;
	
		var absDx = Mathf.Abs(dx);
		if (absDx < DropMinDistanceX) return;
		if (absDx > DropMaxDistanceX) return;
	
		var dy = _player.GlobalPosition.Y - GlobalPosition.Y; 
		if (dy < DropMinHeightDelta) return;                
		if (dy > DropMaxHeightDelta) return;                 
	
		var dir = dx >= 0 ? 1 : -1;
		
		_isPhasing = true;
		_phaseTimer = 0.2f;
		SetCollisionMaskValue(5, false);
		Velocity = new Vector2(dir * DropHorizontalBoost, DropVelocity);
		_dropCooldown = DropCooldown;
	}
	
	
	private void TriggerSingleAttack()
	{
		if (_transitioning || _invincible) return;

		_state = State.Attack;
		_stateTimer = AttackLockTime;

		var idx = _rng.RandiRange(1, 4);
		_sprite.Play($"attack{idx}");

		EnableHitbox();
	}

	private async Task TriggerCombo()
	{
		if (_transitioning || _invincible) return;

		_state = State.Attack;
		_stateTimer = AttackLockTime;

		var a1 = Mathf.Clamp(ComboFirstAttack, 1, 4);
		var a2 = Mathf.Clamp(ComboSecondAttack, 1, 4);

		_sprite.Play($"attack{a1}");
		EnableHitbox();
		await ToSignal(GetTree().CreateTimer(AttackActiveTime), "timeout");
		DisableHitbox();

		await ToSignal(GetTree().CreateTimer(ComboGapSeconds), "timeout");

		if (_state == State.Dead || _transitioning) return;

		_sprite.Play($"attack{a2}");
		EnableHitbox();
		await ToSignal(GetTree().CreateTimer(AttackActiveTime), "timeout");
		DisableHitbox();

		_attackCooldown = GetCooldown();
		_state = State.Chase;
	}

	private async Task TriggerBlitz()
	{
		if (_transitioning || _invincible) return;

		_state = State.Blitz;

		if (_player != null)
		{
			var dx = _player.GlobalPosition.X - GlobalPosition.X;
			_direction = dx >= 0 ? 1 : -1;
			_sprite.FlipH = _direction < 0;
		}

		DisableHitbox();
		_sprite.Play("attack4");

		await ToSignal(GetTree().CreateTimer(BlitzTelegraph), "timeout");

		if (_state == State.Dead || _transitioning) return;

		EnableHitbox();
		_stateTimer = BlitzDuration;
	}

	private void TickAttack(float delta)
	{
		_stateTimer -= delta;

		Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, Accel * delta), Velocity.Y);

		if (_stateTimer <= AttackLockTime - AttackActiveTime)
			DisableHitbox();

		if (_stateTimer > 0) return;

		DisableHitbox();
		_attackCooldown = GetCooldown();
		_state = _player != null ? State.Chase : State.Idle;
	}

	private void TickBlitz(float delta)
	{
		_stateTimer -= delta;

		Velocity = new Vector2(BlitzSpeed * _direction, 0);

		if (_stateTimer > 0) return;

		DisableHitbox();
		_attackCooldown = GetCooldown() + BlitzExtraCooldown;
		Velocity = Vector2.Zero;
		_state = _player != null ? State.Chase : State.Idle;
	}

	private void TickHurt(float delta)
	{
		_stateTimer -= delta;
		if (_stateTimer <= 0)
			_state = _player != null ? State.Chase : State.Idle;
	}

	private void EnableHitbox()
	{
		_canHit = true;
		_hitbox.Monitoring = true;

		foreach (var body in _hitbox.GetOverlappingBodies())
		{
			OnHitboxBodyEntered(body);
			if (!_canHit) break;
		}
	}

	private void DisableHitbox()
	{
		_hitbox.Monitoring = false;
		_canHit = false;
	}

	private void OnHitboxBodyEntered(Node body)
	{
		if (!_canHit) return;
		if (body is not Player.Player player) return;

		player.TakeHit(GetDamage(), new Vector2(_direction * KnockbackStrength, -200));
		DisableHitbox();
		
		_consecutiveHits = 0;
		_consecutiveHitTimer = 0f;
	}

	public override void TakeHit(int dmg, Vector2 attackerWorldPos)
	{
		if (_state == State.Dead) return;
		if (_invincible) return;
		if (_transitioning) return;

		Health -= dmg;

		switch (Health)
		{
			case >= 20:
				_healthBar.GetNode<HealthBar>("HealthBar").UpdateHealth(Health - 20, 10);
				break;
			case >= 10:
				_healthBar.GetNode<HealthBar>("HealthBar2").UpdateHealth(Health - 10, 10);
				break;
			case >= 0:
				_healthBar.GetNode<HealthBar>("HealthBar3").UpdateHealth(Health, 10);
				break;
		}

		if (Health <= 0)
		{
			Die();
			return;
		}

		_damageStreak++;
		_consecutiveHits++;
		_consecutiveHitTimer = ConsecutiveHitWindow;


		if (_consecutiveHits >= ConsecutiveHitsToEscape && IsOnFloor())
		{
			_consecutiveHits = 0;
			_damageStreak = 0;
		
			Velocity = new Vector2(-_direction * EscapeHorizontalBoost, EscapeJumpVelocity);
		}
		else if (_damageStreak >= DamageStreakLimit && IsOnFloor())
		{
			_damageStreak = 0;
			Velocity = new Vector2(-_direction * 420, EscapeJumpVelocity);
		}
		else
		{
			_state = State.Hurt;
			_stateTimer = HurtTime;
			_sprite.Play("hurt");
		}
		
		CheckPhaseAndTransition();
	}

	private void CheckPhaseAndTransition()
	{
		var percent = (float)Health / MaxHealth;

		switch (_phase)
		{
			case 1 when percent <= Phase2HealthPercent:
				_phase = 2;
				_ = TriggerPhaseTransition();
				return;
			case 2 when percent <= Phase3HealthPercent:
				_phase = 3;
				_ = TriggerPhaseTransition();
				break;
		}
	}

	private async Task TriggerPhaseTransition()
	{
		if (_transitioning) return;

		_transitioning = true;
		_invincible = true;

		DisableHitbox();
		_attackCooldown = Mathf.Max(_attackCooldown, 0.8f);
		_projectileCooldown = Mathf.Max(_projectileCooldown, 0.8f);

		_state = State.PhaseTransition;
		Velocity = Vector2.Zero;

		_sprite.Play("shutdown");
		await ToSignal(_sprite, "animation_finished");

		await ToSignal(GetTree().CreateTimer(TransitionFreezeSeconds), "timeout");

		_sprite.Play("enabling");
		await ToSignal(_sprite, "animation_finished");

		await ToSignal(GetTree().CreateTimer(TransitionPostSeconds), "timeout");

		_damageStreak = 0;
		_invincible = false;
		_transitioning = false;

		_state = _player != null ? State.Chase : State.Idle;
	}

	private void ShootProjectile(float absDx)
	{
		if (ProjectileScene == null) return;
		if (_projectileCooldown > 0f) return;
		if (_projectileCheckTimer > 0f) return;
		if (absDx < ProjectileMinDistanceX) return;
		if (_inAttackRange) return;

		_projectileCheckTimer = ProjectileCheckInterval;

		if (_rng.Randf() > ProjectileChancePerCheck)
			return;

		var inst = ProjectileScene.Instantiate<Node2D>();
		inst.GlobalPosition = _projectileSpawn.GlobalPosition;

		var dir = _player.GlobalPosition.X - inst.GlobalPosition.X >= 0 ? 1f : -1f;

		if (inst.HasMethod("Init"))
		{
			inst.Call("Init", new Vector2(dir, 0), ProjectileSpeed, ProjectileDamage);
		}
		else switch (inst)
		{
			case CharacterBody2D cb:
				cb.Velocity = new Vector2(dir * ProjectileSpeed, 0);
				break;
			case RigidBody2D rb:
				rb.LinearVelocity = new Vector2(dir * ProjectileSpeed, 0);
				break;
		}

		GetTree().CurrentScene.AddChild(inst);

		_projectileCooldown = ProjectileCooldown;
	}
	
	

	private async void Die() // Add 'async' here
	{
		_state = State.Dead;
		_transitioning = true;
		_invincible = true;

		DisableHitbox();
		Velocity = Vector2.Zero;

		_sprite.Play("dead");

		await ToSignal(GetTree().CreateTimer(3.0f), SceneTreeTimer.SignalName.Timeout);

		GetTree().ChangeSceneToFile("res://Scenes/EndCredit/endCredit.tscn");
}

	private float GetSpeed()
	{
		return _phase switch
		{
			1 => Phase1Speed,
			2 => Phase2Speed,
			_ => Phase3Speed
		};
	}

	private int GetDamage()
	{
		return _phase switch
		{
			1 => Phase1Damage,
			2 => Phase2Damage,
			_ => Phase3Damage
		};
	}

	private float GetCooldown()
	{
		return _phase switch
		{
			1 => Phase1Cooldown,
			2 => Phase2Cooldown,
			_ => Phase3Cooldown
		};
	}

	private void OnPlayerDetected(Node body)
	{
		if (body is Player.Player p)
			_player = p;
	}

	private void OnPlayerLost(Node body)
	{
		if (body is not Player.Player) return;
		_player = null;
		_inAttackRange = false;
	}

	private void OnRangeBodyEntered(Node body)
	{
		if (body is Player.Player)
			_inAttackRange = true;
	}

	private void OnRangeBodyExited(Node body)
	{
		if (body is Player.Player)
			_inAttackRange = false;
	}
}
