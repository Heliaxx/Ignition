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
		if (_hull != null && cfg != null)
			_hull.Visible = cfg.GetShowShipModel();

		InitWeapons();
		InitBoost();
		InitTargeting();
		InitThrusters();
		_cockpitCamera.MakeCurrent();
	}

	private void InitThrusters()
	{
		_thrusterFlame = GetNodeOrNull<MeshInstance3D>("ThrusterFlame");
		if (_thrusterFlame != null)
		{
			var srcMat = _thrusterFlame.GetSurfaceOverrideMaterial(0) as ShaderMaterial
				?? _thrusterFlame.Mesh?.SurfaceGetMaterial(0) as ShaderMaterial;
			if (srcMat != null)
			{
				_thrusterMaterial = (ShaderMaterial)srcMat.Duplicate();
				_thrusterFlame.SetSurfaceOverrideMaterial(0, _thrusterMaterial);
			}
		}

		_reverseThruster1 = GetNodeOrNull<MeshInstance3D>("ReverseThruster1");
		_reverseThruster2 = GetNodeOrNull<MeshInstance3D>("ReverseThruster2");
		if (_reverseThruster1 != null)
		{
			var srcMat = _reverseThruster1.GetSurfaceOverrideMaterial(0) as ShaderMaterial
				?? _reverseThruster1.Mesh?.SurfaceGetMaterial(0) as ShaderMaterial;
			if (srcMat != null)
			{
				_reverseThrusterMaterial1 = (ShaderMaterial)srcMat.Duplicate();
				_reverseThruster1.SetSurfaceOverrideMaterial(0, _reverseThrusterMaterial1);
			}
		}
		if (_reverseThruster2 != null)
		{
			var srcMat = _reverseThruster2.GetSurfaceOverrideMaterial(0) as ShaderMaterial
				?? _reverseThruster2.Mesh?.SurfaceGetMaterial(0) as ShaderMaterial;
			if (srcMat != null)
			{
				_reverseThrusterMaterial2 = (ShaderMaterial)srcMat.Duplicate();
				_reverseThruster2.SetSurfaceOverrideMaterial(0, _reverseThrusterMaterial2);
			}
		}
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

	public void TakeDamage(float amount, CollisionShape3D hitShape = null)
	{
		health.TakeDamage(amount);
	}

	private void OnDied()
	{
		_isDead = true;
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

		// Camera switch
		if (Input.IsActionJustPressed("camera_switch"))
			ToggleCameraView();

		// Boost input
		if (Input.IsActionJustPressed("boost") && CanBoost && !_isBoosting)
		{
			ActivateBoost();
		}

		// Target cycling
		if (Input.IsActionJustPressed("target_cycle"))
			CycleTarget();

		// Missiles
		if (Input.IsActionJustPressed("secondary_fire"))
			FireMissile();
	}

	private void ApplyInputs(float delta)
	{
		Vector2 screenSize = GetViewport().GetVisibleRect().Size;
		Vector2 center = screenSize / 2.0f;
		Vector2 mousePos;
		if (Input.MouseMode == Input.MouseModeEnum.Visible)
		{
			mousePos = GetViewport().GetMousePosition();
			widgetCursor = mousePos;
			widgetCursorInitialized = true;
		}
		else
		{
			if (!widgetCursorInitialized)
			{
				widgetCursor = center;
				widgetCursorInitialized = true;
			}
			mousePos = widgetCursor;
		}

		Vector2 diff = mousePos - center;
		Vector2 norm = diff / AimRadius;
		float mag = norm.Length();
		if (mag > 1.0f)
			norm = norm / mag;
		if (norm.Length() < AimDeadzone)
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

		thrust = Vector3.Zero;

		// During active boost, force full forward thrust and disable backward
		if (_isBoosting)
		{
			thrust = Transform.Basis.X * _currentAcceleration;
			if (Input.IsActionPressed("strafe_up"))
				thrust += Transform.Basis.Y * _currentAcceleration;
			if (Input.IsActionPressed("strafe_down"))
				thrust -= Transform.Basis.Y * _currentAcceleration;
			if (Input.IsActionPressed("strafe_right"))
				thrust += Transform.Basis.Z * _currentAcceleration;
			if (Input.IsActionPressed("strafe_left"))
				thrust -= Transform.Basis.Z * _currentAcceleration;
		}
		else
		{
			if (Input.IsActionPressed("thrust_forward"))
				thrust += Transform.Basis.X * _currentAcceleration;
			if (Input.IsActionPressed("thrust_backward"))
				thrust -= Transform.Basis.X * _currentAcceleration;
			if (Input.IsActionPressed("strafe_up"))
				thrust += Transform.Basis.Y * _currentAcceleration;
			if (Input.IsActionPressed("strafe_down"))
				thrust -= Transform.Basis.Y * _currentAcceleration;
			if (Input.IsActionPressed("strafe_right"))
				thrust += Transform.Basis.Z * _currentAcceleration;
			if (Input.IsActionPressed("strafe_left"))
				thrust -= Transform.Basis.Z * _currentAcceleration;
		}

		torque = Vector3.Zero;
		rollInput = Input.GetActionStrength("roll_right") - Input.GetActionStrength("roll_left");
		torque.X = rollInput * _currentRollAcceleration;
		torque.Y = yawInput * _currentYawAcceleration;
		torque.Z = pitchInput * _currentPitchAcceleration;

		// Stop key: kills all movement and rotation
		if (Input.IsActionPressed("stop") && !_isBoosting)
		{
			thrust = Vector3.Zero;
			torque = Vector3.Zero;
			if (Velocity.Length() > 0.1f)
				Velocity = Velocity.MoveToward(Vector3.Zero, ACCELERATION * delta);
			angularVelocity = angularVelocity.MoveToward(Vector3.Zero, MAX_PITCH_SPEED * 4f * delta);
		}

		Velocity += thrust * delta;

		// Cap velocity based on current state
		if (_isBoostDecaying)
		{
			float decayProgress = 1.0f - (_boostDecayTimer / BoostDecayTime);
			float decayMaxSpeed = Mathf.Lerp(_speedAtBoostEnd, MAX_SPEED, decayProgress);
			Velocity = Velocity.LimitLength(decayMaxSpeed);
		}
		else
		{
			Velocity = Velocity.LimitLength(_currentMaxSpeed);
		}

		angularVelocity += torque * delta;
		angularVelocity.X = Mathf.Clamp(angularVelocity.X, -_currentMaxRollSpeed, _currentMaxRollSpeed);
		angularVelocity.Y = Mathf.Clamp(angularVelocity.Y, -_currentMaxYawSpeed, _currentMaxYawSpeed);
		angularVelocity.Z = Mathf.Clamp(angularVelocity.Z, -_currentMaxPitchSpeed, _currentMaxPitchSpeed);

		var collision = MoveAndCollide(Velocity * delta);
		if (collision != null)
		{
			Vector3 normal = collision.GetNormal();

			float impactSpeed = Mathf.Max(0.0f, -Velocity.Dot(normal));
			if (impactSpeed > CollisionDamageSpeedThreshold)
			{
				float damage = (impactSpeed - CollisionDamageSpeedThreshold) * CollisionDamageMultiplier;
				health.TakeDamage(damage);
			}

			Velocity = Velocity.Slide(normal) * CollisionLinearDamping;
			angularVelocity *= CollisionAngularDamping;

			if (CollisionPushOutDistance > 0.0f)
			{
				GlobalPosition += normal * CollisionPushOutDistance;
			}
		}

		RotateObjectLocal(Vector3.Right, angularVelocity.X * delta);
		RotateObjectLocal(Vector3.Up, angularVelocity.Y * delta);
		RotateObjectLocal(Vector3.Back, angularVelocity.Z * delta);

		// Prevent floating-point drift in the rotation matrix from accumulating over time
		Transform = Transform.Orthonormalized();
	}

	private void ToggleCameraView()
	{
		_isExternalView = !_isExternalView;

		if (_isExternalView)
		{
			_externalCamera.MakeCurrent();
			canvasLayer.Visible = false;
			if (_scoreHud != null) _scoreHud.Visible = false;
			if (dustParticles != null)
				dustParticles.Visible = false;
		}
		else
		{
			_cockpitCamera.MakeCurrent();
			canvasLayer.Visible = true;
			if (_scoreHud != null) _scoreHud.Visible = true;
			if (dustParticles != null)
				dustParticles.Visible = true;
		}
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
		// Forward thruster
		if (_thrusterMaterial != null)
		{
			float targetIntensity;
			if (_isBoosting)
				targetIntensity = 1.5f;
			else if (Input.IsActionPressed("thrust_forward"))
				targetIntensity = 0.6f + _currentBoostPower * 0.4f;
			else if (Velocity.LengthSquared() > 1f)
				targetIntensity = 0.15f;
			else
				targetIntensity = 0.0f;

			_thrusterIntensity = Mathf.Lerp(_thrusterIntensity, targetIntensity, 8.0f * delta);
			_thrusterMaterial.SetShaderParameter("intensity", _thrusterIntensity);
		}

		// Reverse thrusters
		if (_reverseThrusterMaterial1 != null || _reverseThrusterMaterial2 != null)
		{
			float reverseTarget;
			if (Input.IsActionPressed("thrust_backward") && !_isBoosting)
				reverseTarget = 0.6f;
			else
				reverseTarget = 0.0f;

			_reverseThrusterIntensity = Mathf.Lerp(_reverseThrusterIntensity, reverseTarget, 8.0f * delta);
			_reverseThrusterMaterial1?.SetShaderParameter("intensity", _reverseThrusterIntensity);
			_reverseThrusterMaterial2?.SetShaderParameter("intensity", _reverseThrusterIntensity);
		}
	}
}
