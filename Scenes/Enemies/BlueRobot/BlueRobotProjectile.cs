using Godot;

namespace BojongGame.Scenes.Enemies.BlueRobot;

public partial class BlueRobotProjectile : Area2D
{
	[Export] public float LifeTime = 2.5f;

	private int _dir = 1;
	private float _speed = 420f;
	private int _damage = 1;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		var dt = (float)delta;
		GlobalPosition += new Vector2(_dir * _speed * dt, 0f);

		LifeTime -= dt;
		if (LifeTime <= 0f) QueueFree();
	}

	public void Setup(int direction, float speed, int damage)
	{
		_dir = direction == 0 ? 1 : direction;
		_speed = speed;
		_damage = damage;
	}

	private void OnBodyEntered(Node body)
	{
		if (body is Player.Player player)
		{
			player.TakeHit(_damage, new Vector2(_dir * 260f, -120f));
			QueueFree();
		}
		else if (body is StaticBody2D || body is TileMap)
		{
			QueueFree();
		}
	}
}
