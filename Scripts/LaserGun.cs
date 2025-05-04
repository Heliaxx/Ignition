using Godot;
using System;
using System.Threading.Tasks;

public partial class LaserGun : RayCast3D
{
    // Onready equivalent:
    private Node3D beamMesh;
    private Node3D endParticles;
    private Node3D beamParticles;

    [Export]
    public float DamageMax { get; set; } = 20.0f;

    [Export]
    public float RayLength { get; set; } = 400.0f;

    [Export]
    public float PowerOnTime { get; set; } = 5.0f;

    [Export]
    public float PowerOffTime { get; set; } = 5.0f;

    // Measures progress into powering on/off
    private float progressTime = 0.0f;

    private Tween tween;
    private float beamRadius = 0.5f;
    private bool stayOn = false;

    public enum LaserState
    {
        OFF,
        ON,
        POWERING_OFF,
        POWERING_ON
    }

    private LaserState state = LaserState.OFF;

    public override async void _Ready()
    {
        // Cache child nodes
        beamMesh = GetNode<Node3D>("BeamMesh");
        endParticles = GetNode<Node3D>("EndParticles");
        beamParticles = GetNode<Node3D>("BeamParticles");

        // Wait 2 seconds, then deactivate and activate in sequence.
        await ToSignal(GetTree().CreateTimer(2.0f), "timeout");
        await Deactivate(1.0f);
        await ToSignal(GetTree().CreateTimer(2.0f), "timeout");
        await Activate(1.0f);
    }

    public override void _Process(double delta)
    {
        ForceRaycastUpdate();

        if (IsColliding())
        {
            // Convert collision point to local space.
            Vector3 castPoint = ToLocal(GetCollisionPoint());

            // -- Adjust beam mesh --
            // Assuming beamMesh is a MeshInstance3D with a CylinderMesh as its mesh.
            if (beamMesh is MeshInstance3D bm && bm.Mesh is CylinderMesh cylMesh)
            {
                // Set the height of the cylinder (using negative castPoint.y)
                cylMesh.Height = -castPoint.y;
                // Position the beam mesh halfway along the beam's length.
                bm.Position = new Vector3(bm.Position.x, castPoint.y / 2.0f, bm.Position.z);
            }

            // -- Adjust particles positions --
            // Assuming these nodes are Node3D or a subclass with Position property.
            endParticles.Position = new Vector3(endParticles.Position.x, castPoint.y, endParticles.Position.z);
            beamParticles.Position = new Vector3(beamParticles.Position.x, castPoint.y / 2.0f, beamParticles.Position.z);

            // Calculate number of particles (snapped to 1 increments).
            int particleAmount = (int)Math.Round(Math.Abs(castPoint.y) * 50.0f);
            particleAmount = Math.Max(particleAmount, 1);

            // Adjust particle settings if beamParticles is a Particles3D.
            if (beamParticles is Particles3D particles)
            {
                particles.Amount = (uint)particleAmount;
                // Adjust emission box extents.
                // Get the top radius from the beam mesh (if using a CylinderMesh).
                float radius = 0.0f;
                if (beamMesh is MeshInstance3D bm2 && bm2.Mesh is CylinderMesh cyl)
                {
                    radius = cyl.TopRadius;
                }
                float height = Math.Abs(castPoint.y) / 2.0f;

                // Assume the ProcessMaterial is a ParticlesMaterial.
                if (particles.ProcessMaterial is ParticlesMaterial partMat)
                {
                    partMat.EmissionBoxExtents = new Vector3(radius, height, radius);
                }
            }
        }
    }

    public async Task Activate(float time)
    {
        tween = GetTree().CreateTween();
        Visible = true;
        
        if (beamParticles is Particles3D bp)
            bp.Emitting = true;
        if (endParticles is Particles3D ep)
            ep.Emitting = true;
        
        tween.SetParallel(true);

        // Tween the beam mesh's radii.
        if (beamMesh is MeshInstance3D bm && bm.Mesh is CylinderMesh cylMesh)
        {
            tween.TweenProperty(cylMesh, "top_radius", beamRadius, time);
            tween.TweenProperty(cylMesh, "bottom_radius", beamRadius, time);
        }

        // Tween properties on the beam particles.
        if (beamParticles is Particles3D bpMat && bpMat.ProcessMaterial is ParticlesMaterial beamMat)
        {
            tween.TweenProperty(beamMat, "scale_min", 1.0f, time);
        }
        if (endParticles is Particles3D epMat && epMat.ProcessMaterial is ParticlesMaterial endMat)
        {
            tween.TweenProperty(endMat, "scale_min", 1.0f, time);
        }

        await tween.ToSignal(tween, "finished");
    }

    public async Task Deactivate(float time)
    {
        tween = GetTree().CreateTween();
        tween.SetParallel(true);

        if (beamMesh is MeshInstance3D bm && bm.Mesh is CylinderMesh cylMesh)
        {
            tween.TweenProperty(cylMesh, "top_radius", 0.0f, time);
            tween.TweenProperty(cylMesh, "bottom_radius", 0.0f, time);
        }
        if (beamParticles is Particles3D bp && bp.ProcessMaterial is ParticlesMaterial beamMat)
        {
            tween.TweenProperty(beamMat, "scale_min", 0.0f, time);
        }
        if (endParticles is Particles3D ep && ep.ProcessMaterial is ParticlesMaterial endMat)
        {
            tween.TweenProperty(endMat, "scale_min", 0.0f, time);
        }

        await tween.ToSignal(tween, "finished");

        Visible = false;
        if (beamParticles is Particles3D bp2)
            bp2.Emitting = false;
        if (endParticles is Particles3D ep2)
            ep2.Emitting = false;
    }
}