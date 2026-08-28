using Godot;

public partial class Kaito
{
    [ExportGroup("Missiles")]
    [Export] public bool UnlimitedMissiles = false;
    [Export] public int MaxMissiles = 8;
    [Export] public float MissileCooldown = 1f;
    [Export] public float MissileDamageMin = 50f;
    [Export] public float MissileDamageMax = 70f;

    private int _currentMissiles;
    public int CurrentMissiles => _currentMissiles;
    private double _timeSinceLastMissile = 0.0;
    private PackedScene _missileScene;

    [ExportGroup("Gatling")]
    [Export] public bool UnlimitedGatlingAmmo = false;

    private GatlingWeapon _gatling;

    private bool UnlimitedAmmo => _gatling?.UnlimitedAmmo ?? false;
    private int _currentAmmo  => _gatling?.CurrentAmmo    ?? 0;

    private double timeSinceLastShot = 0.0;
    private double fireCooldown;

    private AudioStreamPlayer3D startShooting;
    private AudioStreamPlayer3D shooting;
    private AudioStreamPlayer3D endShooting;

    private Label3D _ammoDisplay3D;
    private Label3D _missilesDisplay3D;

    private void InitWeapons()
    {
        startShooting = GetNode<AudioStreamPlayer3D>("ShootingStart");
        shooting      = GetNode<AudioStreamPlayer3D>("Shooting");
        endShooting   = GetNode<AudioStreamPlayer3D>("ShootingEnd");

        _missileScene = GD.Load<PackedScene>("res://Scenes/FlightModelMissile.tscn");
        _timeSinceLastMissile = MissileCooldown;
        _currentMissiles = MaxMissiles;

        _ammoDisplay3D    = GetNodeOrNull<Label3D>("AmmoDisplay");
        _missilesDisplay3D = GetNodeOrNull<Label3D>("MissilesDisplay");

        _gatling = GetNodeOrNull<GatlingWeapon>("GatlingWeapon");
        if (_gatling != null)
        {
            if (UnlimitedGatlingAmmo)
                _gatling.UnlimitedAmmo = true;
            _gatling.Shooter = this;
            _gatling.SpawnParent = GetParent() as Node3D;
            _gatling.AmmoChanged += (cur, max) => UpdateAmmoHUD();
        }

        fireCooldown = _gatling != null ? 1.0 / _gatling.FireRate : 0.1;
        timeSinceLastShot = fireCooldown;

        UpdateAmmoHUD();
        UpdateMissileHUD();
    }

    private void UpdateGimbalTracking(float delta)
    {
        if (_gatling == null) return;
        _gatling.SetTarget(_lockedTarget);
        _gatling.ShipVelocity = Velocity;
    }

    private void Shoot()
    {
        if (!UnlimitedAmmo && _currentAmmo <= 0) return;

        int ammoBefore = _currentAmmo;
        _gatling?.FireOnce();

        if (!UnlimitedAmmo && ammoBefore > 0 && _currentAmmo <= 0)
        {
            shooting.Stop();
            endShooting.Play();
        }
    }

    private void FireMissile()
    {
        if (_missileScene == null) return;
        if (!UnlimitedMissiles && _currentMissiles <= 0) return;
        if (_timeSinceLastMissile < MissileCooldown) return;
        if (_lockedTarget != null && IsInstanceValid(_lockedTarget) && !IsMissileLocked) return;

        // Flight-model missile (physics-based, PN guidance).
        var instance = _missileScene.Instantiate<FlightModelMissile>();
        instance.Target = (_lockedTarget != null && IsInstanceValid(_lockedTarget)) ? _lockedTarget : null;
        instance.Damage = (float)GD.RandRange(MissileDamageMin, MissileDamageMax);
        instance.InheritedVelocity = Velocity; // launch matching ship momentum
        instance.Source = this;
        instance.IgnoreBody(this); // don't detonate on the launcher

        AddCollisionExceptionWith(instance);
        instance.TreeExited += () =>
        {
            if (IsInstanceValid(this) && IsInstanceValid(instance))
                RemoveCollisionExceptionWith(instance);
        };

        Transform3D spawn = _gatling?.GlobalTransform ?? GlobalTransform;
        spawn.Origin -= spawn.Basis.Z * 5f;
        instance.Transform = spawn;
        GetParent().AddChild(instance);

        if (!UnlimitedMissiles) _currentMissiles--;
        _timeSinceLastMissile = 0.0;
        UpdateMissileHUD();
    }

    private void UpdateAmmoHUD()
    {
        if (_ammoDisplay3D == null || _gatling == null) return;
        _ammoDisplay3D.Text = _gatling.UnlimitedAmmo ? "∞" : $"{_gatling.CurrentAmmo}/{_gatling.MaxAmmo}";
    }

    private void UpdateMissileHUD()
    {
        if (_missilesDisplay3D != null)
            _missilesDisplay3D.Text = UnlimitedMissiles ? "∞" : $"{_currentMissiles}/{MaxMissiles}";
    }

    public float GetGimbalScreenRadius()    => _gatling?.GetGimbalScreenRadius() ?? 0f;
    public Vector2? GetGimbalAimScreenPos() => _gatling?.GetAimScreenPos();

    // Kaito.Targeting.cs uses these names — delegate to GatlingWeapon
    private Node3D barrel     => _gatling;
    private float GimbalAngle => _gatling?.GimbalAngle  ?? 10f;
    private float BulletSpeed => _gatling?.BulletSpeed  ?? 1600f;
}
