using Godot;

public partial class Pursue : CombatState
{
    public override void Enter()
    {
        Fighter?.SetLaserFiring(false);
    }

    protected override void CombatUpdate(float delta)
    {
        float distance = Fighter.DistanceToPlayer();

        if (distance <= Fighter.WeaponRange)
        {
            stateMachine.TransitionTo("Joust");
            return;
        }

        Fighter.PursuePlayer(delta, skipFacing: true);
        Fighter.FaceTarget(delta);
    }
}
