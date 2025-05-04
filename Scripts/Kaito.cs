using Godot;
using System;

public partial class Kaito : CharacterBody3D
{
	private const float MAX_SPEED = 200.0f;
	private const float MAX_ANGULAR_SPEED = 2.0f;
	private const float ACCELERATION = 40.0f;
	private const float ROTATION_ACCELERATION = 2.0f;
	private const float MOUSE_SENSITIVITY = 0.1f;

	private Vector3 angularVelocity = Vector3.Zero;
	private Vector3 thrust = Vector3.Zero;
	private Vector3 torque = Vector3.Zero;

	private float pitchInput = 0.0f;
	private float yawInput = 0.0f;
	private float rollInput = 0.0f;

	private Node3D barrelRight;
	private Node3D barrelLeft;
	private AudioStreamPlayer3D startShooting;
	private AudioStreamPlayer3D shooting;
	private AudioStreamPlayer3D endShooting;
	private Light3D lightLeft;
	private Light3D lightRight;
	private Node3D dustParticles;
	
	private PackedScene bullet;

	public override void _Ready()
	{
		barrelRight = GetNode<Node3D>("BarrelRight");
		barrelLeft = GetNode<Node3D>("BarrelLeft");
		startShooting = GetNode<AudioStreamPlayer3D>("ShootingStart");
		shooting = GetNode<AudioStreamPlayer3D>("Shooting");
		endShooting = GetNode<AudioStreamPlayer3D>("ShootingEnd");
		lightLeft = GetNode<Light3D>("LeftLight");
		lightRight = GetNode<Light3D>("RightLight");
		dustParticles = GetNode<Node3D>("dustParticles");
		bullet = GD.Load<PackedScene>("res://Scenes/bullet.tscn");
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseEvent)
		{
			pitchInput = -mouseEvent.Relative.Y * MOUSE_SENSITIVITY;
			yawInput = -mouseEvent.Relative.X * MOUSE_SENSITIVITY;
		}
	}

	public override void _Process(double delta)
	{
		ApplyInputs((float)delta);
	}

	private void ApplyInputs(float delta)
	{
		thrust = Vector3.Zero;
		if (Input.IsActionPressed("thrust_forward"))
			thrust += Transform.Basis.X * ACCELERATION;
		if (Input.IsActionPressed("thrust_backward"))
			thrust -= Transform.Basis.X * ACCELERATION;
		if (Input.IsActionPressed("strafe_up"))
			thrust += Transform.Basis.Y * ACCELERATION;
		if (Input.IsActionPressed("strafe_down"))
			thrust -= Transform.Basis.Y * ACCELERATION;
		if (Input.IsActionPressed("strafe_right"))
			thrust += Transform.Basis.Z * ACCELERATION;
		if (Input.IsActionPressed("strafe_left"))
			thrust -= Transform.Basis.Z * ACCELERATION;

		torque = Vector3.Zero;
		rollInput = Input.GetActionStrength("roll_right") - Input.GetActionStrength("roll_left");
		torque.X = rollInput * ROTATION_ACCELERATION;
		torque.Y = yawInput * ROTATION_ACCELERATION;
		torque.Z = pitchInput * ROTATION_ACCELERATION;

		Velocity += thrust * delta;
		Velocity = Velocity.LimitLength(MAX_SPEED);

		angularVelocity += torque * delta;
		angularVelocity = angularVelocity.LimitLength(MAX_ANGULAR_SPEED);

		var collision = MoveAndCollide(Velocity * delta);
		if (collision != null)
			{
    		// Jednoduchý "bounce back" efekt
    		Velocity = Velocity.Bounce(collision.GetNormal()) * 0.2f;
		}

		RotateObjectLocal(Vector3.Right, angularVelocity.X * delta);
		RotateObjectLocal(Vector3.Up, angularVelocity.Y * delta);
		RotateObjectLocal(Vector3.Back, angularVelocity.Z * delta);
	}

	private void Shoot()
	{
		if (bullet == null) return;
		var instanceLeft = (Node3D)bullet.Instantiate();
		var instanceRight = (Node3D)bullet.Instantiate();
		
		instanceLeft.Position = barrelLeft.GlobalPosition;
		instanceRight.Position = barrelRight.GlobalPosition;
		
		instanceLeft.Transform = barrelLeft.GlobalTransform;
		instanceRight.Transform = barrelRight.GlobalTransform;
		
		GetParent().AddChild(instanceLeft);
		GetParent().AddChild(instanceRight);
	}

	private void GetInput(float delta)
	{
		if (Input.IsActionJustPressed("light"))
		{
			lightLeft.Visible = !lightLeft.Visible;
			lightRight.Visible = !lightRight.Visible;
		}

		if (Input.IsActionPressed("primary_fire"))
		{
			GetNode("TraumaCauser").Call("CauseTrauma");
			Shoot();
		}

		if (Input.IsActionJustPressed("primary_fire"))
			shooting.Play();

		if (Input.IsActionJustReleased("primary_fire"))
		{
			shooting.Stop();
			endShooting.Play();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		GetInput((float)delta);
	}
}
