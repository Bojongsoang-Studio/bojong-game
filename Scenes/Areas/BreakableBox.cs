using Godot;
using System;

public partial class BreakableBox : StaticBody2D
{
	[Export]
	public PackedScene PickupScene;

	public override void _Ready()
	{
		AddToGroup("Destructible");
	}

	public void Smash()
	{
		if (PickupScene == null)
		{
			GD.PrintErr("BreakableBox: No PickupScene assigned in the Inspector!");
			QueueFree();
			return;
		}

		Node2D pickup = (Node2D)PickupScene.Instantiate();

		pickup.GlobalPosition = GlobalPosition;

		GetParent().CallDeferred("add_child", pickup);

		QueueFree();
	}
}
