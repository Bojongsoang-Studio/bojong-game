using Godot;
using System;

public partial class EndCredit : Control
{
	[Export] public float ScrollSpeed = 100.0f;
	
	private RichTextLabel _label;
	private AudioStreamPlayer _music; // Added reference

	public override void _Ready()
	{
		_label = GetNode<RichTextLabel>("RichTextLabel");
		_music = GetNode<AudioStreamPlayer>("MusicPlayer"); // Match the node name

		// Start position
		Vector2 screenSize = GetViewportRect().Size;
		_label.Position = new Vector2(_label.Position.X, screenSize.Y);
		
		// Optional: Start music here if not using Autoplay
		// _music.Play();
	}

	public override void _Process(double delta)
	{
		Vector2 currentPos = _label.Position;
		currentPos.Y -= ScrollSpeed * (float)delta;
		_label.Position = currentPos;

		if (_label.Position.Y < -_label.Size.Y)
		{
			FinishCredits();
		}
	}

	private void FinishCredits()
	{
		_music?.Stop();
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu/main_menu.tscn");
	}
}
