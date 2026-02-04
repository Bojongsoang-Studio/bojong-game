using Godot;
// Alias for your Player class
using PlayerClass = BojongGame.Scenes.Player.Player;

namespace BojongGame.Scenes.Areas.Forest;

public partial class GreenFlyingRobot : CharacterBody2D
{
	[Export] public float Speed = 80.0f; // Slower speed for hovering feels better
	[Export] public int DamageAmount = 1;
	[Export] public float KnockbackForce = 300.0f;
	[Export] public float HoverHeight = 150.0f; // How high above the player to fly

	private AnimatedSprite2D _sprite;
	private RayCast2D _laserRay;
	private Node2D _playerTarget;

	private bool _isAttacking = false;
	private float _bobOffset = 0f;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_laserRay = GetNode<RayCast2D>("LaserRay"); // <--- GET THE RAYCAST

		// Setup Hitbox (Contact Damage - if player jumps into drone)
		var hitbox = GetNode<Area2D>("Hitbox");
		hitbox.BodyEntered += OnHitboxBodyEntered;

		// Setup Detection (Finding Player)
		var detection = GetNode<Area2D>("DetectionArea");
		detection.BodyEntered += (body) =>
		{
			if (body is PlayerClass p)
			{
				GD.Print("TARGET ACQUIRED");
				_playerTarget = p;
			}
		};
		detection.BodyExited += (body) => { if (body == _playerTarget) _playerTarget = null; };

		// Connect animation signal
		_sprite.AnimationFinished += OnAnimationFinished;
		_sprite.Play("fly");

		// Safety: Ignore Player Body collisions
		SetCollisionLayerValue(3, true);
		SetCollisionMaskValue(1, true);
		SetCollisionMaskValue(2, false);
	}

	public override void _PhysicsProcess(double delta)
	{
		// 1. IF ATTACKING, FREEZE!
		if (_isAttacking)
		{
			Velocity = Velocity.MoveToward(Vector2.Zero, 100 * (float)delta);
			MoveAndSlide();
			return;
		}

		Vector2 velocity = Vector2.Zero;

		if (_playerTarget != null)
		{

			// 1. Calculate the "Hover Spot" (Directly above player)
			Vector2 targetPos = _playerTarget.GlobalPosition;
			targetPos.Y -= HoverHeight; // Aim for the sky above player

			// 2. Move towards that hover spot
			Vector2 direction = (targetPos - GlobalPosition).Normalized();
			velocity = direction * Speed;

			// 3. Face the player
			if (_playerTarget.GlobalPosition.X > GlobalPosition.X)
				_sprite.FlipH = false; // Face Right
			else
				_sprite.FlipH = true;  // Face Left

			// 4. CHECK LASER SIGHT
			// If the raycast hits something AND that something is the player...
			if (_laserRay.IsColliding())
			{
				var hitObject = _laserRay.GetCollider();
				if (hitObject is PlayerClass playerToZap)
				{
					FireLaser(playerToZap);
				}
			}
		}
		else
		{
			// --- IDLE BOBBING ---
			_bobOffset += (float)delta * 5.0f;
			velocity = Velocity.MoveToward(Vector2.Zero, 200 * (float)delta);
			_sprite.Offset = new Vector2(0, Mathf.Sin(_bobOffset) * 5);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void FireLaser(PlayerClass player)
	{
		GD.Print("ZAP!");
		_isAttacking = true;
		_sprite.Play("attack"); // Play the laser animation

		// Deal Damage IMMEDIATELY (Zap!)
		// Since the laser pushes DOWN, knockback should be DOWN
		Vector2 laserKnockback = Vector2.Down * KnockbackForce;
		player.TakeHit(DamageAmount, laserKnockback);
	}

	private void OnHitboxBodyEntered(Node2D body)
	{
		// Keep this for "Body Contact" damage (if player jumps into the drone)
		if (_isAttacking) return;

		if (body is PlayerClass player)
		{
			Vector2 pushDir = (player.GlobalPosition - GlobalPosition).Normalized();
			player.TakeHit(DamageAmount, pushDir * KnockbackForce);
		}
	}

	private void OnAnimationFinished()
	{
		// Go back to flying after zap is done
		if (_sprite.Animation == "attack")
		{
			_isAttacking = false;
			_sprite.Play("fly");
		}
	}
}
 