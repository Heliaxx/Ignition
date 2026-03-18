using Godot;

public partial class Flee : CombatState
{
    public override void Enter()
    {
        Fighter?.SetLaserFiring(false);
    }

    public override void PhysicsUpdate(float delta)
    {
        if (Fighter == null || !Fighter.HasTarget()) return;

        // Full speed evasion away from player
        Fighter.EvadePlayer(delta);
    }
}
