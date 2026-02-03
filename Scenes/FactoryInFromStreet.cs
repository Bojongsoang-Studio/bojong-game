using Godot;

namespace BojongGame.Scenes;

public partial class FactoryInFromStreet : Area2D
{
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
			_player.GlobalPosition = GetParent().GetNode<Area2D>("StreetInFromFactory")
				.GetNode<CollisionShape2D>("CollisionShape2D").GlobalPosition;
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player.Player player) return;
		_player = player;
		player.ShowGuide();
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is not Player.Player player || _player != player) return;
		_player = null;
		player.HideGuide();
	}
}
