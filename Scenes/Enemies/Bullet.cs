using Godot;
using BojongGame.Scenes.Player;

namespace BojongGame.Scenes.Enemies;

public partial class Bullet : Area2D
{
	[Export] public float Speed = 400.0f;
	public int Direction = 1;
	[Export] public int Damage = 1;
	[Export] public float KnockbackStrength = 300f;

	public override void _PhysicsProcess(double delta)
	{
		// Move in a straight line
		Position += new Vector2(Speed * Direction * (float)delta, 0);
	}

	// Connect the body_entered signal from the Area2D to this function
	private void OnBodyEntered(Node2D body)
	{
	// 1. Check if the thing we hit is actually the Player class
	if (body is BojongGame.Scenes.Player.Player player) 
	{
		var knockback = new Vector2(Direction * KnockbackStrength, -KnockbackStrength * 0.35f);;
		player.TakeDamage(Damage, knockback);
		QueueFree(); // Destroy bullet on hit
		return; // Exit early so we don't check for walls
	}
	
	// 2. Destroy if it hits environment (TileMap or StaticBody)
	// Note: In Godot 4.x, TileMap is often handled via TileMapLayer
	if (body is TileMap || body is StaticBody2D || body is TileMapLayer) 
	{
		QueueFree();
	}
	}
}
