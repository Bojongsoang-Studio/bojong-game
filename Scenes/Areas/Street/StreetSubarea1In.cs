using Godot;

namespace BojongGame.Scenes.Areas.Street;

public partial class StreetSubarea1In : Area2D
{
	[Export] public float CameraZoom = 3.0f;
	[Export] public int CameraLimitLeft = -1000;
	[Export] public int CameraLimitRight = 1000;
	[Export] public int CameraLimitTop = -1000;
	[Export] public int CameraLimitBottom = 1000;

	private Node2D _parent;

	public override void _Ready()
	{
		_parent = GetParent()?.GetParent<Node2D>();
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player.Player player) return;
		var camera = player.GetNode<Camera2D>("Camera");
		if (camera == null) return;
		camera.Zoom = new Vector2(CameraZoom, CameraZoom);
		camera.LimitLeft = (int)_parent.Position.X + CameraLimitLeft;
		camera.LimitRight = (int)_parent.Position.X + CameraLimitRight;
		camera.LimitTop = (int)_parent.Position.Y + CameraLimitTop;
		camera.LimitBottom = (int)_parent.Position.Y + CameraLimitBottom;
	}
}
