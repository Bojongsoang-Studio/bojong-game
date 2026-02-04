using System;
using Godot;

namespace BojongGame.Scenes.Areas.Street.Animals;

public partial class Cat : Node2D
{
	[Export] public int Speed = 60;
	[Export] public double IdleTime = 1.0;

	private int _direction = 1;
	private int _lastDirection = 1;
	private double _idleTime;

	private AnimatedSprite2D _sprite;
	private RayCast2D _rightRayCast;
	private RayCast2D _leftRayCast;

	private static readonly Random Random = new();

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("Sprite");
		_rightRayCast = GetNode<RayCast2D>("RightRayCast");
		_leftRayCast = GetNode<RayCast2D>("LeftRayCast");
	}

	public override void _Process(double delta)
	{
		var position = Position;

		if (_rightRayCast.IsColliding() && _direction > 0)
		{
			_direction = -1;
			_lastDirection = _direction;
			_sprite.FlipH = true;
		}

		if (_leftRayCast.IsColliding() && _direction < 0)
		{
			_direction = 1;
			_lastDirection = _direction;
			_sprite.FlipH = false;
		}

		if (_direction != 0 && Random.Next(1, 101) == 5)
		{
			_lastDirection = _direction;
			_direction = 0;
			_idleTime = IdleTime;
		}

		_sprite.Animation = _direction == 0 ? "idle" : "walk";

		if (_idleTime > 0)
		{
			_idleTime -= delta;
		}
		else
		{
			if (_direction == 0)
			{
				_direction = _lastDirection;
			}

			position.X += _direction * Speed * (float)delta;
			Position = position;
		}
	}
}
