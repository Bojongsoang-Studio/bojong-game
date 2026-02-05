using System.Collections.Generic;
using Godot;

namespace BojongGame.Scenes.UI;

public partial class HealthBar : Node2D
{
	[Export] public Texture2D HeartFull;
	[Export] public Texture2D HeartHalf;
	[Export] public Texture2D HeartEmpty;

	public void UpdateHealth(float currentHealth, float maxHealth)
	{
		List<Sprite2D> heartSprites =
		[
			GetNode<Sprite2D>("1"),
			GetNode<Sprite2D>("2"),
			GetNode<Sprite2D>("3"),
			GetNode<Sprite2D>("4"),
			GetNode<Sprite2D>("5")
		];

		var hpPerHeart = maxHealth / 5.0f;
		var halfHeartThreshold = hpPerHeart / 2.0f;

		for (var i = 0; i < heartSprites.Count; i++)
		{
			var heartLowerBound = i * hpPerHeart;
			var heartValue = currentHealth - heartLowerBound;

			if (heartValue >= hpPerHeart)
			{
				heartSprites[i].Texture = HeartFull;
			}
			else if (heartValue > 0)
			{
				heartSprites[i].Texture = heartValue > halfHeartThreshold ? HeartFull : HeartHalf;
			}
			else
			{
				heartSprites[i].Texture = HeartEmpty;
			}
		}
	}
}
