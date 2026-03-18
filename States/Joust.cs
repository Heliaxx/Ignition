using Godot;

public partial class Joust : CombatState
{
    public override void Enter()
    {
        Fighter?.SetLaserFiring(false);
    }

    public override void Exit()
    {
        Fighter?.SetLaserFiring(false);
    }

    public override void PhysicsUpdate(float delta)
    {
        if (Fighter == null || !Fighter.HasTarget()) return;

        // Flee check
        if (Fighter.GetHealthPercent() <= Fighter.FleeHealthThreshold)
        {
            stateMachine.TransitionTo("Flee");
            return;
        }

        // Evade check
        if (Fighter.TookRecentDamage)
        {
            stateMachine.TransitionTo("Evade");
            return;
        }

        float distance = Fighter.DistanceToPlayer();

        // Player escaped weapon range → pursue
        if (distance > Fighter.WeaponRange * 1.2f)
        {
            stateMachine.TransitionTo("Pursue");
            return;
        }

        // Close range → switch to orbit/circle-strafe
        if (distance < Fighter.CloseRange)
        {
            stateMachine.TransitionTo("Orbit");
            return;
        }

        // Head-on attack run: pursue while facing target
        Fighter.PursuePlayer(delta, skipFacing: true);
        Fighter.FaceTarget(delta);

        // Fire when player is in the nose cone
        Fighter.SetLaserFiring(Fighter.IsPlayerInFiringArc());
    }
}
