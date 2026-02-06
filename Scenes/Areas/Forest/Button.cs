using Godot;

namespace BojongGame.Scenes.Areas.Forest;

public partial class Button : Area2D
{
    [Export] public Door TargetDoor;

    private Label _label;
    private Label _label2;

    private bool _hasTriggered;

    public override void _Ready()
    {
        _label = GetNode<Label>("PopupDialogLabel");
        _label2 = GetNode<Label>("PopupDialogLabel2");

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not Player.Player) return;

        if (!_hasTriggered)
        {
            _label.Visible = true;

            if (TargetDoor == null) return;
            TargetDoor.Open();
            _hasTriggered = true;
        }
        else
        {
            _label2.Visible = true;
        }
    }

    private void OnBodyExited(Node2D _)
    {
        _label.Visible = false;
        _label2.Visible = false;
    }
}