using Godot;

namespace BojongGame.Scenes.Areas.Street.Citizens;

public partial class Biker : Node2D
{
	private Area2D _area;
	private Label _dialog;
	
	public override void _Ready()
	{
		_area = GetNode<Area2D>("Area");
		_dialog = GetNode<Label>("Dialog");
		
		_area.BodyEntered += OnBodyEntered;
		_area.BodyExited += OnBodyExited;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player.Player)
		{
			_dialog.Visible = true;
		}
	}
	
	private void OnBodyExited(Node2D body)
	{
		if (body is Player.Player)
		{
			_dialog.Visible = false;
		}
	}
}
