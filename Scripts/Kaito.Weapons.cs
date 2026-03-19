using Godot;

public partial class Kaito
{
	[ExportGroup("Weapon")]
	[Export] public bool UnlimitedAmmo = false;
	[Export] public int MaxAmmo = 30;
	[Export] public float BulletSpeed = 1600f;
	[Export] public float FireRate = 10f; // shots per second
	[Export] public float GimbalAngle = 10.0f; // degrees — max weapon deflection from ship forward
	[Export] public float GimbalTrackingSpeed = 5.0f; // How fast the gimbal converges on target (deg/s-like factor)

	private int _currentAmmo;
	public int CurrentAmmo => _currentAmmo;

	private Node3D barrel;
	private AudioStreamPlayer3D startShooting;
	private AudioStreamPlayer3D shooting;
	private AudioStreamPlayer3D endShooting;
	private PackedScene bullet;
	private double timeSinceLastShot = 0.0;
	private double fireCooldown; // seconds — computed from FireRate in _Ready()
	private Label _ammoLabel;
	private Vector3 _currentGimbalDir = Vector3.Forward; // smoothed gimbal aim direction (local space)

	private void InitWeapons()
	{
		barrel = GetNode<Node3D>("Barrel");
		startShooting = GetNode<AudioStreamPlayer3D>("ShootingStart");
		shooting = GetNode<AudioStreamPlayer3D>("Shooting");
		endShooting = GetNode<AudioStreamPlayer3D>("ShootingEnd");
		bullet = GD.Load<PackedScene>("res://Scenes/bullet.tscn");
		fireCooldown = 1.0 / FireRate;
		timeSinceLastShot = fireCooldown; // allow immediate first shot
		_currentAmmo = MaxAmmo;
		_ammoLabel = canvasLayer.GetNode<Label>("Panel/MarginContainer/VBoxContainer/HBoxContainer3/AmmoCount");
		UpdateAmmoHUD();
	}

	private void UpdateAmmoHUD()
	{
		if (_ammoLabel == null) return;
		_ammoLabel.Text = UnlimitedAmmo ? "∞" : $"{_currentAmmo}/{MaxAmmo}";
	}

	private void Shoot()
	{
		if (bullet == null) return;
		if (!UnlimitedAmmo && _currentAmmo <= 0) return;

		var instance = bullet.Instantiate<Bullet>();
		instance.Transform = GetGimbaledBarrelTransform();
		instance.InheritedVelocity = Velocity;
		instance.Speed = BulletSpeed;
		GetParent().AddChild(instance);

		if (!UnlimitedAmmo)
		{
			_currentAmmo--;
			UpdateAmmoHUD();
			if (_currentAmmo <= 0)
			{
				shooting.Stop();
				endShooting.Play();
			}
		}
	}

	// ── Gimbal Tracking ──────────────────────────────────────────

	/// <summary>
	/// Computes the ideal (instant) gimbal aim direction in world space.
	/// Returns null if no target or target is invalid.
	/// </summary>
	private Vector3? GetIdealGimbalDirection()
	{
		if (_lockedTarget == null || !IsInstanceValid(_lockedTarget))
			return null;

		Transform3D t = barrel.GlobalTransform;
		Vector3 forward = (-t.Basis.Z).Normalized();

		// Compute lead position
		Vector3 targetPos = _lockedTarget.GlobalPosition;
		Vector3 targetVel = _lockedTarget.GetVelocity();
		Vector3 shooterPos = GlobalPosition;
		float bulletWorldSpeed = BulletSpeed + Mathf.Max(0, Velocity.Dot((-GlobalTransform.Basis.Z).Normalized()));

		float dist = shooterPos.DistanceTo(targetPos);
		Vector3 leadPos = targetPos;
		for (int i = 0; i < 2; i++)
		{
			float tof = dist / bulletWorldSpeed;
			leadPos = targetPos + targetVel * tof;
			dist = shooterPos.DistanceTo(leadPos);
		}

		Vector3 toTarget = (leadPos - t.Origin).Normalized();

		// Clamp to gimbal cone
		float angle = forward.AngleTo(toTarget);
		float gimbalRad = Mathf.DegToRad(GimbalAngle);
		if (angle > gimbalRad)
		{
			Vector3 axis = forward.Cross(toTarget);
			if (axis.LengthSquared() < 0.0001f)
				return null; // target is directly behind
			axis = axis.Normalized();
			return forward.Rotated(axis, gimbalRad).Normalized();
		}

		return toTarget;
	}

