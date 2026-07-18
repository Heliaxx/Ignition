using Godot;

// Shared aiming math for weapons, missiles, and lead reticles.
public static class AimUtils
{
    // Returns the world-space point to aim at so a projectile of the given speed
    // intercepts a moving target. Iterates to converge on the time-of-flight.
    public static Vector3 PredictIntercept(Vector3 shooterPos, Vector3 targetPos,
                                           Vector3 targetVel, float projectileSpeed,
                                           int iterations = 2)
    {
        float speed = Mathf.Max(projectileSpeed, 1f);
        Vector3 intercept = targetPos;
        float dist = shooterPos.DistanceTo(targetPos);
        for (int i = 0; i < iterations; i++)
        {
            float tof = dist / speed;
            intercept = targetPos + targetVel * tof;
            dist = shooterPos.DistanceTo(intercept);
        }
        return intercept;
    }

    // Proportional Navigation commanded acceleration. Flies so the missile's
    // bearing to the target stays constant until impact (constant-bearing,
    // decreasing-range). Returns the world-space acceleration command; steer the
    // missile toward (position + command).
    public static Vector3 ProportionalNavigation(Vector3 missilePos, Vector3 missileVel,
                                                 Vector3 targetPos, Vector3 targetVel,
                                                 float pnGain = 4f)
    {
        Vector3 los = targetPos - missilePos;              // line of sight
        float range = los.Length();
        if (range < 0.5f)
            return targetPos - missilePos;

        Vector3 losUnit = los / range;
        Vector3 relativeVel = targetVel - missileVel;
        float closingVel = -relativeVel.Dot(losUnit);
        // LOS rotation rate (rad/s) as a vector.
        Vector3 losRate = los.Cross(relativeVel) / (range * range);
        return pnGain * closingVel * losRate.Cross(losUnit);
    }
}
