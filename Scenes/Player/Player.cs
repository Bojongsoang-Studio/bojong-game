using System.Collections.Generic;
using System.Linq;
using BojongGame.Scenes.UI;
using Godot;

namespace BojongGame.Scenes.Player;

public partial class Player : CharacterBody2D
{
	[Export] public int MaxHealth = 10;
	[Export] public float AnimationCooldown = 1f;
	[Export] public int Damage = 1;

	[Export] public Vector2 SpawnPoint;

	[Export] public float WalkSpeed = 220.0f;
	[Export] public float SprintSpeed = 360.0f;
	[Export] public float JumpVelocity = -480.0f;
	[Export] public float Gravity = 1200.0f;

	[Export] public float Acceleration = 18.0f;
	[Export] public float Deceleration = 22.0f;

	[Export] public float DashSpeed = 650.0f;
	[Export] public float DashDuration = 0.15f;
	[Export] public float DashCooldown = 1f;

	[Export] public float InvincibilityDuration = 1.5f;

	private int _health;

	private float _gravity;

	private bool _isDashing;
	private float _dashDuration;
	private float _dashCooldown;
	private int _dashDirection = 1;

	private bool _isInVerticalMovement;
	private uint _originalCollisionMask;
	private bool _isInvincible;

	private float _animationCooldown;
	private Vector2 _knockbackVelocity = Vector2.Zero;

	private readonly List<Enemies.Enemy> _enemies = [];

	private AnimatedSprite2D _sprite;
	private CollisionShape2D _collision;
	private Area2D _attackArea;
	private HealthBar _healthBar;

	public override void _Ready()
	{
		_health = MaxHealth;
		_sprite = GetNode<AnimatedSprite2D>("Sprite");
		_collision = GetNode<CollisionShape2D>("Collision");
		_attackArea = GetNode<Area2D>("AttackArea");
		_healthBar = GetNode<HealthBar>("HealthBar");

		_attackArea.BodyEntered += OnAttackBodyEntered;
		_attackArea.BodyExited += OnAttackBodyExited;

		SetCollisionLayerValue(2, true);
		SetCollisionLayerValue(1, false);
		SetCollisionMaskValue(1, true);
		SetCollisionMaskValue(3, false);
	}

