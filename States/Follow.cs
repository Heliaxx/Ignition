using Godot;
using System;

public partial class Follow : State
{
    private float holdTimer = 0f;
    private float holdDuration = 3.5f;

    public override void Enter()
    {
        GD.Print("Entering Follow state");
        holdTimer = 0f;
    }

    public override void PhysicsUpdate(float delta)
    {
        Fighter fighter = GetParent().GetParent() as Fighter;
        if (fighter == null || !fighter.HasTarget()) return;

        float distance = fighter.DistanceToPlayer();

        // Maintain ~100m distance
        if (distance > fighter.ClosestDistance + 10f)
        {
            fighter.MoveTowardPlayer(delta);
        }
        else if (distance < fighter.ClosestDistance - 10f)
        {
            fighter.MoveAwayFromPlayer(delta);
        }
        else
        {
            fighter.StopMoving(); // drift in place
        }

        // Count time spent in "holding" distance
        if (Mathf.Abs(distance - fighter.ClosestDistance) < 10f)
        {
            holdTimer += delta;
            if (holdTimer >= holdDuration)
            {
                // Randomly choose next state
                if (GD.Randf() < 0.5f)
                {
                    stateMachine.TransitionTo("Run");
                }
                else
                {
                    stateMachine.TransitionTo("Idle");
                }
            }
        }
    }
}
