using Godot;
namespace BojongGame.Scenes.Areas.Forest;
public partial class Door : StaticBody2D
{
	// Drag your "Opened Door" image here in the Inspector
	[Export]
	public Texture2D OpenTexture;

	private Sprite2D _sprite;
	private CollisionShape2D _collision;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_collision = GetNode<CollisionShape2D>("CollisionShape2D");
	}

	public void Open()
	{
		// 1. Swap the image to the open version
		if (OpenTexture != null)
		{
			_sprite.Texture = OpenTexture;
		}

		// 2. Turn off the physical wall so player can walk through
		// SetDeferred is safer for physics properties during gameplay
		_collision.SetDeferred("disabled", true);
	}
}
