using Godot;

public partial class Idle : CombatState
{
    [Export] public float MaxWanderRadius = 1000f;

    private float detectionRangeSquared;
    private float maxWanderRadiusSquared;
    private ReturnToCenter _returnToCenter;

    public override void Enter()
    {
        float range = Fighter?.DetectionRange ?? 600f;
        detectionRangeSquared = range * range;
        maxWanderRadiusSquared = MaxWanderRadius * MaxWanderRadius;
        _returnToCenter = stateMachine.GetNodeOrNull<ReturnToCenter>("ReturnToCenter");
        Fighter?.SetLaserFiring(false);
    }

    public override void PhysicsUpdate(float delta)
    {
        if (Fighter == null || !Fighter.HasTarget()) return;

        Fighter.Wander(delta);

        if (_returnToCenter != null)
        {
            float distSquared = Fighter.GlobalPosition.DistanceSquaredTo(_returnToCenter.CenterPoint);
            if (distSquared > maxWanderRadiusSquared)
            {
                stateMachine.TransitionTo("ReturnToCenter");
                return;
            }
        }

        if (Fighter.DistanceSquaredToPlayer() < detectionRangeSquared)
            stateMachine.TransitionTo("Pursue");
    }
}
