using Godot;

public partial class Pursue : CombatState
{
    public override void Enter()
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

        // In weapon range → start attack run
        if (distance <= Fighter.WeaponRange)
        {
            stateMachine.TransitionTo("Joust");
            return;
        }

        // Chase the player — skipFacing since FaceTarget handles rotation
        Fighter.PursuePlayer(delta, skipFacing: true);
        Fighter.FaceTarget(delta);
    }
}
