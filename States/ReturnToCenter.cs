using Godot;
using System;

public partial class ReturnToCenter : State
{
    [Export] public Vector3 CenterPoint = Vector3.Zero;
    [Export] public float AcceptableRadius = 50f;

    public override void Enter()
    {
        GD.Print("Entering ReturnToCenter state");
    }

    public override void PhysicsUpdate(float delta)
    {
        Fighter fighter = GetParent().GetParent() as Fighter;
        if (fighter == null) return;

        Vector3 toCenter = CenterPoint - fighter.GlobalPosition;
        float distance = toCenter.Length();

        if (distance < AcceptableRadius)
        {
            stateMachine.TransitionTo("Idle");
            return;
        }

        // Move and face center
        Vector3 desiredVelocity = toCenter.Normalized() * fighter.Speed;
        // fighter.Velocity = fighter.Velocity.Lerp(desiredVelocity, fighter.Acceleration * delta);
		fighter.MoveInDirection(toCenter.Normalized(), delta);
        // fighter.MoveAndSlide();
        fighter.FaceDirection(toCenter, delta);
    }
}
