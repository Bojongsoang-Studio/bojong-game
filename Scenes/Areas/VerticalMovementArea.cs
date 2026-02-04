using Godot;

namespace BojongGame.Scenes.Areas;

public partial class VerticalMovementArea : Area2D
{
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	private static void OnBodyEntered(Node2D body)
	{
		if (body is Player.Player player)
		{
			player.EnterVerticalMovement();
		}
	}

	private static void OnBodyExited(Node2D body)
	{
		if (body is Player.Player player)
		{
			player.ExitVerticalMovement();
		}
	}
}
