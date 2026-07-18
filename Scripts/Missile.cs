using Godot;

public partial class Missile : Node3D
{
	public enum ChaseMode
	{
		Pursuit,                // steer toward the predicted intercept point
		ProportionalNavigation, // constant-bearing guidance; falls back to Pursuit when not facing the target
	}

	[Export] public ChaseMode Guidance = ChaseMode.Pursuit;
	[Export] public float PnGain = 4f; // aggressivity of Proportional Navigation corrections

	[Export] public float MaxSpeed = 400f;
	[Export] public float Acceleration = 200f; // speed increase per second
	[Export] public float MaxTurnRate = 45f; // degrees per second
	[Export] public float Damage = 80f;
	[Export] public float BurnTime = 5f; // seconds of guided flight
	[Export] public float Lifetime = 10f;
	[Export] public bool FadeOutOnDeath = true; // glow fades out before lifetime timeout instead of popping
	[Export] public float FadeOutDuration = 1.5f; // seconds of glow fade before lifetime timeout
	[Export] public float ProximityRadius = 10f; // detonate if within this distance of target

	// Set before adding to the scene.
	public GimbalTarget Target { get; set; }
	public Vector3 InheritedVelocity { get; set; } = Vector3.Zero;

	private float _currentSpeed;
	private float _burnTimer = 0f;
	private float _lifeTimer = 0f;
	private bool _hasDetonated = false;
	private ShaderMaterial _meshMaterial;

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

		// Duplicate the mesh material per-instance so driving life_fade on this
		// missile doesn't affect every other missile sharing the scene's material.
		// Only needed when the end-of-life fade is enabled.
		if (FadeOutOnDeath && _mesh?.GetActiveMaterial(0) is ShaderMaterial shared)
		{
			_meshMaterial = (ShaderMaterial)shared.Duplicate();
			_mesh.SetSurfaceOverrideMaterial(0, _meshMaterial);
		}

		_currentSpeed = InheritedVelocity.Length();

		GetTree().CreateTimer(Lifetime).Timeout += QueueFree;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_hasDetonated) return;

		float dt = (float)delta;
		_burnTimer += dt;
		_lifeTimer += dt;

		// Smoothly fade the glow out over the final FadeOutDuration seconds before
		// the lifetime timeout. Detonation bypasses this (it returns early above).
		if (FadeOutOnDeath && _meshMaterial != null && FadeOutDuration > 0f)
		{
			float remaining = Lifetime - _lifeTimer;
			float lifeFade = Mathf.Clamp(remaining / FadeOutDuration, 0f, 1f);
			_meshMaterial.SetShaderParameter("life_fade", lifeFade);
		}

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
		Vector3 currentForward = (-GlobalTransform.Basis.Z).Normalized();
		Vector3 targetPos = Target.GlobalPosition;
		Vector3 targetVel = Target.GetVelocity();

		Vector3 desiredDir = ComputeGuidanceDir(currentForward, targetPos, targetVel);

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

	// Desired flight direction for the current guidance mode.
	private Vector3 ComputeGuidanceDir(Vector3 currentForward, Vector3 targetPos, Vector3 targetVel)
	{
		if (Guidance == ChaseMode.ProportionalNavigation)
		{
			Vector3 toTarget = (targetPos - GlobalPosition).Normalized();
			if (toTarget.Dot(currentForward) > 0.1f)
			{
				Vector3 missileVel = currentForward * _currentSpeed;
				Vector3 command = AimUtils.ProportionalNavigation(
					GlobalPosition, missileVel, targetPos, targetVel, PnGain);
				return (toTarget + command / Mathf.Max(_currentSpeed, 1f)).Normalized();
			}
		}

		// Pursuit: steer toward the predicted intercept point.
		Vector3 interceptPos = AimUtils.PredictIntercept(
			GlobalPosition, targetPos, targetVel, _currentSpeed);
		return (interceptPos - GlobalPosition).Normalized();
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
