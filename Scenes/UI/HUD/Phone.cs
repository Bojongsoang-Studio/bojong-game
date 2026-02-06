using System.Collections.Generic;
using Godot;

namespace BojongGame.Scenes.UI.HUD;

public partial class Phone : VBoxContainer
{
	[Export] public string DefaultContent;

	private readonly List<string> _notes = [];
	private int _currentNoteIndex = -1;
	private RichTextLabel _content;
	private TextureRect _leftEnabled;
	private TextureRect _leftDisabled;
	private TextureRect _rightEnabled;
	private TextureRect _rightDisabled;

	public override void _Ready()
	{
		_content = GetNode<RichTextLabel>("HBoxContainer/CenterContainer/Note/Content");
		_leftEnabled = GetNode<TextureRect>("HBoxContainer/Left/Enabled");
		_leftDisabled = GetNode<TextureRect>("HBoxContainer/Left/Disabled");
		_rightEnabled = GetNode<TextureRect>("HBoxContainer/Right/Enabled");
		_rightDisabled = GetNode<TextureRect>("HBoxContainer/Right/Disabled");
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("move_left") && _leftEnabled.Visible)
		{
			_currentNoteIndex--;
		}

		if (Input.IsActionJustPressed("move_right") && _rightEnabled.Visible)
		{
			_currentNoteIndex++;
		}

		UpdateContent();
	}

	private void AddNote(string note)
	{
		_notes.Add(note);
		_currentNoteIndex = _notes.Count - 1;
		UpdateContent();
	}

	private void UpdateContent()
	{
		if (_currentNoteIndex < 0 || _currentNoteIndex >= _notes.Count)
		{
			_content.Text = DefaultContent;
			_leftDisabled.Visible = true;
			_leftEnabled.Visible = false;
			_rightDisabled.Visible = true;
			_rightEnabled.Visible = false;
		}
		else
		{
			_content.Text = _notes[_currentNoteIndex];
			if (_currentNoteIndex > 0)
			{
				_leftDisabled.Visible = false;
				_leftEnabled.Visible = true;
			}
			else
			{
				_leftDisabled.Visible = true;
				_leftEnabled.Visible = false;
			}

			if (_currentNoteIndex >= _notes.Count - 1)
			{
				_rightDisabled.Visible = false;
				_rightEnabled.Visible = true;
			}
			else
			{
				_rightDisabled.Visible = true;
				_rightEnabled.Visible = false;
			}
		}
	}
}
