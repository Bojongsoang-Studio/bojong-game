using Godot;

namespace BojongGame.Scenes.UI.HUD;

public partial class Hud : CanvasLayer
{
	[Export] public AnimationPlayer PhoneHudNotificationAnimationPlayer;
	[Export] public AnimationPlayer PhoneHudKeymapAnimationPlayer;

	private ColorRect _scrim;
	private VBoxContainer _phone;
	private PauseMenu _pauseMenu;

	public override void _Ready()
	{
		_scrim = GetNode<ColorRect>("CenterContainer/Scrim");
		_phone = GetNode<VBoxContainer>("CenterContainer/Phone");
		_pauseMenu = GetNode<PauseMenu>("CenterContainer/PauseMenu");

		_scrim.Visible = false;
		_phone.Visible = false;
		_pauseMenu.Visible = false;
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("esc"))
		{
			GetTree().Paused = !GetTree().Paused;
			_scrim.Visible = !_scrim.Visible;

			if (!_phone.Visible)
			{
				_pauseMenu.Visible = !_pauseMenu.Visible;
			}
			else
			{
				_phone.Visible = false;
			}
		}

		if (!Input.IsActionJustPressed("phone") || _pauseMenu.Visible) return;
		GetTree().Paused = !GetTree().Paused;
		_scrim.Visible = !_scrim.Visible;
		_phone.Visible = !_phone.Visible;
		PhoneHudNotificationAnimationPlayer.Stop();
		PhoneHudNotificationAnimationPlayer.Seek(0, true);
		PhoneHudKeymapAnimationPlayer.Stop();
		PhoneHudKeymapAnimationPlayer.Seek(0, true);
	}

	public void DisplayScrim(bool show)
	{
		_scrim.Visible = show;
	}
}
