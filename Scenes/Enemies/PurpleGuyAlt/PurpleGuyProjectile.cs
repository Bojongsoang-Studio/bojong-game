using Godot;

namespace BojongGame.Scenes.Enemies.PurpleGuyAlt;

public partial class PurpleGuyProjectile : Area2D
{
	[Export] public float LifeTime = 2.2f;

	private int _dir = 1;
	private float _speed = 520f;
	private int _damage = 1;
	private float _life;

	public override void _Ready()
	{
		_life = LifeTime;
		BodyEntered += OnBodyEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		var dt = (float)delta;

		GlobalPosition += new Vector2(_dir * _speed * dt, 0f);

		_life -= dt;
		if (_life <= 0f)
			QueueFree();
	}

	public void Setup(int direction, float speed, int damage)
	{
		_dir = direction >= 0 ? 1 : -1;
		_speed = speed;
		_damage = damage;

		Scale = new Vector2(_dir, 1);
	}

	private void OnBodyEntered(Node body)
	{
		switch (body)
		{
			case Player.Player player:
				player.TakeHit(_damage, new Vector2(_dir * 260f, -120f));
				QueueFree();
				return;
			case StaticBody2D or TileMapLayer:
				QueueFree();
				break;
		}
	}
}
