using Godot;

namespace BojongGame.Scenes.UI.HUD;

public partial class Hud : CanvasLayer
{
	private ColorRect _scrim;
	private VBoxContainer _phone;

	public override void _Ready()
	{
		_scrim = GetNode<ColorRect>("CenterContainer/Scrim");
		_phone = GetNode<VBoxContainer>("CenterContainer/Phone");

		_scrim.Visible = false;
		_phone.Visible = false;
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("esc"))
		{
			GetTree().Paused = !GetTree().Paused;
			_scrim.Visible = !_scrim.Visible;
			_phone.Visible = false;
		}

		if (Input.IsActionJustPressed("phone"))
		{
			GetTree().Paused = !GetTree().Paused;
			_scrim.Visible = !_scrim.Visible;
			_phone.Visible = !_phone.Visible;
		}
	}
}
