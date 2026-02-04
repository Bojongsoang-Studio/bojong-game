using Godot;

namespace BojongGame.Scenes.Areas;

public partial class CameraAdjustmentArea : Area2D
{
    [Export] public float CameraZoom = 3.0f;
    [Export] public int CameraLimitLeft = -1000;
    [Export] public int CameraLimitRight = 1000;
    [Export] public int CameraLimitTop = -1000;
    [Export] public int CameraLimitBottom = 1000;

    [Export] public Node2D Parent;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not Player.Player player) return;
        var camera = player.GetNode<Camera2D>("Camera");
        if (camera == null) return;
        camera.Zoom = new Vector2(CameraZoom, CameraZoom);
        camera.LimitLeft = (int)Parent.Position.X + CameraLimitLeft;
        camera.LimitRight = (int)Parent.Position.X + CameraLimitRight;
        camera.LimitTop = (int)Parent.Position.Y + CameraLimitTop;
        camera.LimitBottom = (int)Parent.Position.Y + CameraLimitBottom;
    }
}