using Godot;
using PlayerClass = BojongGame.Scenes.Player.Player;

namespace BojongGame.Scenes.Areas.Objects.HealItem;

public partial class HealItem : Area2D
{
	[Export]
	public int HealAmount = 2;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;

		var tween = CreateTween();

		tween.TweenProperty(this, "position:y", Position.Y - 20, 0.2f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);

		tween.TweenProperty(this, "position:y", Position.Y, 0.4f)
			.SetTrans(Tween.TransitionType.Bounce)
			.SetEase(Tween.EaseType.Out);
	}

	private void OnBodyEntered(Node body)
	{
		if (body is not PlayerClass player) return;
		player.Heal(HealAmount);
		QueueFree();
	}
}