	private void UpdateGimbalTracking(float delta)
	{
		Transform3D t = barrel.GlobalTransform;
		Vector3 forward = (-t.Basis.Z).Normalized();

		Vector3? ideal = GetIdealGimbalDirection();

		// Convert current gimbal dir from local to world
		Vector3 currentWorld = t.Basis * _currentGimbalDir;

		Vector3 targetDir;
		if (ideal.HasValue)
		{
			targetDir = ideal.Value;
		}
		else
		{
			// No target — return to forward
			targetDir = forward;
		}

		// Slerp toward ideal direction
		float t_factor = 1f - Mathf.Exp(-GimbalTrackingSpeed * delta);
		float angleBetween = currentWorld.AngleTo(targetDir);

		Vector3 newDir;
		if (angleBetween < 0.001f)
		{
			newDir = targetDir;
		}
		else
		{
			// Spherical lerp via angle-axis rotation
			Vector3 axis = currentWorld.Cross(targetDir);
			if (axis.LengthSquared() < 0.0001f)
			{
				newDir = targetDir;
			}
			else
			{
				axis = axis.Normalized();
				float rotAmount = angleBetween * t_factor;
				newDir = currentWorld.Rotated(axis, rotAmount).Normalized();
			}
		}

		// Clamp to gimbal cone
		float gimbalRad = Mathf.DegToRad(GimbalAngle);
		float newAngle = forward.AngleTo(newDir);
		if (newAngle > gimbalRad)
		{
			Vector3 clampAxis = forward.Cross(newDir);
			if (clampAxis.LengthSquared() >= 0.0001f)
			{
				clampAxis = clampAxis.Normalized();
				newDir = forward.Rotated(clampAxis, gimbalRad).Normalized();
			}
			else
			{
				newDir = forward;
			}
		}

		// Store back in local space
		_currentGimbalDir = (t.Basis.Inverse() * newDir).Normalized();
	}

	private Transform3D GetGimbaledBarrelTransform()
	{
		Transform3D t = barrel.GlobalTransform;
		Vector3 forward = (-t.Basis.Z).Normalized();

		// Convert smoothed gimbal direction from local to world
		Vector3 aimDir = (t.Basis * _currentGimbalDir).Normalized();

		float angle = forward.AngleTo(aimDir);
		if (angle < 0.00001f)
			return t;

		Vector3 rotAxis = forward.Cross(aimDir);
		if (rotAxis.LengthSquared() < 0.0001f)
			return t;
		rotAxis = rotAxis.Normalized();

		Basis rotated = t.Basis.Rotated(rotAxis, angle);
		return new Transform3D(rotated, t.Origin);
	}

	public float GetGimbalScreenRadius()
	{
		var camera = GetViewport().GetCamera3D();
		if (camera == null) return 0f;
		float fovRad = Mathf.DegToRad(camera.Fov);
		float screenHeight = GetViewport().GetVisibleRect().Size.Y;
		float gimbalRad = Mathf.DegToRad(GimbalAngle);
		return Mathf.Tan(gimbalRad) / Mathf.Tan(fovRad / 2f) * (screenHeight / 2f);
	}

	public Vector2? GetGimbalAimScreenPos()
	{
		var camera = GetViewport().GetCamera3D();
		if (camera == null) return null;

		// Project a point along the gimbaled barrel direction
		Transform3D gimbaled = GetGimbaledBarrelTransform();
		Vector3 aimPoint = gimbaled.Origin + (-gimbaled.Basis.Z).Normalized() * 500f;

		if (camera.IsPositionBehind(aimPoint))
			return null;

		return camera.UnprojectPosition(aimPoint);
	}
}
