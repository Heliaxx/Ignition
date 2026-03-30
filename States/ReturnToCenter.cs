using Godot;
using System;

public partial class ReturnToCenter : CombatState
{
    [Export] public Vector3 CenterPoint = Vector3.Zero;
    [Export] public float AcceptableRadius = 50f;
    private float acceptableRadiusSquared;

    public override void Enter()
    {
        acceptableRadiusSquared = AcceptableRadius * AcceptableRadius;
    }

    public override void PhysicsUpdate(float delta)
    {
        if (Fighter == null) return;

        float detectionSq = Fighter.DetectionRange * Fighter.DetectionRange;
        if (Fighter.DistanceSquaredToPlayer() < detectionSq)
        {
            stateMachine.TransitionTo("Pursue");
            return;
        }

        Vector3 toCenter = CenterPoint - Fighter.GlobalPosition;
        if (toCenter.LengthSquared() < acceptableRadiusSquared)
        {
            stateMachine.TransitionTo("Idle");
            return;
        }

        Fighter.MoveInDirection(toCenter.Normalized(), delta);
        Fighter.FaceDirection(toCenter, delta);
    }
}
