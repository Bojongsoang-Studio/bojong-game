using Godot;

namespace BojongGame.Scenes.UI.PopupDialog;

public partial class PopupDialogArea : Area2D
{
	private Label _label;
	
	public override void _Ready()
	{
		_label = GetNode<Label>("PopupDialogLabel");
		_label.Visible = false;
		
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player.Player) return;
		_label.Visible = true;
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is not Player.Player) return;
		_label.Visible = false;
	}
}
