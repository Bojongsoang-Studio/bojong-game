using Godot;

namespace BojongGame.Scenes.Areas.Street
{
	public partial class ZebraCross1 : Area2D
	{
		public override void _Ready()
		{
			BodyEntered += OnBodyEntered;
			BodyExited += OnBodyExited;
		}

		private static void OnBodyEntered(Node2D body)
		{
			if (body is Player.Player player)
			{
				player.EnterZebraCross();
			}
		}

		private static void OnBodyExited(Node2D body)
		{
			if (body is Player.Player player)
			{
				player.ExitZebraCross();
			}
		}
	}
}
