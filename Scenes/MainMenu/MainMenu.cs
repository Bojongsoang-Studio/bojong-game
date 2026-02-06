using Godot;

namespace BojongGame.Scenes.MainMenu;

public partial class MainMenu : Control
{
	[Export]
	public PackedScene GameScene;

	public override void _Ready()
	{
		var startButton = GetNode<TextureButton>("VBoxContainer/StartButton");
		var exitButton = GetNode<TextureButton>("VBoxContainer/ExitButton");

		startButton.Pressed += OnStartPressed;
		exitButton.Pressed += OnQuitPressed;

		startButton.MouseEntered += () => AnimateButton(startButton, true);
		startButton.MouseExited += () => AnimateButton(startButton, false);

		exitButton.MouseEntered += () => AnimateButton(exitButton, true);
		exitButton.MouseExited += () => AnimateButton(exitButton, false);

        CenterPivot(startButton);
        CenterPivot(exitButton);

		startButton.Resized += () => CenterPivot(startButton);
		exitButton.Resized += () => CenterPivot(exitButton);
	}


	private void OnStartPressed()
	{
		GD.Print("Start Button Clicked!");
		if (GameScene == null)
		{
			GD.PrintErr("MainMenu: No GameScene assigned in the Inspector!");
			return;
		}
		GetTree().ChangeSceneToPacked(GameScene);
	}

	private void OnQuitPressed()
	{
		GD.Print("Quit Button Pressed");
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