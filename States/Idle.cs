using Godot;
using System;

public partial class Idle : CombatState
{
    private float detectionRangeSquared;

    public override void Enter()
    {
        float range = Fighter?.DetectionRange ?? 600f;
        detectionRangeSquared = range * range;
        Fighter?.SetLaserFiring(false);
    }

    public override void PhysicsUpdate(float delta)
    {
        if (Fighter == null || !Fighter.HasTarget()) return;

        // Use steering-based wander for organic movement
        Fighter.Wander(delta);

        // If player enters detection range, pursue
        float distanceSquared = Fighter.DistanceSquaredToPlayer();
        if (distanceSquared < detectionRangeSquared)
        {
            stateMachine.TransitionTo("Pursue");
        }
    }
}
