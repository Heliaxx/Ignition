using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Kaito : CharacterBody3D, IDamageable
{
	private const float MAX_SPEED = 200.0f;
	private const float MAX_ROLL_SPEED = 2.0f;
	private const float ROLL_ACCELERATION = 2.5f;

	private const float MAX_PITCH_SPEED = 4.0f;
	private const float PITCH_ACCELERATION = 6f;

	private const float MAX_YAW_SPEED = 2.0f;
	private const float YAW_ACCELERATION = 6.0f;

	private const float ACCELERATION = 40.0f;
	private const float MOUSE_SENSITIVITY = 0.18f;

	[ExportGroup("Aim")]
	[Export] public float AimRadius = 100.0f;
	[Export] public float AimDeadzone = 0.05f;
	[Export] public float AimSensitivity = 1.2f;
	[Export] public bool AutoCenterCursor = true;
	[Export] public float AutoCenterDelay = 0f;
	[Export] public float AutoCenterSpeed = 8.0f;
	[ExportSubgroup("Dust Effects")]
	[Export] public float DustAlignSmoothing = 60.0f;
	[Export] public float DustSpawnStartSpeed = 20.0f;
	[Export] public float DustSpawnFullSpeed = 120.0f;
	[Export] public float DustMaxAmountRatio = 1.0f;

	[ExportGroup("Collision")]
	[Export] public float CollisionLinearDamping = 0.9f;
	[Export] public float CollisionAngularDamping = 0.75f;
	[Export] public float CollisionPushOutDistance = 0.1f;
	[Export] public float CollisionDamageSpeedThreshold = 50.0f;
	[Export] public float CollisionDamageMultiplier = 0.5f;

	private float aimTargetYaw = 0.0f;
	private float aimTargetPitch = 0.0f;
	private float aimYaw = 0.0f;
	private float aimPitch = 0.0f;
	private const float AIM_RESPONSIVENESS = 18.0f;

	private Vector2 widgetCursor = Vector2.Zero;
	private bool widgetCursorInitialized = false;
	private double timeSinceLastMouseInput = 0.0;
	public Vector2 GetWidgetCursorPos() => widgetCursor;

	private Vector3 angularVelocity = Vector3.Zero;
	private Vector3 thrust = Vector3.Zero;
	private Vector3 torque = Vector3.Zero;

	private float pitchInput = 0.0f;
	private float yawInput = 0.0f;
	private float rollInput = 0.0f;

	// This tick's intent; the flight model reads this, never Input directly.
	private ShipInput _input;

	private bool _justUnpaused = false;
	private float _currentMaxSpeed = MAX_SPEED;
	private float _currentAcceleration = ACCELERATION;
	private float _currentRollAcceleration = ROLL_ACCELERATION;
	private float _currentPitchAcceleration = PITCH_ACCELERATION;
	private float _currentYawAcceleration = YAW_ACCELERATION;
	private float _currentMaxRollSpeed = MAX_ROLL_SPEED;
	private float _currentMaxPitchSpeed = MAX_PITCH_SPEED;
	private float _currentMaxYawSpeed = MAX_YAW_SPEED;

	private Light3D lightLeft;
	private Light3D lightRight;
	private Node3D dustParticles;
	private GpuParticles3D dustParticlesGpu;
	private CanvasLayer canvasLayer;
	private CanvasLayer _scoreHud;
	private Camera3D _cockpitCamera;
	private Camera3D _externalCamera;
	private bool _isExternalView = false;
	private Label3D _healthDisplay;
	private Label3D _speedDisplay;
	private HealthComponent health;
	private bool _isDead = false;

	private MeshInstance3D _hull;
	private bool _showShip = true;
	private MeshInstance3D _thrusterFlame;
	private ShaderMaterial _thrusterMaterial;
	private float _thrusterIntensity = 0.0f;
	private MeshInstance3D _reverseThruster1;
	private MeshInstance3D _reverseThruster2;
	private ShaderMaterial _reverseThrusterMaterial1;
	private ShaderMaterial _reverseThrusterMaterial2;
	private float _reverseThrusterIntensity = 0.0f;

	public float CurrentHealth => health.CurrentHealth;
	public float MaxHealth => health.MaxHealth;
	public float CurrentSpeed => Velocity.Length();
	public static float BaseMaxSpeed => MAX_SPEED;

	public override void _Ready()
	{
		lightLeft = GetNode<Light3D>("LeftLight");
		lightRight = GetNode<Light3D>("RightLight");
		dustParticles = GetNode<Node3D>("dustParticles");
		dustParticlesGpu = dustParticles.GetNodeOrNull<GpuParticles3D>("GPUParticles3D");
		_cockpitCamera = GetNode<Camera3D>("ShakeableCamera");
		_externalCamera = GetNode<Camera3D>("ExternalCamera");
		LoadControlSettings();
		if (dustParticlesGpu != null)
		{
			dustParticlesGpu.Emitting = false;
			dustParticlesGpu.AmountRatio = 0.0f;
		}

		canvasLayer = GetNode<CanvasLayer>("HUD");
		_scoreHud = GetParent().GetParent().GetNodeOrNull<CanvasLayer>("ScoreHUD");
		_healthDisplay = GetNodeOrNull<Label3D>("HealthDisplay");
		_speedDisplay = GetNodeOrNull<Label3D>("SpeedDisplay");
		health = GetNode<HealthComponent>("HealthComponent");
		health.HealthChanged += OnHealthChanged;
		health.Died += OnDied;
		if (_healthDisplay != null) _healthDisplay.Text = $"{health.CurrentHealth:F0}";

		_hull = GetNodeOrNull<MeshInstance3D>("hull");
		var cfg = GetTree().Root.GetNodeOrNull<ConfigFileHandler>("/root/ConfigFileHandler");
		if (cfg != null)
		{
			_showShip = cfg.GetShowShipModel();
			if (_hull != null)
				_hull.Visible = _showShip;

			foreach (string nodeName in new[]
			{
				"HP", "HealthDisplay", "Speed", "SpeedDisplay",
				"Ammo", "AmmoDisplay", "Missiles", "MissilesDisplay",
				"TargetName", "TargetNameDisplay", "TargetDist", "TargetDistDisplay",
				"ThrusterFlame", "ReverseThruster1", "ReverseThruster2"
			})
				GetNodeOrNull<Node3D>(nodeName)?.SetVisible(_showShip);

			if (!_showShip)
			{
				canvasLayer.Visible = false;
				if (_scoreHud != null) _scoreHud.Visible = false;
			}
		}

		InitWeapons();
		InitBoost();
		InitTargeting();
		InitThrusters();
		_cockpitCamera.MakeCurrent();
	}

	private void InitThrusters()
	{
		_thrusterFlame = GetNodeOrNull<MeshInstance3D>("ThrusterFlame");
		_thrusterMaterial = DuplicateThrusterMaterial(_thrusterFlame);

		_reverseThruster1 = GetNodeOrNull<MeshInstance3D>("ReverseThruster1");
		_reverseThruster2 = GetNodeOrNull<MeshInstance3D>("ReverseThruster2");
		_reverseThrusterMaterial1 = DuplicateThrusterMaterial(_reverseThruster1);
		_reverseThrusterMaterial2 = DuplicateThrusterMaterial(_reverseThruster2);
	}

	// Duplicates the mesh's shader material into a per-instance override so each
	// thruster's intensity can be animated independently. Returns null if absent.
	private static ShaderMaterial DuplicateThrusterMaterial(MeshInstance3D mesh)
	{
		if (mesh == null) return null;
		var srcMat = mesh.GetSurfaceOverrideMaterial(0) as ShaderMaterial
			?? mesh.Mesh?.SurfaceGetMaterial(0) as ShaderMaterial;
		if (srcMat == null) return null;

		var dup = (ShaderMaterial)srcMat.Duplicate();
		mesh.SetSurfaceOverrideMaterial(0, dup);
		return dup;
	}

	private void LoadControlSettings()
	{
		if (ConfigFileHandler.Instance == null)
			return;

		var settings = ConfigFileHandler.Instance.LoadControlSettings();
		if (settings.ContainsKey("aim_sensitivity"))
			AimSensitivity = settings["aim_sensitivity"].AsSingle();
		if (settings.ContainsKey("aim_deadzone"))
			AimDeadzone = settings["aim_deadzone"].AsSingle();
		if (settings.ContainsKey("auto_center_speed"))
			AutoCenterSpeed = settings["auto_center_speed"].AsSingle();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationPaused)
			shooting.Stop();
		else if (what == NotificationUnpaused)
			_justUnpaused = true;
	}

	public void TakeDamage(float amount, CollisionShape3D hitShape = null, Node3D source = null)
	{
		health.TakeDamage(amount, source);
	}

	private void OnDied()
	{
		_isDead = true;
		EventBus.EmitKilled(this, health.LastAttacker);

		ClearTarget();
		SetProcess(false);
		SetPhysicsProcess(false);
		SetProcessInput(false);

		if (dustParticlesGpu != null)
		{
			dustParticlesGpu.Emitting = false;
			dustParticlesGpu.AmountRatio = 0.0f;
		}
		Input.MouseMode = Input.MouseModeEnum.Visible;
		shooting.Stop();
		startShooting.Stop();
		endShooting.Stop();

		var deathScreen = GetNode<ColorRect>("DeathScreenLayer/DeathScreen");
		deathScreen.Visible = true;

		var button = deathScreen.GetNode<Button>("VBoxContainer/MainMenuButton");
		button.Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/Menu.tscn");
	}

	private void OnHealthChanged(float current, float max)
	{
		if (_healthDisplay != null) _healthDisplay.Text = $"{current:F0}";
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseEvent)
		{
			if (!widgetCursorInitialized)
			{
				Vector2 sz = GetViewport().GetVisibleRect().Size;
				widgetCursor = sz / 2.0f;
				widgetCursorInitialized = true;
			}

			widgetCursor += mouseEvent.Relative;
			Vector2 center = GetViewport().GetVisibleRect().Size / 2.0f;
			Vector2 offset = widgetCursor - center;
			if (offset.Length() > AimRadius)
				widgetCursor = center + offset.Normalized() * AimRadius;

			timeSinceLastMouseInput = 0.0;
		}
	}

	public override void _Process(double delta)
	{
		UpdateDustSpawnBySpeed();
		AlignDustSpawnToVelocity((float)delta);
		UpdateAutoCenterCursor((float)delta);
		UpdateThrusterFlame((float)delta);
		if (_speedDisplay != null)
			_speedDisplay.Text = $"{CurrentSpeed:F0}";
		UpdateTargetHUD((float)delta);
	}

	public override void _PhysicsProcess(double delta)
	{
		GetInput((float)delta);
		ProcessBoost((float)delta);
		ApplyInputs((float)delta);
		UpdateGimbalTracking((float)delta);
	}

	private void GetInput(float delta)
	{
		if (_isDead) return;
		if (_justUnpaused)
		{
			_justUnpaused = false;
			return;
		}

		timeSinceLastShot += delta;
		_timeSinceLastMissile += delta;
		if (Input.IsActionJustPressed("light"))
		{
			lightLeft.Visible = !lightLeft.Visible;
			lightRight.Visible = !lightRight.Visible;
		}

		if (Input.IsActionPressed("primary_fire"))
		{
			if (timeSinceLastShot >= fireCooldown)
			{
				Shoot();
				timeSinceLastShot = 0.0;
			}
		}

		if (Input.IsActionJustPressed("primary_fire") && (UnlimitedAmmo || _currentAmmo > 0))
			shooting.Play();

		if (Input.IsActionJustReleased("primary_fire"))
		{
			shooting.Stop();
			if (UnlimitedAmmo || _currentAmmo > 0)
				endShooting.Play();
		}

		if (Input.IsActionJustPressed("camera_switch"))
			ToggleCameraView();

		if (Input.IsActionJustPressed("boost") && CanBoost && !_isBoosting)
		{
			ActivateBoost();
		}

		if (Input.IsActionJustPressed("target_cycle"))
			CycleTarget();

		if (Input.IsActionJustPressed("secondary_fire"))
			FireMissile();
	}

	// Applies the current thrust and torque inputs to the ship, integrating them into velocity and angular velocity
	private void ApplyInputs(float delta)
	{
		SampleLocalInput();
		UpdateAim(delta);
		ReadThrustAndTorqueInput();
		ApplyStopKey(delta);
		IntegrateVelocities(delta);
		MoveAndResolveCollision(delta);
		ApplyRotation(delta);
	}

	// Returns the aim cursor's position in screen space, clamped to the aim circle.
	private Vector2 ResolveAimCursor(Vector2 center)
	{
		if (Input.MouseMode == Input.MouseModeEnum.Visible)
		{
			widgetCursor = GetViewport().GetMousePosition();
			widgetCursorInitialized = true;
			return widgetCursor;
		}

		if (!widgetCursorInitialized)
		{
			widgetCursor = center;
			widgetCursorInitialized = true;
		}
		return widgetCursor;
	}

	// Reads the keyboard into this tick's intent. A replayed or remote tick assigns
	// _input from a record instead.
	private void SampleLocalInput()
	{
		_input.ThrustForward  = Input.IsActionPressed("thrust_forward");
		_input.ThrustBackward = Input.IsActionPressed("thrust_backward");
		_input.StrafeUp       = Input.IsActionPressed("strafe_up");
		_input.StrafeDown     = Input.IsActionPressed("strafe_down");
		_input.StrafeRight    = Input.IsActionPressed("strafe_right");
		_input.StrafeLeft     = Input.IsActionPressed("strafe_left");
		_input.Stop           = Input.IsActionPressed("stop");
		_input.Roll           = Input.GetActionStrength("roll_right") - Input.GetActionStrength("roll_left");
	}

	// Turns the aim cursor's offset from screen centre into pitch and yaw input.
	private void UpdateAim(float delta)
	{
		Vector2 center = GetViewport().GetVisibleRect().Size / 2.0f;

		// Offset from centre, scaled to the aim circle and clamped to its edge.
		Vector2 norm = (ResolveAimCursor(center) - center) / AimRadius;
		float mag = norm.Length();
		if (mag > 1.0f)
		{
			norm /= mag;
			mag = 1.0f;
		}

		if (mag < AimDeadzone)
		{
			aimTargetYaw = 0.0f;
			aimTargetPitch = 0.0f;
		}
		else
		{
			aimTargetYaw = -Mathf.Clamp(norm.X, -1.0f, 1.0f) * MOUSE_SENSITIVITY * AimSensitivity;
			aimTargetPitch = Mathf.Clamp(-norm.Y, -1.0f, 1.0f) * MOUSE_SENSITIVITY * AimSensitivity;
		}

		aimYaw = Mathf.Lerp(aimYaw, aimTargetYaw, AIM_RESPONSIVENESS * delta);
		aimPitch = Mathf.Lerp(aimPitch, aimTargetPitch, AIM_RESPONSIVENESS * delta);
		yawInput = aimYaw;
		pitchInput = aimPitch;

		// Only the result is intent; sensitivity and smoothing stay local.
		_input.Pitch = aimPitch;
		_input.Yaw = aimYaw;
	}

	// Collects the movement keys into current frame's thrust and torque vectors.
	private void ReadThrustAndTorqueInput()
	{
		_input = _input.Sanitized();
		thrust = Vector3.Zero;

		// Forward axis (-Z): boost forces full forward thrust and disables backward.
		if (_isBoosting)
		{
			thrust -= Transform.Basis.Z * _currentAcceleration;
		}
		else
		{
			thrust -= Transform.Basis.Z * _currentAcceleration
				* ShipInput.Axis(_input.ThrustForward, _input.ThrustBackward);
		}

		thrust += Transform.Basis.Y * _currentAcceleration * ShipInput.Axis(_input.StrafeUp, _input.StrafeDown);
		thrust += Transform.Basis.X * _currentAcceleration * ShipInput.Axis(_input.StrafeRight, _input.StrafeLeft);

		pitchInput = _input.Pitch;
		yawInput = _input.Yaw;
		rollInput = _input.Roll;
		torque = new Vector3(
			pitchInput * _currentPitchAcceleration,
			yawInput * _currentYawAcceleration,
			-rollInput * _currentRollAcceleration);
	}

	private void ApplyStopKey(float delta)
	{
		if (!_input.Stop || _isBoosting)
			return;

		thrust = Vector3.Zero;
		torque = Vector3.Zero;
		if (Velocity.Length() > 0.1f)
			Velocity = Velocity.MoveToward(Vector3.Zero, ACCELERATION * delta);
		angularVelocity = angularVelocity.MoveToward(Vector3.Zero, MAX_PITCH_SPEED * 4f * delta);
	}

	// Integrates thrust and torque, caps both against the ship's current limits.
	private void IntegrateVelocities(float delta)
	{
		Velocity += thrust * delta;

		if (_isBoostDecaying)
		{
			// While boost bleeds off, the cap eases from the boosted speed back to normal.
			float decayProgress = 1.0f - (_boostDecayTimer / BoostDecayTime);
			Velocity = Velocity.LimitLength(Mathf.Lerp(_speedAtBoostEnd, MAX_SPEED, decayProgress));
		}
		else
		{
			Velocity = Velocity.LimitLength(_currentMaxSpeed);
		}

		angularVelocity += torque * delta;
		angularVelocity.X = Mathf.Clamp(angularVelocity.X, -_currentMaxPitchSpeed, _currentMaxPitchSpeed);
		angularVelocity.Y = Mathf.Clamp(angularVelocity.Y, -_currentMaxYawSpeed, _currentMaxYawSpeed);
		angularVelocity.Z = Mathf.Clamp(angularVelocity.Z, -_currentMaxRollSpeed, _currentMaxRollSpeed);
	}

	private void MoveAndResolveCollision(float delta)
	{
		var collision = MoveAndCollide(Velocity * delta);
		if (collision == null)
			return;

		Vector3 normal = collision.GetNormal();

		float impactSpeed = Mathf.Max(0.0f, -Velocity.Dot(normal));
		if (impactSpeed > CollisionDamageSpeedThreshold)
			health.TakeDamage((impactSpeed - CollisionDamageSpeedThreshold) * CollisionDamageMultiplier,
				collision.GetCollider() as Node3D);

		Velocity = Velocity.Slide(normal) * CollisionLinearDamping;
		angularVelocity *= CollisionAngularDamping;

		if (CollisionPushOutDistance > 0.0f)
			GlobalPosition += normal * CollisionPushOutDistance;
	}

	private void ApplyRotation(float delta)
	{
		RotateObjectLocal(Vector3.Right, angularVelocity.X * delta);
		RotateObjectLocal(Vector3.Up, angularVelocity.Y * delta);
		RotateObjectLocal(Vector3.Back, angularVelocity.Z * delta);
		Transform = Transform.Orthonormalized();
	}

	private void ToggleCameraView()
	{
		_isExternalView = !_isExternalView;

		bool cockpit = !_isExternalView;
		(cockpit ? _cockpitCamera : _externalCamera).MakeCurrent();
		canvasLayer.Visible = cockpit && _showShip;
		if (_scoreHud != null) _scoreHud.Visible = cockpit && _showShip;
		if (dustParticles != null) dustParticles.Visible = cockpit;
	}

	private void UpdateAutoCenterCursor(float delta)
	{
		if (!AutoCenterCursor || !widgetCursorInitialized)
			return;

		timeSinceLastMouseInput += delta;
		if (timeSinceLastMouseInput >= AutoCenterDelay)
		{
			Vector2 center = GetViewport().GetVisibleRect().Size / 2.0f;
			float blendAmount = Mathf.Clamp(delta * AutoCenterSpeed, 0.0f, 1.0f);
			widgetCursor = widgetCursor.Lerp(center, blendAmount);
		}
	}

	private void UpdateDustSpawnBySpeed()
	{
		if (dustParticlesGpu == null)
			return;

		float speed = Velocity.Length();
		if (speed < DustSpawnStartSpeed)
		{
			dustParticlesGpu.Emitting = false;
			dustParticlesGpu.AmountRatio = 0.0f;
			return;
		}

		dustParticlesGpu.Emitting = true;
		float speedRange = Mathf.Max(0.01f, DustSpawnFullSpeed - DustSpawnStartSpeed);
		float t = Mathf.Clamp((speed - DustSpawnStartSpeed) / speedRange, 0.0f, 1.0f);
		dustParticlesGpu.AmountRatio = Mathf.Clamp(t * DustMaxAmountRatio, 0.0f, 1.0f);
	}

	private void AlignDustSpawnToVelocity(float delta)
	{
		if (dustParticles == null)
			return;

		if (Velocity.Length() < DustSpawnStartSpeed)
			return;

		Vector3 desiredAxis = Velocity.Normalized();
		Vector3 reference = dustParticles.GlobalTransform.Basis.Z.Normalized();
		if (Mathf.Abs(reference.Dot(desiredAxis)) > 0.98f)
			reference = dustParticles.GlobalTransform.Basis.X.Normalized();

		Vector3 right = reference.Cross(desiredAxis).Normalized();
		Vector3 forward = desiredAxis.Cross(right).Normalized();
		Basis targetBasis = new Basis(right, desiredAxis, forward);

		Quaternion currentRotation = dustParticles.GlobalTransform.Basis.GetRotationQuaternion();
		Quaternion targetRotation = targetBasis.GetRotationQuaternion();
		float blend = Mathf.Clamp(delta * DustAlignSmoothing, 0.0f, 1.0f);
		Quaternion blendedRotation = currentRotation.Slerp(targetRotation, blend);

		dustParticles.GlobalTransform = new Transform3D(new Basis(blendedRotation), dustParticles.GlobalTransform.Origin);
	}

	private void UpdateThrusterFlame(float delta)
	{
		// Forward thruster flame
		if (_thrusterMaterial != null)
		{
			float targetIntensity;
			if (_isBoosting)
				targetIntensity = 1.5f;
			else if (_input.ThrustForward)
				targetIntensity = 0.6f + _currentBoostPower * 0.4f;
			else if (Velocity.LengthSquared() > 1f)
				targetIntensity = 0.15f;
			else
				targetIntensity = 0.0f;

			_thrusterIntensity = Mathf.Lerp(_thrusterIntensity, targetIntensity, 8.0f * delta);
			_thrusterMaterial.SetShaderParameter("intensity", _thrusterIntensity);
		}

		// Reverse thruster flame
		if (_reverseThrusterMaterial1 != null || _reverseThrusterMaterial2 != null)
		{
			float reverseTarget;
			if (_input.ThrustBackward && !_isBoosting)
				reverseTarget = 0.6f;
			else
				reverseTarget = 0.0f;

			_reverseThrusterIntensity = Mathf.Lerp(_reverseThrusterIntensity, reverseTarget, 8.0f * delta);
			_reverseThrusterMaterial1?.SetShaderParameter("intensity", _reverseThrusterIntensity);
			_reverseThrusterMaterial2?.SetShaderParameter("intensity", _reverseThrusterIntensity);
		}
	}
}
