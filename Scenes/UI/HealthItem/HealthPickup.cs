using Godot;
using System;
using PlayerClass = BojongGame.Scenes.Player.Player;

public partial class HealthPickup : Area2D
{
	[Export]
	public int HealAmount = 2;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;

		Tween tween = CreateTween();

		tween.TweenProperty(this, "position:y", Position.Y - 20, 0.2f)
			 .SetTrans(Tween.TransitionType.Quad)
			 .SetEase(Tween.EaseType.Out);

		tween.TweenProperty(this, "position:y", Position.Y, 0.4f)
			 .SetTrans(Tween.TransitionType.Bounce)
			 .SetEase(Tween.EaseType.Out);
	}

	private void OnBodyEntered(Node body)
	{
		 if (body is PlayerClass player)
		 {
			 player.Heal(HealAmount);
			 QueueFree();
		 }
	}
}
