using Godot;

namespace BojongGame.Scenes.Enemies.Dog;

public partial class Dog : Enemy
{
	private enum State
	{
		Patrol,
		Chase,
		Attack,
		Hurt,
		Dead
	}

	[ExportCategory("Movement")] [Export] public float PatrolSpeed = 65f;
	[Export] public float ChaseSpeed = 120f;
	[Export] public float Acceleration = 900f;
	[Export] public float Gravity = 1200f;

	[ExportCategory("Combat")] [Export] public int MaxHealth = 5;
	[Export] public int Damage = 1;
	[Export] public float AttackCooldown = 0.9f;
	[Export] public float AttackActiveTime = 0.12f;
	[Export] public float AttackLockTime = 0.35f;
	[Export] public float HurtDuration = 0.5f;
	[Export] public float KnockbackStrength = 300f;

	private int _direction = 1;

	private State _state = State.Patrol;
	private float _stateDuration;
	private float _attackCooldown;

	private AnimatedSprite2D _sprite;
	private Node2D _facing;
	private Area2D _detectionArea;
	private Area2D _attackArea;
	private RayCast2D _wallRayCast;
	private RayCast2D _edgeRayCast;

	private Player.Player _target;

	public override void _Ready()
	{
		Health = MaxHealth;

		_sprite = GetNode<AnimatedSprite2D>("Sprite");
		_facing = GetNode<Node2D>("Facing");
		_detectionArea = GetNode<Area2D>("Facing/DetectionArea");
		_attackArea = GetNode<Area2D>("Facing/AttackArea");
		_wallRayCast = GetNode<RayCast2D>("Facing/WallRayCast");
		_edgeRayCast = GetNode<RayCast2D>("Facing/EdgeRayCast");

		_detectionArea.BodyEntered += OnDetectionBodyEntered;
		_detectionArea.BodyExited += OnDetectionBodyExited;

		_attackArea.BodyEntered += OnAttackBodyEntered;

		_attackArea.Monitoring = false;

		PlayAnimation("run");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_state == State.Dead)
		{
			Velocity = new Vector2(0, Velocity.Y + Gravity * (float)delta);
			MoveAndSlide();
			return;
		}

		_attackCooldown = Mathf.Max(0f, _attackCooldown - (float)delta);

		Velocity = new Vector2(Velocity.X, Velocity.Y + Gravity * (float)delta);

		switch (_state)
		{
			case State.Patrol:
				TickPatrol((float)delta);
				break;
			case State.Chase:
				TickChase((float)delta);
				break;
			case State.Attack:
				TickAttack((float)delta);
				break;
			case State.Hurt:
				TickHurt((float)delta);
				break;
		}

		MoveAndSlide();
	}

	private void TickPatrol(float delta)
	{
		if (_wallRayCast.IsColliding()) FlipDirection();

		if (!_edgeRayCast.IsColliding()) FlipDirection();

		var vx = Mathf.MoveToward(Velocity.X, PatrolSpeed * _direction, Acceleration * delta);
		Velocity = new Vector2(vx, Velocity.Y);

		PlayAnimation("run");

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
		_direction = dx >= 0 ? 1 : -1;
		UpdateFacing();

		var vx = Mathf.MoveToward(Velocity.X, ChaseSpeed * _direction, Acceleration * delta);
		Velocity = new Vector2(vx, Velocity.Y);

		PlayAnimation("run");

		if (_attackCooldown <= 0f && IsTargetValid()) Attack();
	}

	private void TickAttack(float delta)
	{
		_stateDuration -= delta;

		Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, Acceleration * delta), Velocity.Y);

		if (_stateDuration <= AttackLockTime - AttackActiveTime) _attackArea.Monitoring = false;

		if (_stateDuration > 0f) return;
		_attackCooldown = AttackCooldown;
		_attackArea.Monitoring = false;
		SetState(IsTargetValid() ? State.Chase : State.Patrol);
	}

	private void TickHurt(float delta)
	{
		_stateDuration -= delta;
		if (_stateDuration <= 0f) SetState(IsTargetValid() ? State.Chase : State.Patrol);
	}

	private void Attack()
	{
		_state = State.Attack;
		_stateDuration = AttackLockTime;

		PlayAnimation("attack");

		_attackArea.Monitoring = true;
	}

	private void SetState(State state)
	{
		_state = state;

		if (_state == State.Patrol) PlayAnimation("run");
		if (_state == State.Chase) PlayAnimation("run");
		if (_state == State.Hurt) PlayAnimation("hurt");
	}

	private void OnDetectionBodyEntered(Node2D body)
	{
		if (body is not Player.Player player) return;
		_target = player;
		if (_state != State.Attack && _state != State.Hurt) SetState(State.Chase);
	}

	private void OnDetectionBodyExited(Node2D body)
	{
		if (body == _target) _target = null;
	}

	private bool IsTargetValid()
	{
		return _target != null && IsInstanceValid(_target);
	}

	private void OnAttackBodyEntered(Node2D body)
	{
        if (_state != State.Attack) return;
        if (body is not Player.Player player) return;
        float pushDir = Mathf.Sign(player.GlobalPosition.X - GlobalPosition.X);
        var knockback = new Vector2(pushDir * KnockbackStrength, -KnockbackStrength * 0.35f);

        player.TakeHit(Damage, knockback);
    }

	public override void TakeHit(int damage, Vector2 attackerPosition)
	{
		if (_state == State.Dead) return;

		Health -= damage;
		
		if (Health <= 0)
		{
			Die();
			return;
		}

		_state = State.Hurt;
		_stateDuration = HurtDuration;

		PlayAnimation("hurt");
		
		var knockbackDirection = (GlobalPosition.X - attackerPosition.X) >= 0 ? 1f : -1f;
		Velocity = new Vector2(knockbackDirection * KnockbackStrength, -KnockbackStrength * 0.3f);
	}

	private void Die()
	{
		_state = State.Dead;
		Velocity = Vector2.Zero;

		PlayAnimation("death");

		_detectionArea.Monitoring = false;
		_attackArea.Monitoring = false;
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
		if (_sprite.Animation == name && _sprite.IsPlaying()) return;
		_sprite.Play(name);
	}
}
