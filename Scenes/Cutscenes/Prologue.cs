using Godot;
using System;

public partial class Prologue : Node2D
{

	[Export]
	public PackedScene Level1Scene;

	public override void _Ready()
	{
		var animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

		animPlayer.AnimationFinished += OnAnimationFinished;

		animPlayer.Play("Cutscene");
	}

	private void OnAnimationFinished(StringName animName)
	{

		if (Level1Scene == null)
		{
			GD.PrintErr("HEY! You forgot to drag the Level 1 scene into the Inspector!");
			return;
		}

		GetTree().ChangeSceneToPacked(Level1Scene);
	}
	
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_accept"))
		{
			 if (Level1Scene != null) GetTree().ChangeSceneToPacked(Level1Scene);
		}
	}
}
