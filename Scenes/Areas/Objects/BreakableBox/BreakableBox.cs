using Godot;

namespace BojongGame.Scenes.Areas.Objects.BreakableBox;

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

		var pickup = (Node2D)PickupScene.Instantiate();

		pickup.GlobalPosition = GlobalPosition;

		GetParent().CallDeferred("add_child", pickup);

		QueueFree();
	}
}
