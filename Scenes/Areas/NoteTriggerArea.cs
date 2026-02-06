using BojongGame.Scenes.UI.HUD;
using Godot;

namespace BojongGame.Scenes.Areas;

public partial class NoteTriggerArea : Area2D
{
    [Export] public string Content;

    private bool _isTriggered;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_isTriggered) return;
        Phone.Instance.AddNote(Content);
        _isTriggered = true;
    }
}