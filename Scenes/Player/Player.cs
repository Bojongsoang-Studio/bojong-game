using Godot;

namespace BojongGame.Scenes.Player
{
	public partial class Player : CharacterBody2D
	{
		[Export]
		public float WalkSpeed = 220.0f;
		[Export]
		public float SprintSpeed = 360.0f;
		[Export]
		public float JumpVelocity = -450.0f;
		[Export]
		public float Gravity = 1200.0f;

		[Export]
		public float Acceleration = 18.0f;
		[Export]
		public float Deceleration = 22.0f;

		[Export]
		public float DashSpeed = 650.0f;
		[Export]
		public float DashDuration = 0.15f;
		[Export]
		public float DashCooldown = 3.0f;

		private bool _isDashing;
		private float _dashTimeLeft;
		private float _dashCooldownLeft;
		private int _dashDirection = 1;

		public override void _PhysicsProcess(double delta)
		{
			var velocity = Velocity;

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

			if (!IsOnFloor())
			{
				velocity.Y += Gravity * (float)delta;
			}

			var dir = 0;

			if (Input.IsActionPressed("move_left"))
			{
				dir -= 1;
			}

			if (Input.IsActionPressed("move_right"))
			{
				dir += 1;
			}

			if (dir != 0)
			{
				_dashDirection = dir;
			}

			var targetSpeed = WalkSpeed;

			if (Input.IsActionPressed("sprint"))
			{
				targetSpeed = SprintSpeed;
			}

			var targetVx = dir * targetSpeed;

			velocity.X = dir != 0
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

			Velocity = velocity;
			MoveAndSlide();
		}

		private void StartDash()
		{
			_isDashing = true;
			_dashTimeLeft = DashDuration;
			_dashCooldownLeft = DashCooldown;
		}
	}
}
