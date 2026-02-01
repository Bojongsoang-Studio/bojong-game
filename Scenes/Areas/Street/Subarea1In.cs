using Godot;

namespace BojongGame.Scenes.Areas.Street;

public partial class Subarea1In : Area2D
{
	[Export] public int CameraLimitLeft = -1000;
	[Export] public int CameraLimitRight = 1000;
	[Export] public int CameraLimitTop = -1000;
	[Export] public int CameraLimitBottom = 1000;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player.Player player) return;
		var camera = player.GetNode<Camera2D>("Camera2D");
		if (camera == null) return;
		camera.LimitLeft = CameraLimitLeft;
		camera.LimitRight = CameraLimitRight;
		camera.LimitTop = CameraLimitTop;
		camera.LimitBottom = CameraLimitBottom;
	}
}
