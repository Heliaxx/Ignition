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

	[Export] public float DamageAmount = 10f;
	private double timeSinceLastShot = 0.0;
	[Export] public double FireCooldown = 5.0;

	public override void _Process(double delta)
	{
		// Skip all processing when laser is hidden (not firing)
		if (!Visible) return;

		timeSinceLastShot += delta;

		if (raycast.GetCollider() != null)
		{
			Vector3 hitPoint = raycast.GetCollisionPoint();
			distance = GlobalTransform.Origin.DistanceTo(hitPoint);
			scaler.Scale = new Vector3(scaler.Scale.X, scaler.Scale.Y, distance / 2f);

			if (timeSinceLastShot >= FireCooldown)
			{
				if (raycast.GetCollider() is IDamageable damageable)
				{
					damageable.TakeDamage(DamageAmount);
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
