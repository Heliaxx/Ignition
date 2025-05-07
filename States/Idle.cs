// using Godot;
// using System;

// public partial class Idle : State
// {
//     public override void Enter()
//     {
//         GD.Print("Entering Idle state");
//     }

//     public override void PhysicsUpdate(float delta)
//     {
//         Fighter fighter = GetParent().GetParent() as Fighter;
//         if (fighter == null || !fighter.HasTarget()) return;

//         float distance = fighter.DistanceToPlayer();
//         if (distance < fighter.ClosestDistance)
//         {
//             stateMachine.TransitionTo("Run");
//         }
//         else
//         {
//             stateMachine.TransitionTo("Follow");
//         }
//     }
// }



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
            GD.Randf() * 0.5f - 0.25f, // slight vertical variance
            GD.Randf() * 2f - 1f
        ).Normalized();
        changeDirectionTimer = directionChangeInterval;
    }
}



// using Godot;
// using System;

// public partial class Idle : State
// {
//     [Export] public float Speed = 10.0f;
//     [Export] public float DirectionChangeInterval = 3.0f;
//     [Export] public float TurnSpeed = 1.5f;

//     private Fighter fighter;
//     private Vector3 targetDirection;
//     private float directionTimer = 0f;

//     public override void Enter()
//     {
//         fighter = GetParent<Fighter>();
//         PickNewDirection();
//         directionTimer = DirectionChangeInterval;
//     }

//     public override void PhysicsUpdate(float delta)
//     {
//         directionTimer -= delta;
//         if (directionTimer <= 0)
//         {
//             PickNewDirection();
//             directionTimer = DirectionChangeInterval;
//         }

//         // Smooth movement toward direction
//         Vector3 desiredVelocity = targetDirection * Speed;
//         fighter.Velocity = fighter.Velocity.Lerp(desiredVelocity, fighter.Acceleration * delta);
//         fighter.MoveAndSlide();

//         // Smooth rotation to face direction
//         if (fighter.Velocity.Length() > 0.1f)
//         {
//             Vector3 currentForward = -fighter.GlobalTransform.Basis.Z;
//             Vector3 newForward = currentForward.Lerp(fighter.Velocity.Normalized(), TurnSpeed * delta).Normalized();
//             Basis newBasis = Basis.LookingAt(newForward, Vector3.Up);
//             fighter.GlobalTransform = new Transform3D(newBasis, fighter.GlobalTransform.Origin);
//         }
//     }

//     private void PickNewDirection()
//     {
//         // Random horizontal direction
//         Vector3 newDir = new Vector3(
//             (float)GD.RandRange(-1.0, 1.0),
//             (float)GD.RandRange(-0.5, 0.5), // Optional vertical drift
//             (float)GD.RandRange(-1.0, 1.0)
//         ).Normalized();

//         targetDirection = newDir;
//     }
// }
