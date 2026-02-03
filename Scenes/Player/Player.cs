using System.Collections.Generic;
using Godot;

namespace BojongGame.Scenes.Player;

public partial class Player : CharacterBody2D
{
	[Export] public int Health = 5;
	[Export] public float AnimationCooldown = 1f;
	[Export] public int Damage = 1;
	
	[Export] public float WalkSpeed = 220.0f;
	[Export] public float SprintSpeed = 360.0f;
	[Export] public float JumpVelocity = -480.0f;
	[Export] public float Gravity = 1200.0f;

	[Export] public float Acceleration = 18.0f;
	[Export] public float Deceleration = 22.0f;

	[Export] public float DashSpeed = 650.0f;
	[Export] public float DashDuration = 0.15f;
	[Export] public float DashCooldown = 1f;

	private int _health;

	private float _gravity;

	private bool _isDashing;
	private float _dashTimeLeft;
	private float _dashCooldownLeft;
	private int _dashDirection = 1;

	private bool _isOnVerticalMovementArea;
	private uint _originalCollisionMask;

	private float _animationCooldown;

	private List<Enemy> _enemies = [];

	private AnimatedSprite2D _sprite;
	private CollisionShape2D _collision;
	private Area2D _attackArea;

	public override void _Ready()
	{
		_health = Health;
		_sprite = GetNode<AnimatedSprite2D>("Sprite");
		_collision = GetNode<CollisionShape2D>("Collision");
		_attackArea = GetNode<Area2D>("AttackArea");
		_attackArea.BodyEntered += OnEnemyEntered;
		_attackArea.BodyExited += OnEnemyExited;
	}

	public override void _PhysicsProcess(double delta)
	{
		_animationCooldown -= (float)delta;

		if (Input.IsActionJustPressed("attack"))
		{
			foreach (var enemy in _enemies)
			{
				Attack(enemy);
			}
		}
		
		var velocity = Velocity;
		_gravity = Gravity;

		if (_dashCooldownLeft > 0.0f)
		{
			_dashCooldownLeft -= (float)delta;
		}

		if (_isDashing)
		{
			_dashTimeLeft -= (float)delta;
			velocity.Y = 0;
			velocity.X = _dashDirection * DashSpeed;

			Velocity = velocity;
			MoveAndSlide();

			if (_dashTimeLeft <= 0)
			{
				_isDashing = false;
			}

			return;
		}

		if (_isOnVerticalMovementArea)
		{
			var verticalInput = Input.GetAxis("move_up", "move_down");
			var horizontalInput = Input.GetAxis("move_left", "move_right");
				
			velocity.Y = Mathf.MoveToward(velocity.Y, verticalInput * WalkSpeed,
				Acceleration * WalkSpeed * (float)delta);
			velocity.X = Mathf.MoveToward(velocity.X, horizontalInput * WalkSpeed,
				Acceleration * WalkSpeed * (float)delta);

			_sprite.Animation = "idle";
			
			Velocity = velocity;
			MoveAndSlide();

			return;
		}

		if (!IsOnFloor())
		{
			velocity.Y += _gravity * (float)delta;
		}

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

		if (direction != 0)
		{
			_dashDirection = direction;
		}

		var targetSpeed = WalkSpeed;

		if (Input.IsActionPressed("sprint"))
		{
			targetSpeed = SprintSpeed;
		}

		var targetVx = direction * targetSpeed;

		velocity.X = direction != 0
			? Mathf.MoveToward(velocity.X, targetVx, Acceleration * targetSpeed * (float)delta)
			: Mathf.MoveToward(velocity.X, 0.0f, Deceleration * targetSpeed * (float)delta);

		if (Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		if (Input.IsActionJustPressed("sprint") && _dashCooldownLeft <= 0.0f)
		{
			StartDash();
		}

		if (_animationCooldown <= 0.0f)
			_sprite.Animation = direction == 0 ? "idle" : "run";

		GD.Print(Velocity);
		GD.Print(velocity);
		Velocity = velocity;
		MoveAndSlide();
	}

	private void StartDash()
	{
		_isDashing = true;
		_dashTimeLeft = DashDuration;
		_dashCooldownLeft = DashCooldown;
	}

	public void EnterVerticalMovementArea()
	{
		_isOnVerticalMovementArea = true;
		_originalCollisionMask = CollisionMask;
		SetCollisionMaskValue(1, false);
		SetCollisionMaskValue(2, false);
		_gravity = 0;
	}

	public void ExitVerticalMovementArea()
	{
		_isOnVerticalMovementArea = false;
		CollisionMask = _originalCollisionMask;
		_gravity = Gravity;
	}

	public void ShowGuide()
	{
		var guide = GetNode<Label>("Guide");
		guide.Visible = true;
	}
	
	public void HideGuide()
	{
		var guide = GetNode<Label>("Guide");
		guide.Visible = false;
	}

	public void TakeDamage(int damage, Vector2 knockback)
	{
		_animationCooldown = AnimationCooldown;
		_sprite.Animation = "hurt";
		_health -= damage;
		var label = GetNode<Label>("Health");
		label.Text = "Health: " + _health;
		Velocity = knockback;
		MoveAndSlide();
	}

	private void OnEnemyEntered(Node body)
	{
		GD.Print(body);
		if (body is Enemy enemy)
		{
			_enemies.Add(enemy);
		}
	}

	private void OnEnemyExited(Node body)
	{
		if (body is Enemy enemy)
		{
			_enemies.Remove(enemy);
		}
	}

	private void Attack(Enemy enemy)
	{
		_animationCooldown = AnimationCooldown;
		_sprite.Animation = "attack";
		enemy.TakeHit(1, GlobalPosition);
	}
}
