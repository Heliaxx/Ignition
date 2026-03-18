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

        Vector3 toCenter = CenterPoint - Fighter.GlobalPosition;
        float distanceSquared = toCenter.LengthSquared();

        if (distanceSquared < acceptableRadiusSquared)
        {
            stateMachine.TransitionTo("Idle");
            return;
        }

        // Move and face center
        Fighter.MoveInDirection(toCenter.Normalized(), delta);
        Fighter.FaceDirection(toCenter, delta);
    }
}
