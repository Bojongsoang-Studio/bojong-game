
using Godot;

namespace BojongGame.Scenes.Player
{
    public partial class Player : CharacterBody2D
    {
        [Export] public int PlatformLayer = 2;
        [Export] public float WalkSpeed = 220.0f;
        [Export] public float SprintSpeed = 360.0f;
        [Export] public float JumpVelocity = -480.0f;
        [Export] public float Gravity = 1200.0f;

        [Export] public float Acceleration = 18.0f;
        [Export] public float Deceleration = 22.0f;

        [Export] public float DashSpeed = 650.0f;
        [Export] public float DashDuration = 0.15f;
        [Export] public float DashCooldown = 1f;

        private float _gravity;

        private bool _isDashing;
        private float _dashTimeLeft;
        private float _dashCooldownLeft;
        private int _dashDirection = 1;

        private bool _isOnZebraCross;
        private uint _originalCollisionMask;

        public override void _PhysicsProcess(double delta)
        {
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

            if (_isOnZebraCross)
            {
                var verticalInput = Input.GetAxis("move_up", "move_down");
                var horizontalInput = Input.GetAxis("move_left", "move_right");

                velocity.Y = Mathf.MoveToward(velocity.Y, verticalInput * WalkSpeed,
                    Acceleration * WalkSpeed * (float)delta);
                velocity.X = Mathf.MoveToward(velocity.X, horizontalInput * WalkSpeed,
                    Acceleration * WalkSpeed * (float)delta);

                Velocity = velocity;
                MoveAndSlide();

                return;
            }

            if (!IsOnFloor())
            {
                velocity.Y += _gravity * (float)delta;
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

            if (Input.IsActionJustPressed("move_down") && IsOnFloor())
            {
                DropThrough();
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

        public void EnterZebraCross()
        {
            _isOnZebraCross = true;
            _originalCollisionMask = CollisionMask;
            SetCollisionMaskValue(1, false);
            SetCollisionMaskValue(2, false);
            _gravity = 0;
        }

        public void ExitZebraCross()
        {
            _isOnZebraCross = false;
            CollisionMask = _originalCollisionMask;
            _gravity = Gravity;
        }

        private async void DropThrough()
        {
            // 1. Disable collision with platforms
            SetCollisionMaskValue(PlatformLayer, false);

            // 2. Wait a split second
            await ToSignal(GetTree().CreateTimer(0.15f), SceneTreeTimer.SignalName.Timeout);

            // 3. Re-enable collision
            SetCollisionMaskValue(PlatformLayer, true);
        }
    }
}
