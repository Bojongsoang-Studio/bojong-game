using Godot;

namespace BojongGame.Scenes.Enemies.GreenRobot;
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
		_hitbox.BodyEntered += OnHitboxBodyEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity.Y += Gravity * (float)delta;
		}
		if (IsOnWall() || (IsOnFloor() && !_ledgeDetector.IsColliding()))
		{
			FlipDirection();
		}

		velocity.X = Speed * _direction;

		Velocity = velocity;
		MoveAndSlide();
	}

	private void FlipDirection()
	{
		_direction *= -1; //-1 kiri, dan sebaliknya
		
		_sprite.FlipH = _direction == -1;

		Vector2 rayPos = _ledgeDetector.Position;
		rayPos.X = Mathf.Abs(rayPos.X) * _direction;
		_ledgeDetector.Position = rayPos;
	}

	private void OnHitboxBodyEntered(Node2D body)
	{
		if (body is Player.Player player)
		{
			GD.Print("HIT PLAYER!"); 

			Vector2 directionToPlayer = (player.GlobalPosition - GlobalPosition).Normalized();

			Vector2 knockbackForce = directionToPlayer * 300f;

			player.TakeHit(DamageAmount,knockbackForce);
		}
	}
}
