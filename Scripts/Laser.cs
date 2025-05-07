using Godot;
using System;

public partial class Laser : Node3D
{
	private float distance;
	private RayCast3D raycast;
	private Node3D scaler;

	public override void _Ready()
	{
		raycast = GetNode<RayCast3D>("RayCast");
		scaler = GetNode<Node3D>("Scaler");
	}

	public int DamageAmount = 5;
	private double timeSinceLastShot = 0.0;
	private double FireCooldown = 5.0; // seconds

	public override void _Process(double delta)
	{
		timeSinceLastShot += delta;

		if (raycast.GetCollider() != null)
		{
			Vector3 hitPoint = raycast.GetCollisionPoint();
			distance = GlobalTransform.Origin.DistanceTo(hitPoint);
			scaler.Scale = new Vector3(scaler.Scale.X, scaler.Scale.Y, distance / 2f);

			if (timeSinceLastShot >= FireCooldown)
			{
				Node collider = raycast.GetCollider() as Node;
				if (collider is Kaito player)
				{
					var health = player.GetNode<HealthComponent>("HealthComponent");
					health.TakeDamage(10);
					timeSinceLastShot = 0;
				}
			}
		}
		else
		{
			scaler.Scale = new Vector3(scaler.Scale.X, scaler.Scale.Y, raycast.TargetPosition.Z);
		}
	}
}