	public override void _PhysicsProcess(double delta)
	{
		var velocity = Velocity;
		_gravity = Gravity;
		_animationCooldown -= (float)delta;

		if (_knockbackVelocity.Length() > 10.0f)
		{
			_knockbackVelocity = _knockbackVelocity.MoveToward(Vector2.Zero, 800.0f * (float)delta);
			velocity = _knockbackVelocity;
		}
		else
		{
			if (Input.IsActionJustPressed("attack")) Attack();

			if (_dashCooldown > 0.0f) _dashCooldown -= (float)delta;

			if (_isDashing)
			{
				_dashDuration -= (float)delta;
				velocity.Y = 0;
				velocity.X = _dashDirection * DashSpeed;

				Velocity = velocity;
				MoveAndSlide();

				if (_dashDuration <= 0)
				{
					_isDashing = false;
				}

				return;
			}

			if (_isInVerticalMovement)
			{
				var verticalInput = Input.GetAxis("move_up", "move_down");
				var horizontalInput = Input.GetAxis("move_left", "move_right");

				velocity.Y = Mathf.MoveToward(velocity.Y, verticalInput * WalkSpeed,
					Acceleration * WalkSpeed * (float)delta);
				velocity.X = Mathf.MoveToward(velocity.X, horizontalInput * WalkSpeed,
					Acceleration * WalkSpeed * (float)delta);

				PlayAnimation("idle");

				Velocity = velocity;
				MoveAndSlide();

				return;
			}

			if (!IsOnFloor()) velocity.Y += _gravity * (float)delta;

			var direction = 0;

			if (Input.IsActionPressed("move_left"))
			{
				direction -= 1;
				_sprite.FlipH = true;
			}

			if (Input.IsActionPressed("move_right"))
			{
				direction += 1;
				_sprite.FlipH = false;
			}
			
			if (Input.IsActionPressed("move_down"))
			{
				DropThrough();
			}

			if (direction != 0) _dashDirection = direction;

			var targetSpeed = WalkSpeed;

			if (Input.IsActionPressed("sprint")) targetSpeed = SprintSpeed;

			var targetVx = direction * targetSpeed;

			velocity.X = direction != 0
				? Mathf.MoveToward(velocity.X, targetVx, Acceleration * targetSpeed * (float)delta)
				: Mathf.MoveToward(velocity.X, 0.0f, Deceleration * targetSpeed * (float)delta);

			if (Input.IsActionJustPressed("jump") && IsOnFloor()) velocity.Y = JumpVelocity;

			if (Input.IsActionJustPressed("sprint") && _dashCooldown <= 0.0f) Dash();

			if (_animationCooldown <= 0.0f) PlayAnimation(direction == 0 ? "idle" : "run");
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void Dash()
	{
		_isDashing = true;
		_dashDuration = DashDuration;
		_dashCooldown = DashCooldown;
	}

	public void EnterVerticalMovement()
	{
		var guide = GetNode<Node2D>("VerticalMovementGuide");
		guide.Visible = true;
		_isInVerticalMovement = true;
		_originalCollisionMask = CollisionMask;
		SetCollisionMaskValue(1, false);
		_gravity = 0;
	}

	public void ExitVerticalMovement()
	{
		var guide = GetNode<Node2D>("VerticalMovementGuide");
		guide.Visible = false;
		_isInVerticalMovement = false;
		CollisionMask = _originalCollisionMask;
		_gravity = Gravity;
	}

	public void DisplayTransitionGuide(bool show)
	{
		var guide = GetNode<Node2D>("TransitionGuide");
		guide.Visible = show;
	}

	public void TakeHit(int damage, Vector2 knockback)
	{
		if (_isInvincible || _health <= 0) return;

		_health -= damage;
		_healthBar.UpdateHealth(_health, MaxHealth);
		_knockbackVelocity = knockback;

		if (IsOnFloor()) _knockbackVelocity.Y = -200;
		Velocity = _knockbackVelocity;

		_animationCooldown = AnimationCooldown;
		PlayAnimation("hurt");

		TriggerInvincibility(InvincibilityDuration);
		MoveAndSlide();

		if (_health <= 0) Die();
	}

	private void OnAttackBodyEntered(Node2D body)
	{
		if (body is Enemies.Enemy enemy) _enemies.Add(enemy);
	}

	private void OnAttackBodyExited(Node2D body)
	{
		if (body is Enemies.Enemy enemy) _enemies.Remove(enemy);
	}

	private void Attack()
	{
		_animationCooldown = AnimationCooldown;
		PlayAnimation("attack");
		foreach (var enemy in _enemies.Where(IsInstanceValid)) enemy.TakeHit(Damage, GlobalPosition);
	}

	private void PlayAnimation(string name)
	{
		if (_sprite.Animation == name && _sprite.IsPlaying()) return;
		_sprite.Play(name);
	}

	private void TriggerInvincibility(float duration)
	{
		_isInvincible = true;

		var tween = CreateTween();
		tween.SetLoops();
		tween.TweenProperty(_sprite, "modulate:a", 0.5f, 0.1f);
		tween.TweenProperty(_sprite, "modulate:a", 1.0f, 0.1f);
		GetTree().CreateTimer(duration).Timeout += () =>
		{
			_isInvincible = false;

			if (tween.IsValid()) tween.Kill();
			_sprite.Modulate = Colors.White;
		};
	}
	
	private async void DropThrough()
	{
		SetCollisionMaskValue(5, false);
		await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
		SetCollisionMaskValue(5, true);
  }

	public void SetSpawnPoint(Vector2 spawnPoint)
	{
		SpawnPoint = spawnPoint;
	}

	private void Die()
	{
		PlayAnimation("death");
		_animationCooldown = AnimationCooldown;
		_health = MaxHealth;
		_healthBar.UpdateHealth(_health, MaxHealth);
		GlobalPosition = SpawnPoint;
		TriggerInvincibility(InvincibilityDuration);
	}
}
