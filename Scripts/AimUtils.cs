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
}
