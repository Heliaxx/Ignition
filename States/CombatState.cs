using Godot;

public partial class CombatState : State
{
    protected Fighter Fighter => Entity as Fighter;

    public override void PhysicsUpdate(float delta)
    {
        if (Fighter == null || !Fighter.HasTarget()) return;

        if (Fighter.GetHealthPercent() <= Fighter.FleeHealthThreshold)
            { stateMachine.TransitionTo("Flee"); return; }

        if (Fighter.TookRecentDamage)
            { stateMachine.TransitionTo("Evade"); return; }

        CombatUpdate(delta);
    }

    protected virtual void CombatUpdate(float delta) {}
}
