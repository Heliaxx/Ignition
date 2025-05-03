using Godot;
using System;

public partial class Bullet : Node3D
{
	private const float SPEED = 400f;
	private const int DAMAGE = 10;
	private const float LIFETIME = 3f;
	private const float COLLISION_DESTROY_DELAY = 1f;

	private Vector3 velocity = Vector3.Zero;

	private MeshInstance3D mesh;
	private RayCast3D ray;
	private GpuParticles3D particles;

	public override void _Ready()
	{
		// Inicializace komponent
		mesh = GetNode<MeshInstance3D>("MeshInstance3D");
		ray = GetNode<RayCast3D>("RayCast3D");
		particles = GetNode<GpuParticles3D>("GPUParticles3D");

		// Nastavení směru střely
		velocity = new Vector3(0, 0, -SPEED);

		// Automatické zničení po LIFETIME sekundách, i když nic netrefí
		GetTree().CreateTimer(LIFETIME).Timeout += QueueFree;
	}

	public override void _PhysicsProcess(double delta)
	{
		//Position += Transform.Basis * velocity * (float)delta;
		if (ray.IsColliding())
		{
			// Vizuálně "zmizí", ale efekty se spustí
			mesh.Visible = false;
			particles.Emitting = true;
			velocity = Vector3.Zero;
			ray.Enabled = false;

			var collider = ray.GetCollider();

			if (collider is Node colliderNode && colliderNode.IsInGroup("enemies"))
			{
				if (colliderNode.HasMethod("Hit"))
					colliderNode.Call("Hit", DAMAGE);
			}
			GetTree().CreateTimer(COLLISION_DESTROY_DELAY).Timeout += QueueFree;
		}
		else
		{
			Position += Transform.Basis * velocity * (float)delta;
		}
	}
}
