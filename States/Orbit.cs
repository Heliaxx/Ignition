using Godot;

public partial class Orbit : CombatState
{
    private float _orbitDirection = 1f; // 1 = right, -1 = left
    private float _directionChangeTimer = 0f;
    private const float MIN_DIRECTION_CHANGE = 2f;
    private const float MAX_DIRECTION_CHANGE = 5f;
    private const float ORBIT_OFFSET = 60f; // lateral offset distance

    public override void Enter()
    {
        _orbitDirection = GD.Randf() < 0.5f ? 1f : -1f;
        _directionChangeTimer = (float)GD.RandRange(MIN_DIRECTION_CHANGE, MAX_DIRECTION_CHANGE);
        Fighter?.SetLaserFiring(false);
    }

    public override void Exit()
    {
        Fighter?.SetLaserFiring(false);
    }

    protected override void CombatUpdate(float delta)
    {
        float distance = Fighter.DistanceToPlayer();

        if (distance > Fighter.CloseRange * 2.5f)
        {
            stateMachine.TransitionTo("Joust");
            return;
        }

        _directionChangeTimer -= delta;
        if (_directionChangeTimer <= 0f)
        {
            _orbitDirection = -_orbitDirection;
            _directionChangeTimer = (float)GD.RandRange(MIN_DIRECTION_CHANGE, MAX_DIRECTION_CHANGE);
        }

        Vector3 playerForward = (Fighter.GetPlayerVelocity().LengthSquared() > 1f)
            ? Fighter.GetPlayerVelocity().Normalized()
            : (Fighter.GlobalPosition - Fighter.GetPlayerPosition()).Normalized();

        Vector3 localOffset = new Vector3(_orbitDirection * ORBIT_OFFSET, 0f, 0f);

        Vector3 steering = SteeringBehaviors.OffsetPursuit(
            Fighter.GlobalPosition, Fighter.Velocity, Fighter.Speed,
            Fighter.GetPlayerPosition(), Fighter.GetPlayerVelocity(),
            playerForward, localOffset
        );

        Fighter.MoveInDirection(steering.Normalized(), delta, skipFacing: true);
        Fighter.FaceTarget(delta);
        Fighter.SetLaserFiring(Fighter.IsPlayerInFiringArc());
    }
}
