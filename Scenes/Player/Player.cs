using System.Collections.Generic;
using Godot;

namespace BojongGame.Scenes.Player;

public partial class Player : CharacterBody2D
{
	[Export] public int Health = 10;
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
	private float _dashDuration;
	private float _dashCooldown;
	private int _dashDirection = 1;

	private bool _isInVerticalMovement;
	private uint _originalCollisionMask;

	private float _animationCooldown;
	private Vector2 _knockbackVelocity = Vector2.Zero;

	private readonly List<Enemy> _enemies = [];

	private AnimatedSprite2D _sprite;
	private CollisionShape2D _collision;
	private Area2D _attackArea;

	public override void _Ready()
	{
		_health = Health;
		_sprite = GetNode<AnimatedSprite2D>("Sprite");
		_collision = GetNode<CollisionShape2D>("Collision");
		_attackArea = GetNode<Area2D>("AttackArea");
		_attackArea.BodyEntered += OnAttackBodyEntered;
		_attackArea.BodyExited += OnAttackBodyExited;
		SetCollisionLayerValue(2, true);
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
		_isInVerticalMovement = true;
		_originalCollisionMask = CollisionMask;
		SetCollisionMaskValue(1, false);
		_gravity = 0;
	}

	public void ExitVerticalMovement()
	{
		_isInVerticalMovement = false;
		CollisionMask = _originalCollisionMask;
		_gravity = Gravity;
	}

	public void DisplayTransitionGuide(bool show)
	{
		var guide = GetNode<Label>("TransitionGuide");
		guide.Visible = show;
	}

	public void TakeHit(int damage, Vector2 knockback)
	{
		_animationCooldown = AnimationCooldown;
		PlayAnimation("hurt");
		_health -= damage;
		var label = GetNode<Label>("Health");
		label.Text = "Health: " + _health;
		_knockbackVelocity = knockback;
		if (IsOnFloor()) _knockbackVelocity.Y = -200; //coba dulu, kalau ga bagus hapus
		Velocity = _knockbackVelocity;
		MoveAndSlide();
	}

	private void OnAttackBodyEntered(Node2D body)
	{
		if (body is Enemy enemy) _enemies.Add(enemy);
	}

	private void OnAttackBodyExited(Node2D body)
	{
		if (body is Enemy enemy) _enemies.Remove(enemy);
	}

	private void Attack()
	{
		_animationCooldown = AnimationCooldown;
		PlayAnimation("attack");
		for (int i = _enemies.Count - 1; i >= 0; i--)
		{
			if (IsInstanceValid(_enemies[i]))
			{
				_enemies[i].TakeHit(Damage, GlobalPosition);
			}
			else
			{
				_enemies.RemoveAt(i);
			}
		}
	}
	
	private void PlayAnimation(string name)
	{
		if (_sprite.Animation == name && _sprite.IsPlaying()) return;
		_sprite.Play(name);
	}
}
