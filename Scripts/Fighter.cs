using Godot;
using System;

public partial class Fighter : CharacterBody3D, IDamageable
{
    private const int MAX_HEALTH = 100;
    private int currentHealth = MAX_HEALTH;
    public int CurrentHealthValue => currentHealth;
    public int MaxHealthValue => MAX_HEALTH;

    [Signal]
    public delegate void DiedEventHandler();
    [Export]
    public string DisplayName = "Gryph";

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

    [Export]
    public float MaxSteeringForce = 200.0f;

    [Export]
    public float WanderStrength = 30.0f;

    [Export]
    public float ObstacleAvoidanceForce = 400.0f;

    [Export]
    public float ObstacleLookAhead = 80.0f;

    [Export(PropertyHint.Layers3DPhysics)]
    public uint ObstacleCollisionMask = 1; // Layer 1 only (environment/asteroids)

    [ExportGroup("Combat")]
    [Export] public float DetectionRange = 600f;
    [Export] public float WeaponRange = 300f;
    [Export] public float CloseRange = 80f;
    [Export] public float FiringArc = 30f; // degrees - nose cone for firing
    [Export] public float FleeHealthThreshold = 0.3f;
    [Export] public float EvadeDamageCooldown = 1.5f; // seconds after hit before evade can trigger again

    // Cached per-frame values to avoid redundant calculations
    private Vector3 _cachedToPlayer;
    private float _cachedDistanceSquared;
    private bool _cacheValid = false;

    // Steering state
    private float _wanderAngle = 0f;

    // Physics state for obstacle avoidance
    private PhysicsDirectSpaceState3D _spaceState;
    private int _avoidanceFrameCounter = 0;
    private const int AVOIDANCE_FRAME_INTERVAL = 3; // raycast every 3rd physics frame
    private Vector3 _cachedAvoidance = Vector3.Zero;
    private float _cachedAvoidanceUrgency = 0f;

    // Combat state
    private Node3D _laserNode;
    private float _recentDamageTimer = 0f;
    private bool _tookRecentDamage = false;
    private float _firingArcCosThreshold;

    // Thruster visuals
    private ShaderMaterial _thrusterMaterial;

    private Node3D player;
    private CharacterBody3D playerBody;

    public override void _Ready()
    {
        currentHealth = MAX_HEALTH;
        player = GetNode<Node3D>("../Player"); // Adjust if player path changes
        playerBody = player as CharacterBody3D;
        _laserNode = GetNodeOrNull<Node3D>("Laser");
        _firingArcCosThreshold = Mathf.Cos(Mathf.DegToRad(FiringArc));
        var thrusterMesh = GetNodeOrNull<MeshInstance3D>("ThrusterFlame");
        if (thrusterMesh != null)
            _thrusterMaterial = thrusterMesh.GetSurfaceOverrideMaterial(0) as ShaderMaterial
                ?? thrusterMesh.Mesh?.SurfaceGetMaterial(0) as ShaderMaterial;
        // Stagger avoidance frames across instances so they don't all raycast simultaneously
        _avoidanceFrameCounter = (int)(GD.Randi() % AVOIDANCE_FRAME_INTERVAL);
        SetLaserFiring(false);
        if (!HasTarget())
        {
            GD.PrintErr("Player not found in scene.");
        }
    }
    
    public void Reset()
    {
        currentHealth = MAX_HEALTH;
        Velocity = Vector3.Zero;
        _cacheValid = false;
        _wanderAngle = GD.Randf() * Mathf.Tau;
        _tookRecentDamage = false;
        _recentDamageTimer = 0f;
        SetLaserFiring(false);

        // Reset state machine to initial state
        var stateMachine = GetNodeOrNull<StateMachine>("StateMachine");
        stateMachine?.ResetToInitialState();
    }

