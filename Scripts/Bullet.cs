using Godot;

public partial class Bullet : Node3D
{
	public float Speed { get; set; } = 2500f;
	[Export] public float Damage { get; set; } = 10f;
	private const float LIFETIME = 2f;
	private const float COLLISION_DESTROY_DELAY = 1f;

	private Vector3 velocity = Vector3.Zero;
	private bool _hasHit = false;

	// Velocity inherited from the spawner. Set before adding to the scene.
	public Vector3 InheritedVelocity { get; set; } = Vector3.Zero;

	// Ship that fired this bullet for kill credit. Set before adding to the scene.
	public Node3D Source { get; set; }

	private MeshInstance3D mesh;
	private RayCast3D ray;
	private GpuParticles3D particles;

	public override void _Ready()
	{
		mesh = GetNode<MeshInstance3D>("MeshInstance3D");
		ray = GetNode<RayCast3D>("RayCast3D");
		particles = GetNode<GpuParticles3D>("GPUParticles3D");
		velocity = new Vector3(0, 0, -Speed);

		// Automatic self-destruction after lifetime expiry
		GetTree().CreateTimer(LIFETIME).Timeout += QueueFree;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_hasHit && ray.IsColliding())
		{
			_hasHit = true;
			mesh.Visible = false;
			particles.Emitting = true;
			velocity = Vector3.Zero;
			ray.Enabled = false;

			var collider = ray.GetCollider();

			if (collider is IDamageable damageable)
			{
				CollisionShape3D hitShape = null;
				if (collider is StaticBody3D body)
				{
					uint ownerId = body.ShapeFindOwner(ray.GetColliderShape());
					hitShape = body.ShapeOwnerGetOwner(ownerId) as CollisionShape3D;
				}
				damageable.TakeDamage(Damage, hitShape, Source);
			}
			GetTree().CreateTimer(COLLISION_DESTROY_DELAY).Timeout += QueueFree;
		}
		else
		{
			// Move bullet: local velocity (forward) + inherited world velocity from spawner
			Position += (Transform.Basis * velocity + InheritedVelocity) * (float)delta;
		}
	}
}
