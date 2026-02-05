using BojongGame.Scenes.UI;
using Godot;

namespace BojongGame.Scenes.Enemies.GreenFlyingRobot;

public partial class GreenFlyingRobot : Enemy
{
    [Export] public int MaxHealth = 5;
    [Export] public float Speed = 80.0f;
    [Export] public int DamageAmount = 1;
    [Export] public float KnockbackForce = 300.0f;
    [Export] public float HoverHeight = 150.0f;

    private AnimatedSprite2D _sprite;
    private RayCast2D _laserRay;
    private Node2D _playerTarget;
    private HealthBar _healthBar;

    private AudioStreamPlayer _sfxHit;
    private AudioStreamPlayer _sfxHurt;
    private AudioStreamPlayer _sfxDeath;

    private bool _isAttacking;
    private float _bobOffset;

    public override void _Ready()
    {
        Health = MaxHealth;

        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _laserRay = GetNode<RayCast2D>("LaserRay");
        _healthBar = GetNode<HealthBar>("HealthBar");

        _sfxHit = GetNodeOrNull<AudioStreamPlayer>("SfxHit");
        _sfxHurt = GetNodeOrNull<AudioStreamPlayer>("SfxHurt");
        _sfxDeath = GetNodeOrNull<AudioStreamPlayer>("SfxDeath");

        var hitbox = GetNode<Area2D>("Hitbox");
        hitbox.BodyEntered += OnHitboxBodyEntered;

        var detection = GetNode<Area2D>("DetectionArea");
        detection.BodyEntered += body =>
        {
            if (body is not Player.Player p) return;
            _playerTarget = p;
        };
        detection.BodyExited += body =>
        {
            if (body == _playerTarget) _playerTarget = null;
        };

        _sprite.AnimationFinished += OnAnimationFinished;
        _sprite.Play("fly");

        SetCollisionLayerValue(3, true);
        SetCollisionMaskValue(1, true);
        SetCollisionMaskValue(2, false);
    }

    public override void _PhysicsProcess(double delta)
    {
        var velocity = Velocity;

        if (_isAttacking && Health > 0)
        {
            Velocity = Velocity.MoveToward(Vector2.Zero, 100 * (float)delta);
            MoveAndSlide();
            return;
        }

        if (Health <= 0)
        {
            velocity.X = 0;
            velocity.Y += 1000 * (float)delta;
        }
        else if (_playerTarget != null)
        {
            var targetPos = _playerTarget.GlobalPosition;
            targetPos.Y -= HoverHeight;

            var direction = (targetPos - GlobalPosition).Normalized();
            velocity = direction * Speed;

            _sprite.FlipH = !(_playerTarget.GlobalPosition.X > GlobalPosition.X);

            if (_laserRay.IsColliding())
            {
                var hitObject = _laserRay.GetCollider();
                if (hitObject is Player.Player playerToZap)
                {
                    FireLaser(playerToZap);
                }
            }
        }
        else
        {
            _bobOffset += (float)delta * 5.0f;
            velocity = Velocity.MoveToward(Vector2.Zero, 200 * (float)delta);
            _sprite.Offset = new Vector2(0, Mathf.Sin(_bobOffset) * 5);
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    private void FireLaser(Player.Player player)
    {
        if (Health <= 0) return;
        _isAttacking = true;
        _sprite.Play("attack");

        var laserKnockback = Vector2.Down * KnockbackForce;
        player.TakeHit(DamageAmount, laserKnockback);

        _sfxHit?.Stop();
        _sfxHit?.Play();
    }

    private void OnHitboxBodyEntered(Node2D body)
    {
        if (Health <= 0) return;
        if (_isAttacking) return;

        if (body is not Player.Player player) return;
        var pushDir = (player.GlobalPosition - GlobalPosition).Normalized();
        player.TakeHit(DamageAmount, pushDir * KnockbackForce);

        _sfxHit?.Stop();
        _sfxHit?.Play();
    }

    private void OnAnimationFinished()
    {
        if (_sprite.Animation != "attack" && _sprite.Animation != "hurt") return;
        _isAttacking = false;
        PlayAnimation("fly");
    }

    public override void TakeHit(int dmg, Vector2 attackerWorldPos)
    {
        Health -= dmg;
        _healthBar.UpdateHealth(Health, MaxHealth);

        if (Health > 0)
        {
            _sfxHurt?.Stop();
            _sfxHurt?.Play();
        }

        PlayAnimation("hurt");
        var knockbackDirection = GlobalPosition.X - attackerWorldPos.X >= 0 ? 1f : -1f;
        Velocity = new Vector2(knockbackDirection * 300f, -300f * 0.3f);
        MoveAndSlide();
        
        if (Health > 0) return;
        _sfxDeath?.Stop();
        _sfxDeath?.Play();
        PlayAnimation("death");
    }

    private void PlayAnimation(string name)
    {
        if (_sprite.Animation == name && _sprite.IsPlaying()) return;
        _sprite.Play(name);
    }
}