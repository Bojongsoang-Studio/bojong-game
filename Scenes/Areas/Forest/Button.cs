using BojongGame.Scenes.Player;
using Godot;
namespace BojongGame.Scenes.Areas.Forest;
public partial class Button : Area2D
{
	[Export]
	public Door TargetDoor;

	private bool _hasTriggered = false;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!_hasTriggered && body is Player.Player)
		{
			if (TargetDoor != null)
			{
				TargetDoor.Open();
				_hasTriggered = true;
			}
		}
	}
}
