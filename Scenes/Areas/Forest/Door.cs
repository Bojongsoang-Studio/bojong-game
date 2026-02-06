using Godot;

namespace BojongGame.Scenes.Areas.Forest;

public partial class Door : StaticBody2D
{
    [Export] public Texture2D OpenTexture;

    private Sprite2D _sprite;
    private CollisionShape2D _collision;
    private Area2D _area;
    private Label _label;

    private bool _isOpen;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite2D");
        _collision = GetNode<CollisionShape2D>("CollisionShape2D");
        _area = GetNode<Area2D>("Area2D");
        _label = GetNode<Label>("PopupDialogLabel");

        _area.BodyEntered += OnBodyEntered;
        _area.BodyExited += OnBodyExited;
    }

    public void Open()
    {
        if (OpenTexture != null)
        {
            _sprite.Texture = OpenTexture;
        }

        _collision.SetDeferred("disabled", true);
        _isOpen = true;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player.Player && !_isOpen)
        {
            _label.Visible = true;
        }
    }

    private void OnBodyExited(Node2D _)
    {
        _label.Visible = false;
    }
}