    public override void _PhysicsProcess(double delta)
    {
        // Invalidate cache at start of each physics frame
        _cacheValid = false;

        // Cache space state for obstacle avoidance raycasts
        _spaceState = GetWorld3D()?.DirectSpaceState;

        // Staggered obstacle avoidance - only raycast every Nth frame
        _avoidanceFrameCounter++;
        if (_avoidanceFrameCounter >= AVOIDANCE_FRAME_INTERVAL)
        {
            _avoidanceFrameCounter = 0;
            if (_spaceState != null && ObstacleAvoidanceForce > 0)
            {
                _cachedAvoidance = SteeringBehaviors.ObstacleAvoidance(
                    _spaceState,
                    GlobalPosition, Velocity, GetForward(),
                    ObstacleAvoidanceForce, ObstacleLookAhead,
                    ObstacleCollisionMask, GetRid(),
                    out _cachedAvoidanceUrgency
                );
            }
            else
            {
                _cachedAvoidance = Vector3.Zero;
                _cachedAvoidanceUrgency = 0f;
            }
        }

        // Tick recent damage timer
        if (_recentDamageTimer > 0f)
        {
            _recentDamageTimer -= (float)delta;
            if (_recentDamageTimer <= 0f)
                _tookRecentDamage = false;
        }

        // Update thruster intensity based on speed
        if (_thrusterMaterial != null)
        {
            float speedRatio = Velocity.Length() / Speed;
            _thrusterMaterial.SetShaderParameter("intensity", Mathf.Clamp(speedRatio * 1.2f, 0f, 1.5f));
        }
    }

    private void EnsureCacheValid()
    {
        if (_cacheValid || player == null) return;
        _cachedToPlayer = player.GlobalPosition - GlobalPosition;
        _cachedDistanceSquared = _cachedToPlayer.LengthSquared();
        _cacheValid = true;
    }

    public bool HasTarget() => player != null;

    public Vector3 ToPlayerVector()
    {
        EnsureCacheValid();
        return _cachedToPlayer;
    }

    public float DistanceSquaredToPlayer()
    {
        EnsureCacheValid();
        return _cachedDistanceSquared;
    }
    
    public Vector3 GetPlayerVelocity()
    {
        return playerBody?.Velocity ?? Vector3.Zero;
    }
    
    public Vector3 GetPlayerPosition()
    {
        return player?.GlobalPosition ?? GlobalPosition;
    }
    
    public Vector3 GetForward()
    {
        return -GlobalTransform.Basis.Z;
    }

    /// <summary>
    /// Pursue the player using predictive interception (leads the target).
    /// Pass skipFacing=true if the caller handles rotation (e.g. via FaceTarget).
    /// </summary>
    public void PursuePlayer(float delta, bool skipFacing = false)
    {
        if (player == null) return;

        Vector3 steering = SteeringBehaviors.Pursue(
            GlobalPosition, Velocity, Speed,
            GetPlayerPosition(), GetPlayerVelocity()
        );

        Vector3 wander = SteeringBehaviors.Wander(GetForward(), WanderStrength * 0.3f, 5f, ref _wanderAngle);
        steering += wander;

        ApplySteeringForce(steering, delta, skipFacing);
    }

    /// <summary>
    /// Evade away from predicted player position.
    /// </summary>
    public void EvadePlayer(float delta)
    {
        if (player == null) return;

        Vector3 steering = SteeringBehaviors.Evade(
            GlobalPosition, Velocity, Speed,
            GetPlayerPosition(), GetPlayerVelocity()
        );

        Vector3 wander = SteeringBehaviors.Wander(GetForward(), WanderStrength * 0.5f, 8f, ref _wanderAngle);
        steering += wander;

        ApplySteeringForce(steering, delta);
    }
    
    /// <summary>
    /// Maintain preferred combat distance from player.
    /// </summary>
    public void MaintainCombatRange(float delta, float preferredRange, float tolerance = 15f)
    {
        if (player == null) return;
        
        Vector3 steering = SteeringBehaviors.MaintainDistance(
            GlobalPosition, Velocity, Speed,
            GetPlayerPosition(), GetPlayerVelocity(),
            preferredRange, tolerance
        );
        
        // Add wander when in the sweet spot
        if (steering.LengthSquared() < 1f)
        {
            steering = SteeringBehaviors.Wander(GetForward(), WanderStrength, 10f, ref _wanderAngle);
        }
        else
        {
            // Small wander even when adjusting position
            Vector3 wander = SteeringBehaviors.Wander(GetForward(), WanderStrength * 0.2f, 3f, ref _wanderAngle);
            steering += wander;
        }
        
        ApplySteeringForce(steering, delta);
    }
    
