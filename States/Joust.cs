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

    protected override void CombatUpdate(float delta)
    {
        float distance = Fighter.DistanceToPlayer();

        if (distance > Fighter.WeaponRange * 1.2f)
        {
            stateMachine.TransitionTo("Pursue");
            return;
        }

        if (distance < Fighter.CloseRange)
        {
            stateMachine.TransitionTo("Orbit");
            return;
        }

        Fighter.PursuePlayer(delta, skipFacing: true);
        Fighter.FaceTarget(delta);
        Fighter.SetLaserFiring(Fighter.IsPlayerInFiringArc());
    }
}
