using Godot;

namespace BojongGame.Scenes;

public abstract partial class Enemy : CharacterBody2D
{
	public int Health = 5;

	public abstract void TakeHit(int dmg, Vector2 attackerWorldPos);
}
