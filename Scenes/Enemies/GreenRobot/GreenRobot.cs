using BojongGame.Scenes.UI;
using Godot;

namespace BojongGame.Scenes.Enemies.GreenRobot;

public partial class GreenRobot : Enemy
{
    [Export] public int MaxHealth = 5;
    [Export] public float Speed = 50.0f;
    [Export] public float Gravity = 980.0f;
    [Export] public int DamageAmount = 1;
    [Export] public float AnimationCooldown = 0.2f;

	private AnimatedSprite2D _sprite;
	private RayCast2D _ledgeDetector;
	private Area2D _hitbox;
	private HealthBar _healthBar;
	private AudioStreamPlayer _sfxHit;
	private AudioStreamPlayer _sfxHurt;
	private AudioStreamPlayer _sfxDeath;

    private int _direction = 1;
    private float _animationCooldown;

    public override void _Ready()
    {
        Health = MaxHealth;

        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _ledgeDetector = GetNode<RayCast2D>("LedgeDetector");
        _hitbox = GetNode<Area2D>("Hitbox");
        _healthBar = GetNode<HealthBar>("HealthBar");

		_sfxHit = GetNodeOrNull<AudioStreamPlayer>("SfxHit");
		_sfxHurt = GetNodeOrNull<AudioStreamPlayer>("SfxHurt");
		_sfxDeath = GetNodeOrNull<AudioStreamPlayer>("SfxDeath");
		
		_hitbox.BodyEntered += OnHitboxBodyEntered;
	}

    public override void _PhysicsProcess(double delta)
    {
        var velocity = Velocity;
        _animationCooldown -= (float)delta;

        if (!IsOnFloor())
        {
            velocity.Y += Gravity * (float)delta;
        }

        if (IsOnWall() || (IsOnFloor() && !_ledgeDetector.IsColliding()))
        {
            FlipDirection();
        }

        if (Health > 0 && _animationCooldown <= 0f)
        {
            velocity.X = Speed * _direction;
            PlayAnimation(_direction == 0 ? "idle" : "walk");
        }
        else
        {
            velocity.X = 0;
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    private void FlipDirection()
    {
        _direction *= -1;

        _sprite.FlipH = _direction == -1;

        if (_direction == 0) return;
        _ledgeDetector.Scale = new Vector2(_direction, 1f);
    }

    private void OnHitboxBodyEntered(Node2D body)
    {
        if (Health <= 0) return;
        if (body is not Player.Player player) return;
        var directionToPlayer = (player.GlobalPosition - GlobalPosition).Normalized();

        var knockbackForce = directionToPlayer * 300f;

		player.TakeHit(DamageAmount, knockbackForce);
		
		_sfxHit?.Stop();
		_sfxHit?.Play();
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

		_animationCooldown = AnimationCooldown;
		PlayAnimation("hurt");
		var knockbackDirection = GlobalPosition.X - attackerWorldPos.X >= 0 ? 1f : -1f;
		Velocity = new Vector2(knockbackDirection * 300f, -300f * 0.3f);
		MoveAndSlide();
		if (Health <= 0)
		{
			_sfxDeath?.Stop();
			_sfxDeath?.Play();
			PlayAnimation("death");
		}
	}

    private void PlayAnimation(string name)
    {
        if (_sprite.Animation == name && _sprite.IsPlaying()) return;
        _sprite.Play(name);
    }
}