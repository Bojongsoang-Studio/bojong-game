using Godot;

namespace BojongGame.Scenes.Enemies;

public partial class PurpleGuy : Enemy
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
    [Export] public float Accel = 900f;
    [Export] public float Gravity = 1200f;

    [ExportCategory("Combat")] [Export] public int MaxHp = 5;
    [Export] public int Damage = 1;
    [Export] public float BulletSpeed = 200f;
    [Export] public float AttackCooldown = 0.9f;
    [Export] public float AttackActiveTime = 0.12f;
    [Export] public float AttackLockTime = 1f;
    [Export] public float HurtTime = 0.22f;
    [Export] public float KnockbackStrength = 300f;

    [Export] public PackedScene BulletScene;

    private int _hp;
    private int _direction = 1;

    private State _state = State.Patrol;
    private float _stateTimer;
    private float _attackCd;

    private AnimatedSprite2D _animation;
    private Area2D _detectArea;
    private Area2D _attackArea;
    private RayCast2D _wallRay;
    private RayCast2D _edgeRay;
    private Area2D _bullet;

    private Player.Player _target;

    public override void _Ready()
    {
        _hp = MaxHp;
        Health = _hp;

        _animation = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _detectArea = GetNode<Area2D>("DetectionArea");
        _attackArea = GetNode<Area2D>("AttackArea");
        _wallRay = GetNode<RayCast2D>("WallRay");
        _edgeRay = GetNode<RayCast2D>("EdgeRay");
        _bullet = GetNode<Area2D>("Bullet");

        _detectArea.BodyEntered += OnDetectEntered;
        _detectArea.BodyExited += OnDetectExited;

        _attackArea.BodyEntered += OnAttackAreaBodyEntered;

        _attackArea.Monitoring = false;

        _bullet.BodyEntered += OnBulletEntered;

        PlayAnimation("run");
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
        if (_wallRay.IsColliding())
            FlipDirection();

        if (_edgeRay != null && !_edgeRay.IsColliding())
            FlipDirection();

        var vx = Mathf.MoveToward(Velocity.X, PatrolSpeed * _direction, Accel * dt);
        Velocity = new Vector2(vx, Velocity.Y);

        PlayAnimation("run");

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
        _direction = dx >= 0 ? 1 : -1;
        UpdateFacing();

        var vx = Mathf.MoveToward(Velocity.X, ChaseSpeed * _direction, Accel * dt);
        Velocity = new Vector2(vx, Velocity.Y);

        PlayAnimation("run");

        if (_attackCd <= 0f && IsTargetInAttackArea())
            StartAttack();
    }

    private void TickAttack(float dt)
    {
        _stateTimer -= dt;

        //Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, Accel * dt), Velocity.Y);

        if (_stateTimer <= AttackLockTime - AttackActiveTime)
            _attackArea.Monitoring = false;

        if (_stateTimer > 0f)
        {
            _bullet.GlobalPosition = new Vector2(_bullet.GlobalPosition.X + BulletSpeed * dt * _direction,
                _bullet.GlobalPosition.Y);
            return;
        }

        _attackCd = AttackCooldown;
        _bullet.Visible = false;
        _bullet.GlobalPosition = GlobalPosition;
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
        _bullet.Visible = true;
        _bullet.GlobalPosition = GlobalPosition;
        if (_direction != 0) _bullet.Scale = new Vector2(_direction, 1f);

        _attackArea.Monitoring = true;
    }

    private void SetState(State s)
    {
        _state = s;

        if (_state == State.Patrol) PlayAnimation("run");
        if (_state == State.Chase) PlayAnimation("run");
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
        if (body == _target)
            _target = null;
    }

    private bool IsTargetValid()
    {
        return _target != null && IsInstanceValid(_target);
    }

    private bool IsTargetInAttackArea()
    {
        return _target != null;
    }

    private void OnAttackAreaBodyEntered(Node body)
    {
        // if (_state != State.Attack) return;
        // if (_attackCd > 0)
        // {
        //     if (!_bullet.Visible)
        //     {
        //         
        //     }
        // }
    }

    private void OnBulletEntered(Node body)
    {
        if (_state != State.Attack) return;
        if (body is not Player.Player player) return;
        if (_stateTimer > 0f && _bullet.Visible)
        {
            var knockback = new Vector2(_direction * KnockbackStrength, -KnockbackStrength * 0.35f);
            player.TakeDamage(Damage, knockback);
        }

        _bullet.GlobalPosition = GlobalPosition;
        _bullet.Visible = false;
        _attackArea.Monitoring = false;
    }

    public override void TakeHit(int dmg, Vector2 attackerWorldPos)
    {
        if (_state == State.Dead) return;

        _hp -= dmg;
        Health = _hp;

        GD.Print(Health);

        if (_hp <= 0)
        {
            Die();
            return;
        }

        _state = State.Hurt;
        _stateTimer = HurtTime;

        PlayAnimation("hurt");

        var knockbackDirection = (GlobalPosition.X - attackerWorldPos.X) >= 0 ? 1f : -1f;
        Velocity = new Vector2(knockbackDirection * KnockbackStrength, -KnockbackStrength * 0.3f);
    }

    private void Die()
    {
        _state = State.Dead;
        Velocity = Vector2.Zero;

        PlayAnimation("death");

        _detectArea.Monitoring = false;
        _attackArea.Monitoring = false;
    }

    private void FlipDirection()
    {
        _direction *= -1;
        UpdateFacing();
    }

    private void UpdateFacing()
    {
        _animation.FlipH = _direction < 0;

        var attackAreaPosition = _attackArea.Position;
        attackAreaPosition.X = Mathf.Abs(attackAreaPosition.X) * _direction;
        _attackArea.Position = attackAreaPosition;

        var wallRayPosition = _wallRay.Position;
        wallRayPosition.X = Mathf.Abs(wallRayPosition.X) * _direction;
        _wallRay.Position = wallRayPosition;

        if (_edgeRay == null) return;
        var edgeRayPosition = _edgeRay.Position;
        edgeRayPosition.X = Mathf.Abs(edgeRayPosition.X) * _direction;
        _edgeRay.Position = edgeRayPosition;
    }

    private void PlayAnimation(string name)
    {
        if (_state == State.Dead && name != "death") return;

        if (_animation.Animation == name && _animation.IsPlaying())
            return;

        _animation.Play(name);
    }
}