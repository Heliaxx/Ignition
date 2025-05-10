using Godot;
using System;

public partial class Idle : State
{
    private Vector3 wanderDirection = Vector3.Zero;
    private float changeDirectionTimer = 0f;
    private float directionChangeInterval = 3f;

    public override void Enter()
    {
        GD.Print("Entering Idle state");
        SetRandomDirection();
    }

    public override void PhysicsUpdate(float delta)
    {
        Fighter fighter = GetParent().GetParent() as Fighter;
        if (fighter == null || !fighter.HasTarget()) return;

        // Move in the current wander direction
        fighter.MoveInDirection(wanderDirection, delta);
		fighter.FaceDirection(wanderDirection, delta);

        // Change direction at intervals
        changeDirectionTimer -= delta;
        if (changeDirectionTimer <= 0f)
        {
            SetRandomDirection();
        }

        // If player gets close, transition to Follow
        float distance = fighter.DistanceToPlayer();
        if (distance < fighter.ClosestDistance + 100f)
        {
            stateMachine.TransitionTo("Follow");
        }
    }

    private void SetRandomDirection()
    {
        wanderDirection = new Vector3(
            GD.Randf() * 2f - 1f,
            GD.Randf() * 0.5f - 0.25f,
            GD.Randf() * 2f - 1f
        ).Normalized();
        changeDirectionTimer = directionChangeInterval;
    }
}