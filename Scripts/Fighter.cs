using Godot;
using System;

public partial class Fighter : CharacterBody3D
{
	private const int MAX_HEALTH = 100;
	private int currentHealth = MAX_HEALTH;

	[Signal]
	public delegate void DiedEventHandler();
	[Export]
	public float Speed = 100.0f;

	[Export]
	public float ClosestDistance = 100.0f;

	[Export]
	public float FarthestDistance = 500.0f;

	[Export]
	public float Acceleration = 10.0f;

	[Export]
	public float TurnSpeed = 2.0f;

	private Node3D player;

	public override void _Ready()
	{
		currentHealth = MAX_HEALTH;
		player = GetNode<Node3D>("../Player");
		if (!HasTarget())
		{
			GD.PrintErr("Player not found in scene.");
		}   
	}

	public bool HasTarget()
	{
		return player != null;
	}

	public float DistanceToPlayer()
	{
		return (player.GlobalPosition - GlobalPosition).Length();
	}

	public void MoveTowardPlayer(float delta)
	{
		Vector3 toPlayer = player.GlobalPosition - GlobalPosition;
		Vector3 desiredVelocity = toPlayer.Normalized() * Speed;

		Velocity = Velocity.Lerp(desiredVelocity, Acceleration * delta);
		MoveAndSlide();
		FaceDirection(toPlayer, delta);
	}

	public void MoveAwayFromPlayer(float delta)
	{
	Vector3 awayFromPlayer = (GlobalPosition - player.GlobalPosition).Normalized();
	Vector3 desiredVelocity = awayFromPlayer * Speed;

	Velocity = Velocity.Lerp(desiredVelocity, Acceleration * delta);
	MoveAndSlide();
	FaceDirection(awayFromPlayer, delta);
	}

	public void FacePlayer(float delta)
	{
		Vector3 toPlayer = player.GlobalPosition - GlobalPosition;
		Vector3 targetDirection = toPlayer.Normalized();
		Vector3 currentForward = -GlobalTransform.Basis.Z;
		Vector3 newForward = currentForward.Lerp(targetDirection, TurnSpeed * delta).Normalized();

		Basis newBasis = Basis.LookingAt(newForward, Vector3.Up);
		GlobalTransform = new Transform3D(newBasis, GlobalTransform.Origin);
	}

	public void MoveInDirection(Vector3 direction, float delta)
	{
		Vector3 desiredVelocity = direction.Normalized() * Speed;
		Velocity = Velocity.Lerp(desiredVelocity, Acceleration * delta);
		MoveAndSlide();
	}

	public void FaceDirection(Vector3 direction, float delta)
	{
		Vector3 currentForward = -GlobalTransform.Basis.Z;
		Vector3 newForward = currentForward.Lerp(direction.Normalized(), TurnSpeed * delta).Normalized();
		Basis newBasis = Basis.LookingAt(newForward, Vector3.Up);
		GlobalTransform = new Transform3D(newBasis, GlobalTransform.Origin);
	}

	public void StopMoving()
	{
		Velocity = Vector3.Zero;
		MoveAndSlide();
	}

	public void Hit(int damage = 10)
	{
		currentHealth -= damage;
		GD.Print($"{Name} hit! Health: {currentHealth}");

		if (currentHealth <= 0)
		{
			Die();
		}
	}

	private void Die()
	{
		GD.Print($"{Name} destroyed!");
		EmitSignal(SignalName.Died);
		QueueFree();
	}
}