using Godot;

namespace BojongGame.Scenes.UI.HUD;

public partial class PauseMenu : CenterContainer
{
	[Export] public Hud Hud;
	
	public override void _Ready()
	{
		var startButton = GetNode<TextureButton>("VBoxContainer/ResumeButton");
		var exitButton = GetNode<TextureButton>("VBoxContainer/ExitButton");

		startButton.Pressed += OnResumePressed;
		exitButton.Pressed += OnExitPressed;

		startButton.MouseEntered += () => AnimateButton(startButton, true);
		startButton.MouseExited += () => AnimateButton(startButton, false);

		exitButton.MouseEntered += () => AnimateButton(exitButton, true);
		exitButton.MouseExited += () => AnimateButton(exitButton, false);

		CenterPivot(startButton);
		CenterPivot(exitButton);

		startButton.Resized += () => CenterPivot(startButton);
		exitButton.Resized += () => CenterPivot(exitButton);
	}
	
	private void OnResumePressed()
	{
		Hud.DisplayScrim(false);
		Visible = false;
		GetTree().Paused = false;
	}

	private void OnExitPressed()
	{
		GetTree().Quit();
	}
	
	private void AnimateButton(Control button, bool isHovered)
	{
		var tween = CreateTween();
		if (isHovered)
		{
			tween.TweenProperty(button, "scale", new Vector2(1.2f, 1.2f), 0.1f)
				.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		}
		else
		{
			tween.TweenProperty(button, "scale", Vector2.One, 0.1f)
				.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		}
	}

	private static void CenterPivot(Control button)
	{
		button.PivotOffset = button.Size / 2;
	}
}
