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
        
        // If we've run far enough, stop running
        if (distance > fighter.FarthestDistance + 50f) // adjust buffer as needed
        {
            stateMachine.TransitionTo("Idle");
            return;
        }

        fighter.MoveAwayFromPlayer((float)delta);
    }
}



// using Godot;
// using System;

// public partial class Run : State
// {
//     public override void Enter()
//     {
//         GD.Print("Entering Run state");
//     }

//     public override void PhysicsUpdate(float delta)
//     {
//         Fighter fighter = GetParent().GetParent() as Fighter;
//         if (fighter == null || !fighter.HasTarget()) return;

//         float distance = fighter.DistanceToPlayer();

//         // Keep fleeing
//         fighter.MoveAwayFromPlayer(delta);

//         // Once far enough, go back to idle
//         if (distance >= fighter.FarthestDistance)
//         {
//             stateMachine.TransitionTo("Idle");
//         }
//     }
// }