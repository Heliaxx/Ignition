using Godot;

public partial class Missile : Node3D
{
	[Export] public float MaxSpeed = 400f;
	[Export] public float Acceleration = 200f; // speed increase per second
	[Export] public float MaxTurnRate = 45f; // degrees per second
	[Export] public float Damage = 80f;
	[Export] public float BurnTime = 5f; // seconds of guided flight
	[Export] public float Lifetime = 10f;
	[Export] public float ProximityRadius = 10f; // detonate if within this distance of target

	// Set before adding to the scene.
	public GimbalTarget Target { get; set; }
	public Vector3 InheritedVelocity { get; set; } = Vector3.Zero;

	private float _currentSpeed;
	private float _burnTimer = 0f;
	private bool _hasDetonated = false;

	private RayCast3D _ray;
	private GpuParticles3D _exhaustParticles;
	private GpuParticles3D _trailParticles;
	private GpuParticles3D _explosionParticles;
	private MeshInstance3D _mesh;

	private bool TargetAlive => Target != null && IsInstanceValid(Target) && Target.IsValid();
	private bool IsGuided => _burnTimer < BurnTime && TargetAlive;

	public override void _Ready()
	{
		_ray = GetNodeOrNull<RayCast3D>("RayCast3D");
		_exhaustParticles = GetNodeOrNull<GpuParticles3D>("ExhaustParticles");
		_trailParticles = GetNodeOrNull<GpuParticles3D>("TrailParticles");
		_explosionParticles = GetNodeOrNull<GpuParticles3D>("ExplosionParticles");
		_mesh = GetNodeOrNull<MeshInstance3D>("MeshInstance3D");

		_currentSpeed = InheritedVelocity.Length();

		GetTree().CreateTimer(Lifetime).Timeout += QueueFree;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_hasDetonated) return;

		float dt = (float)delta;
		_burnTimer += dt;

		_currentSpeed = Mathf.MoveToward(_currentSpeed, MaxSpeed, Acceleration * dt);

		if (IsGuided)
			SteerTowardTarget(dt);

		// Proximity detonation
		if (TargetAlive && GlobalPosition.DistanceTo(Target.GlobalPosition) < ProximityRadius)
		{
			Detonate(Target.TargetOwner as IDamageable);
			return;
		}

		// Raycast collision
		if (_ray != null && _ray.IsColliding())
		{
			var collider = _ray.GetCollider();
			CollisionShape3D hitShape = null;
			if (collider is StaticBody3D body)
			{
				uint ownerId = body.ShapeFindOwner(_ray.GetColliderShape());
				hitShape = body.ShapeOwnerGetOwner(ownerId) as CollisionShape3D;
			}
			Detonate(collider as IDamageable, hitShape);
			return;
		}

		GlobalPosition += (-GlobalTransform.Basis.Z).Normalized() * _currentSpeed * dt;
	}

	private void SteerTowardTarget(float dt)
	{
		Vector3 interceptPos = AimUtils.PredictIntercept(
			GlobalPosition, Target.GlobalPosition, Target.GetVelocity(), _currentSpeed);

		Vector3 desiredDir = (interceptPos - GlobalPosition).Normalized();
		Vector3 currentForward = (-GlobalTransform.Basis.Z).Normalized();

		float angleToDesired = currentForward.AngleTo(desiredDir);
		if (angleToDesired < 0.001f) return;

		// Rotate toward the intercept direction, capped at the max turn rate.
		float maxStep = Mathf.DegToRad(MaxTurnRate) * dt;
		Vector3 newForward = currentForward.Slerp(desiredDir, Mathf.Min(maxStep / angleToDesired, 1f)).Normalized();

		Vector3 up = GlobalTransform.Basis.Y;
		if (Mathf.Abs(newForward.Dot(up)) > 0.99f)
			up = GlobalTransform.Basis.X;

		GlobalTransform = new Transform3D(Basis.LookingAt(newForward, up), GlobalTransform.Origin);
	}

	private void Detonate(IDamageable target = null, CollisionShape3D hitShape = null)
	{
		if (_hasDetonated) return;
		_hasDetonated = true;

		target?.TakeDamage(Damage, hitShape);

		if (_mesh != null) _mesh.Visible = false;
		if (_exhaustParticles != null) _exhaustParticles.Emitting = false;
		if (_explosionParticles != null) _explosionParticles.Emitting = true;
		if (_ray != null) _ray.Enabled = false;

		// Detach trail so it lingers even after the missile is gone
		if (_trailParticles != null)
		{
			_trailParticles.Emitting = false;
			var parent = GetParent();
			if (parent != null)
			{
				RemoveChild(_trailParticles);
				parent.AddChild(_trailParticles);
				_trailParticles.GlobalPosition = GlobalPosition;
				GetTree().CreateTimer(_trailParticles.Lifetime).Timeout += _trailParticles.QueueFree;
			}
		}

		GetTree().CreateTimer(2f).Timeout += QueueFree;
	}
}