    /// <summary>
    /// Wander aimlessly with organic movement.
    /// </summary>
    public void Wander(float delta)
    {
        Vector3 steering = SteeringBehaviors.Wander(GetForward(), WanderStrength, 15f, ref _wanderAngle);
        
        // Maintain some forward momentum
        steering += GetForward() * Speed * 0.5f;
        
        ApplySteeringForce(steering, delta);
    }
    
    /// <summary>
    /// Apply a steering force, update velocity, move, and optionally face movement direction.
    /// Uses cached obstacle avoidance (computed in _PhysicsProcess on staggered frames).
    /// Pass skipFacing=true when the caller will handle rotation separately (e.g. FaceTarget).
    /// </summary>
    private void ApplySteeringForce(Vector3 steering, float delta, bool skipFacing = false)
    {
        // Blend in cached obstacle avoidance
        if (_cachedAvoidance.LengthSquared() > 0.01f)
        {
            Vector3 avoidDir = _cachedAvoidance.Normalized();
            float conflictAmount = Mathf.Max(0, -steering.Normalized().Dot(avoidDir));
            float blendFactor = _cachedAvoidanceUrgency * (0.5f + conflictAmount * 0.5f);
            steering = steering.Lerp(_cachedAvoidance, blendFactor);
            steering += _cachedAvoidance * (1f - blendFactor);
        }

        Velocity = SteeringBehaviors.ApplySteering(Velocity, steering, Speed, MaxSteeringForce, delta);
        MoveAndSlide();

        if (!skipFacing)
        {
            Vector3 faceDir = Velocity.LengthSquared() > 1f ? Velocity : steering;
            FaceDirection(faceDir, delta);
        }
    }

    public void MoveInDirection(Vector3 direction, float delta, bool skipFacing = false)
    {
        if (direction.LengthSquared() <= Mathf.Epsilon)
            return;

        Vector3 steering = direction.Normalized() * Speed;
        Vector3 wander = SteeringBehaviors.Wander(GetForward(), WanderStrength * 0.2f, 3f, ref _wanderAngle);
        steering += wander;

        ApplySteeringForce(steering, delta, skipFacing);
    }

    public void FaceDirection(Vector3 direction, float delta)
    {
        if (direction.LengthSquared() <= Mathf.Epsilon)
            return;

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

    // ── Combat utilities ──────────────────────────────────────────

    public void SetLaserFiring(bool enabled)
    {
        if (_laserNode != null)
            _laserNode.Visible = enabled;
    }

    public bool IsPlayerInFiringArc()
    {
        if (player == null) return false;
        Vector3 toPlayer = (GetPlayerPosition() - GlobalPosition).Normalized();
        float dot = GetForward().Dot(toPlayer);
        return dot >= _firingArcCosThreshold;
    }

    public void FaceTarget(float delta)
    {
        if (player == null) return;
        Vector3 toPlayer = GetPlayerPosition() - GlobalPosition;
        if (toPlayer.LengthSquared() > 0.01f)
            FaceDirection(toPlayer, delta);
    }

    public float GetHealthPercent()
    {
        return (float)currentHealth / MAX_HEALTH;
    }

    public bool TookRecentDamage => _tookRecentDamage;

    public float DistanceToPlayer()
    {
        EnsureCacheValid();
        return Mathf.Sqrt(_cachedDistanceSquared);
    }

    public void TakeDamage(float amount, CollisionShape3D hitShape = null)
    {
        currentHealth -= (int)amount;
        _tookRecentDamage = true;
        _recentDamageTimer = EvadeDamageCooldown;
        GD.Print($"{Name} hit! Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private static PackedScene _explosionScene;

    private void Die()
    {
        GD.Print($"{Name} destroyed!");
        SpawnExplosion();
        EmitSignal(SignalName.Died);
        // Don't QueueFree - let the pool recycle this instance
    }

    private void SpawnExplosion()
    {
        _explosionScene ??= GD.Load<PackedScene>("res://Scenes/BigExplosionScene.tscn");
        if (_explosionScene == null) return;

        Vector3 pos = GlobalPosition;
        var explosion = _explosionScene.Instantiate<Node3D>();
        GetTree().Root.AddChild(explosion);
        explosion.GlobalPosition = pos;

        // Auto-free after particles finish (lifetime + small buffer)
        GetTree().CreateTimer(3.0).Timeout += () => explosion.QueueFree();
    }
}