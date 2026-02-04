using Godot;
namespace BojongGame.Scenes.Areas.Forest;
public partial class GreenRobot : CharacterBody2D
{
	[Export] public float Speed = 50.0f;
	[Export] public float Gravity = 980.0f;
	[Export] public int DamageAmount = 1;
	

	private AnimatedSprite2D _sprite;
	private RayCast2D _ledgeDetector;
	private Area2D _hitbox;
	
	// Direction: 1 is Right, -1 is Left
	private int _direction = 1; 

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_ledgeDetector = GetNode<RayCast2D>("LedgeDetector");
		_hitbox = GetNode<Area2D>("Hitbox");

		// 2. Connect the "BodyEntered" signal via code
		// This means: "When something enters the hitbox, run OnHitboxBodyEntered"
		_hitbox.BodyEntered += OnHitboxBodyEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// 1. Apply Gravity
		if (!IsOnFloor())
		{
			velocity.Y += Gravity * (float)delta;
		}

		// 2. Check for Obstacles (Walls) or Cliffs
		// IsOnWall() checks if the body hit a wall.
		// !_ledgeDetector.IsColliding() means the raycast sees NO floor (Cliff).
		if (IsOnWall() || (IsOnFloor() && !_ledgeDetector.IsColliding()))
		{
			FlipDirection();
		}

		// 3. Move
		velocity.X = Speed * _direction;

		Velocity = velocity;
		MoveAndSlide();
	}

	private void FlipDirection()
	{
		_direction *= -1; // Swap 1 to -1, or -1 to 1
		
		// Flip the visual sprite
		_sprite.FlipH = _direction == -1;

		// Crucial: Flip the "Ledge Detector" so it points in front of the new direction
		// If moving Right (1), Raycast should be at +10. If Left (-1), at -10.
		Vector2 rayPos = _ledgeDetector.Position;
		rayPos.X = Mathf.Abs(rayPos.X) * _direction;
		_ledgeDetector.Position = rayPos;
	}

	private void OnHitboxBodyEntered(Node2D body)
	{
		// Check if the body is actually the Player
		if (body is Player.Player player)
		{
			// Call a "TakeDamage" function on your Player script
			// You need to make sure your Player.cs has this function!
			// player.TakeDamage(DamageAmount);
			
			// Optional: Knockback?
			// player.Knockback(GlobalPosition);
		}
	}
}
