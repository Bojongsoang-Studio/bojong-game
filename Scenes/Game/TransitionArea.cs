using Godot;

namespace BojongGame.Scenes.Game;

public partial class TransitionArea : Area2D
{
	[Export] public Node2D TransitionNode;
	
	private Player.Player _player;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_player == null) return;
		if (Input.IsActionJustPressed("enter"))
		{
			_player.GlobalPosition = TransitionNode.GlobalPosition;
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player.Player player) return;
		_player = player;
		player.DisplayTransitionGuide(true);
		player.SetSpawnPoint(GetNode<CollisionShape2D>("CollisionShape2D").GlobalPosition);
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is not Player.Player player || _player != player) return;
		_player = null;
		player.DisplayTransitionGuide(false);
	}
}
