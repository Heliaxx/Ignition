using Godot;

public partial class Evade : CombatState
{
    private float _evadeTimer = 0f;
    private const float MIN_EVADE_TIME = 2f;
    private const float MAX_EVADE_TIME = 3.5f;

    public override void Enter()
    {
        _evadeTimer = (float)GD.RandRange(MIN_EVADE_TIME, MAX_EVADE_TIME);
        Fighter?.SetLaserFiring(false);
    }

    public override void PhysicsUpdate(float delta)
    {
        if (Fighter == null || !Fighter.HasTarget()) return;

        // Flee check — even while evading, if health is critical, flee
        if (Fighter.GetHealthPercent() <= Fighter.FleeHealthThreshold)
        {
            stateMachine.TransitionTo("Flee");
            return;
        }

        _evadeTimer -= delta;

        if (_evadeTimer <= 0f)
        {
            // Re-engage: pick state based on distance
            float distance = Fighter.DistanceToPlayer();
            if (distance <= Fighter.WeaponRange)
                stateMachine.TransitionTo("Joust");
            else
                stateMachine.TransitionTo("Pursue");
            return;
        }

        // Evasive maneuvers
        Fighter.EvadePlayer(delta);
    }
}
