using Godot;

// Rapid-fire projectile weapon. Fires manually via FireOnce() or automatically
// between StartFiring()/StopFiring(). Gimbal tracking runs every physics frame.
public partial class GatlingWeapon : WeaponBase
{
    [ExportGroup("Gatling")]
    [Export] public float FireRate          = 10f;     // shots per second (auto mode + fireCooldown calc)
    [Export] public float BulletSpeed       = 2500f;
    [Export] public float DamagePerBullet   = 10f;
    [Export] public float SpreadAngle       = 0.5f;    // max bullet deviation, degrees
    [Export] public PackedScene BulletScene;

    [ExportGroup("Ammo")]
    [Export] public bool UnlimitedAmmo = false;
    [Export] public int MaxAmmo = 400;

    protected override float GimbalLeadSpeed => BulletSpeed;

    public int CurrentAmmo { get; private set; }
    public bool IsFiring => _autoFiring;

    // Node bullets are spawned under.
    public Node3D SpawnParent { get; set; }

    // Ship this weapon belongs to. Stamped onto every bullet for kill attribution.
    public Node3D Shooter { get; set; }

    // Ship velocity inherited by spawned bullets.
    public Vector3 ShipVelocity { get; set; }

    [Signal] public delegate void ShotFiredEventHandler();
    [Signal] public delegate void AmmoChangedEventHandler(int current, int max);

    private bool _autoFiring;
    private double _autoTimer;

    public override void _Ready()
    {
        CurrentAmmo = MaxAmmo;
        _autoTimer = 1.0 / FireRate;
    }

    public override void StartFiring()
    {
        if (!UnlimitedAmmo && CurrentAmmo <= 0) return;
        _autoFiring = true;
    }

    public override void StopFiring()
    {
        _autoFiring = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (HasGimbal) UpdateGimbal((float)delta);

        if (!_autoFiring) return;

        _autoTimer += delta;
        double cooldown = 1.0 / FireRate;
        while (_autoTimer >= cooldown)
        {
            _autoTimer -= cooldown;
            FireOnce();
            if (!UnlimitedAmmo && CurrentAmmo <= 0)
            {
                _autoFiring = false;
                break;
            }
        }
    }

    // Fires one bullet, returning the muzzle transform it actually used, spread included,
    // so the caller can relay the shot. Null when nothing was fired.
    public Transform3D? FireOnce()
    {
        if (BulletScene == null) return null;
        if (!UnlimitedAmmo && CurrentAmmo <= 0) return null;

        Transform3D spawn = GlobalTransform;
        if (SpreadAngle > 0f)
        {
            float phi = GD.Randf() * Mathf.Tau;
            float theta = GD.Randf() * Mathf.DegToRad(SpreadAngle);
            Vector3 axis = (GlobalTransform.Basis.X * Mathf.Cos(phi)
                          + GlobalTransform.Basis.Y * Mathf.Sin(phi)).Normalized();
            spawn.Basis = spawn.Basis.Rotated(axis, theta);
        }

        SpawnBullet(spawn, ShipVelocity, hasAuthority: true);

        if (!UnlimitedAmmo)
        {
            CurrentAmmo--;
            EmitSignal(SignalName.AmmoChanged, CurrentAmmo, MaxAmmo);
        }

        EmitSignal(SignalName.ShotFired);
        return spawn;
    }

    // Used by the local shot and by one relayed from another peer. A relayed bullet has no
    // authority: it draws the tracer but never reports a hit.
    public void SpawnBullet(Transform3D spawn, Vector3 inheritedVelocity, bool hasAuthority)
    {
        if (BulletScene == null) return;

        var bullet = BulletScene.Instantiate<Bullet>();
        bullet.Speed = BulletSpeed;
        bullet.Damage = DamagePerBullet;
        bullet.InheritedVelocity = inheritedVelocity;
        bullet.Source = Shooter;
        bullet.HasAuthority = hasAuthority;

        var parent = SpawnParent ?? GetTree().CurrentScene;
        parent.AddChild(bullet);
        bullet.GlobalTransform = spawn;
    }
}
