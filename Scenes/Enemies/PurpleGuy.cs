using System;
using Godot;

namespace BojongGame.Scenes.Enemies;

public partial class PurpleGuy : CharacterBody2D
{
	[Export] public float Speed = 100.0f;
	[Export] public float ChaseSpeed = 120.0f;
	[Export] public float Gravity = 1500.0f; // Increased gravity for a snappier feel

	private int _direction = 1; 
	private Node2D _player = null;
	
	public int health = 4;

	private RayCast2D _wallDetector;
	private RayCast2D _ledgeDetector;

	public override void _Ready()
	{
		// Using the names from your screenshot
		_wallDetector = GetNode<RayCast2D>("WallDetectionRayCast");
		_ledgeDetector = GetNode<RayCast2D>("EdgeDetectionRayCast");

		// Safety: Enable the raycasts via code just in case they are off in the editor
		_wallDetector.Enabled = true;
		_ledgeDetector.Enabled = true;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// 1. Gravity Logic
		if (!IsOnFloor())
		{
			velocity.Y += Gravity * (float)delta;
		}

		// 2. Movement Logic
		if (_player != null)
		{
			// CHASE
			float dirToPlayer = Math.Sign(_player.GlobalPosition.X - GlobalPosition.X);
			velocity.X = dirToPlayer * ChaseSpeed;

			if (dirToPlayer != 0 && dirToPlayer != _direction)
			{
				Flip();
			}
		}
		else
		{
			// PATROL
			// If hitting a wall OR no floor detected by the edge raycast
			if (IsOnWall() || !_ledgeDetector.IsColliding())
			{
				Flip();
			}
			velocity.X = _direction * Speed;
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void Flip()
	{
		_direction *= -1;

		// 1. Flip the Sprite visually
		var sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		sprite.FlipH = _direction == -1;

		// 2. Flip the RayCasts by moving their Target Position
		// This assumes your Wall RayCast starts pointing Right (positive X)
		_wallDetector.TargetPosition = new Vector2(Math.Abs(_wallDetector.TargetPosition.X) * _direction, 0);
	
		// 3. Move the Ledge detector's position to the "front" of the enemy
		// If it was at X=20, it needs to move to X=-20 when facing left
		_ledgeDetector.Position = new Vector2(Math.Abs(_ledgeDetector.Position.X) * _direction, _ledgeDetector.Position.Y);
	
		Vector2 pos = GlobalPosition;
		pos.X += (20 * _direction);
		GlobalPosition = pos;
	}

	// Remember to connect these signals in the Godot Editor!
	private void OnDetectionAreaBodyEntered(Node2D body)
	{
		if (body is Player.Player) {
			_player = body;
			GD.Print("Spotted the player!");
		}
	}

	private void OnDetectionAreaBodyExited(Node2D body)
	{
		if (body == _player){
			_player = null;
			GD.Print("Spotted the player!");
		} 
	}
}
