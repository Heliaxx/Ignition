using Godot;
using System;

public partial class Run : State
{
    public override void Enter()
    {
        GD.Print("Entering Run state");
    }

    public override void PhysicsUpdate(float delta)
    {
        Fighter fighter = GetParent().GetParent() as Fighter;
        if (fighter == null || !fighter.HasTarget()) return;

        float distance = fighter.DistanceToPlayer();
        
        if (distance > fighter.FarthestDistance + 50f)
        {
            stateMachine.TransitionTo("Idle");
            return;
        }

        fighter.MoveAwayFromPlayer((float)delta);
    }
}