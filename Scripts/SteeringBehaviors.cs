using Godot;
using System;

/// <summary>
/// Static class implementing Craig Reynolds' steering behaviors for autonomous agent movement.
/// Methods return a steering force (desired velocity change), not final velocity.
/// </summary>
public static class SteeringBehaviors
{
    /// <summary>
    /// Seek: Move directly toward a target position.
    /// </summary>
    public static Vector3 Seek(Vector3 position, Vector3 targetPosition, float maxSpeed)
    {
        Vector3 desired = (targetPosition - position).Normalized() * maxSpeed;
        return desired;
    }

    /// <summary>
    /// Flee: Move directly away from a target position.
    /// </summary>
    public static Vector3 Flee(Vector3 position, Vector3 targetPosition, float maxSpeed)
    {
        Vector3 desired = (position - targetPosition).Normalized() * maxSpeed;
        return desired;
    }

    /// <summary>
    /// Pursue: Intercept a moving target by predicting where it will be.
    /// More natural than Seek - leads the target like a real pilot would.
    /// </summary>
    public static Vector3 Pursue(Vector3 position, Vector3 currentVelocity, float maxSpeed,
                                   Vector3 targetPosition, Vector3 targetVelocity)
    {
        Vector3 toTarget = targetPosition - position;
        float distance = toTarget.Length();
        
        // Estimate time to intercept based on closing speed
        float mySpeed = Mathf.Max(currentVelocity.Length(), maxSpeed * 0.5f);
        float lookAheadTime = distance / (mySpeed + targetVelocity.Length() + 0.01f);
        
        // Clamp look-ahead to prevent chasing ghosts
        lookAheadTime = Mathf.Min(lookAheadTime, 2.0f);
        
        // Predict where the target will be
        Vector3 predictedPosition = targetPosition + targetVelocity * lookAheadTime;
        
        return Seek(position, predictedPosition, maxSpeed);
    }

    /// <summary>
    /// Evade: Flee from where a moving target will be
    /// </summary>
    public static Vector3 Evade(Vector3 position, Vector3 currentVelocity, float maxSpeed,
                                  Vector3 targetPosition, Vector3 targetVelocity)
    {
        Vector3 toTarget = targetPosition - position;
        float distance = toTarget.Length();
        
        float mySpeed = Mathf.Max(currentVelocity.Length(), maxSpeed * 0.5f);
        float lookAheadTime = distance / (mySpeed + targetVelocity.Length() + 0.01f);
        lookAheadTime = Mathf.Min(lookAheadTime, 1.5f);
        
        Vector3 predictedPosition = targetPosition + targetVelocity * lookAheadTime;
        
        return Flee(position, predictedPosition, maxSpeed);
    }

    /// <summary>
    /// Arrive: Move toward target but decelerate getting closer.
    /// Prevents the overshooting and oscillation when near the target.
    /// </summary>
    public static Vector3 Arrive(Vector3 position, Vector3 targetPosition, float maxSpeed, float slowingRadius)
    {
        Vector3 toTarget = targetPosition - position;
        float distance = toTarget.Length();
        
        if (distance < 0.1f)
            return Vector3.Zero;
        
        // Ramp down speed when entering the slowing radius
        float rampedSpeed = maxSpeed * (distance / slowingRadius);
        float clippedSpeed = Mathf.Min(rampedSpeed, maxSpeed);
        
        Vector3 desired = toTarget.Normalized() * clippedSpeed;
        return desired;
    }

    /// <summary>
    /// Wander: Add organic randomness to movement. Returns a small deviation force.
    /// Uses a "wander circle" projected in front of the agent.
    /// </summary>
    public static Vector3 Wander(Vector3 forward, float wanderStrength, float wanderRadius, ref float wanderAngle)
    {
        // Gradually shift the wander angle
        wanderAngle += (GD.Randf() - 0.5f) * 0.5f;
        
        // Create a point on a circle in front of the agent
        Vector3 right = forward.Cross(Vector3.Up).Normalized();
        if (right.LengthSquared() < 0.01f)
            right = forward.Cross(Vector3.Forward).Normalized();
        
        Vector3 up = right.Cross(forward).Normalized();
        
        // Circle displacement
        Vector3 displacement = right * Mathf.Cos(wanderAngle) + up * Mathf.Sin(wanderAngle);
        displacement *= wanderRadius;
        
        // Project circle ahead and add displacement
        Vector3 wanderForce = (forward * wanderStrength + displacement).Normalized() * wanderStrength;
        
        return wanderForce;
    }

    /// <summary>
    /// MaintainDistance: Steering force to stay at a preferred range from target.
    /// Returns pursue if too far, evade if too close, zero if just right.
    /// </summary>
    public static Vector3 MaintainDistance(Vector3 position, Vector3 currentVelocity, float maxSpeed,
                                             Vector3 targetPosition, Vector3 targetVelocity,
                                             float preferredDistance, float tolerance)
    {
        float distance = (targetPosition - position).Length();
        float minRange = preferredDistance - tolerance;
        float maxRange = preferredDistance + tolerance;
        
        if (distance > maxRange)
        {
            // Too far - pursue
            return Pursue(position, currentVelocity, maxSpeed, targetPosition, targetVelocity);
        }
        else if (distance < minRange)
        {
            // Too close - evade
            return Evade(position, currentVelocity, maxSpeed, targetPosition, targetVelocity);
        }
        
        // Within range - just maintain heading toward target 
        return Vector3.Zero;
    }

    /// <summary>
    /// OffsetPursuit: Pursue to a position offset from the target (e.g., stay behind them).
    /// Tailing or strafing maneuvers.
    /// </summary>
    public static Vector3 OffsetPursuit(Vector3 position, Vector3 currentVelocity, float maxSpeed,
                                          Vector3 targetPosition, Vector3 targetVelocity, Vector3 targetForward,
                                          Vector3 localOffset)
    {
        // Transform local offset to world space relative to target's orientation
        Vector3 targetRight = targetForward.Cross(Vector3.Up).Normalized();
        Vector3 targetUp = targetRight.Cross(targetForward).Normalized();
        
        Vector3 worldOffset = targetForward * localOffset.Z + 
                             targetRight * localOffset.X + 
                             targetUp * localOffset.Y;
        
        Vector3 offsetPosition = targetPosition + worldOffset;
        
        return Pursue(position, currentVelocity, maxSpeed, offsetPosition, targetVelocity);
    }

    // Pre-allocated arrays to avoid per-frame GC allocations
    [ThreadStatic] private static Vector3[] _rayDirections;
    [ThreadStatic] private static Godot.Collections.Array<Rid> _excludeArray;
    [ThreadStatic] private static PhysicsRayQueryParameters3D _rayQuery;

    private static void EnsureRayBuffers()
    {
        _rayDirections ??= new Vector3[9];
        _excludeArray ??= new Godot.Collections.Array<Rid> { default };
        _rayQuery ??= new PhysicsRayQueryParameters3D();
    }

    /// <summary>
    /// ObstacleAvoidance: raycast ahead and to the sides, steer away from detected obstacles.
    /// Returns a steering force away from the nearest obstacle, or zero if path is clear.
    /// Also outputs an avoidance weight (0-1) for blending with other steering.
    /// </summary>
    public static Vector3 ObstacleAvoidance(PhysicsDirectSpaceState3D spaceState,
                                              Vector3 position, Vector3 velocity, Vector3 forward,
                                              float avoidanceForce, float lookAheadDistance,
                                              uint collisionMask, Rid excludeRid,
                                              out float avoidanceUrgency)
    {
        avoidanceUrgency = 0f;

        if (spaceState == null)
            return Vector3.Zero;

        EnsureRayBuffers();

        Vector3 moveDir = velocity.LengthSquared() > 1f ? velocity.Normalized() : forward;

        // Build orthogonal vectors for side rays
        Vector3 right = moveDir.Cross(Vector3.Up).Normalized();
        if (right.LengthSquared() < 0.01f)
            right = moveDir.Cross(Vector3.Forward).Normalized();
        Vector3 up = right.Cross(moveDir).Normalized();

        // Fill pre-allocated ray directions array
        _rayDirections[0] = moveDir;                                                    // Center
        _rayDirections[1] = (moveDir + right * 0.7f).Normalized();                     // Right
        _rayDirections[2] = (moveDir - right * 0.7f).Normalized();                     // Left
        _rayDirections[3] = (moveDir + up * 0.5f).Normalized();                        // Up
        _rayDirections[4] = (moveDir - up * 0.5f).Normalized();                        // Down
        _rayDirections[5] = (moveDir + right * 0.35f + up * 0.35f).Normalized();       // Upper-right
        _rayDirections[6] = (moveDir - right * 0.35f + up * 0.35f).Normalized();       // Upper-left
        _rayDirections[7] = (moveDir + right * 0.35f - up * 0.35f).Normalized();       // Lower-right
        _rayDirections[8] = (moveDir - right * 0.35f - up * 0.35f).Normalized();       // Lower-left

        Vector3 totalAvoidance = Vector3.Zero;
        int hitCount = 0;
        float closestDistSq = lookAheadDistance * lookAheadDistance;

        // Reuse exclude array
        _excludeArray[0] = excludeRid;

        // Reuse query object
        _rayQuery.CollisionMask = collisionMask;
        _rayQuery.Exclude = _excludeArray;

        for (int i = 0; i < 9; i++)
        {
            Vector3 rayDir = _rayDirections[i];
            _rayQuery.From = position;
            _rayQuery.To = position + rayDir * lookAheadDistance;

            var result = spaceState.IntersectRay(_rayQuery);

            if (result.Count > 0)
            {
                Vector3 hitPoint = (Vector3)result["position"];
                Vector3 hitNormal = (Vector3)result["normal"];
                float distSq = (hitPoint - position).LengthSquared();

                if (distSq < closestDistSq)
                    closestDistSq = distSq;

                // Urgency based on squared distance (avoids sqrt)
                float normalizedDistSq = distSq / (lookAheadDistance * lookAheadDistance);
                float urgency = (1f - normalizedDistSq); // linear in squared space ≈ quadratic in linear

                Vector3 avoidDir = hitNormal;
                if (avoidDir.Dot(moveDir) < -0.5f)
                {
                    avoidDir = right * (rayDir.Dot(right) < 0 ? 1f : -1f);
                }

                totalAvoidance += avoidDir.Normalized() * urgency;
                hitCount++;
            }
        }

        if (hitCount == 0)
            return Vector3.Zero;

        totalAvoidance = totalAvoidance.Normalized();

        float closestNormSq = closestDistSq / (lookAheadDistance * lookAheadDistance);
        avoidanceUrgency = Mathf.Clamp(1f - closestNormSq, 0f, 1f);

        return totalAvoidance * avoidanceForce * (0.5f + avoidanceUrgency * 0.5f);
    }

    /// <summary>
    /// Simplified overload without urgency output for backward compatibility.
    /// </summary>
    public static Vector3 ObstacleAvoidance(PhysicsDirectSpaceState3D spaceState,
                                              Vector3 position, Vector3 velocity, Vector3 forward,
                                              float avoidanceForce, float lookAheadDistance,
                                              uint collisionMask, Rid excludeRid)
    {
        return ObstacleAvoidance(spaceState, position, velocity, forward,
                                  avoidanceForce, lookAheadDistance, collisionMask, excludeRid,
                                  out _);
    }

    /// <summary>
    /// Applies a steering force to current velocity with mass/acceleration consideration.
    /// Returns the new velocity (clamped to maxSpeed).
    /// </summary>
    public static Vector3 ApplySteering(Vector3 currentVelocity, Vector3 steeringForce,
                                          float maxSpeed, float maxForce, float delta)
    {
        // Clamp steering force (squared comparison avoids sqrt)
        if (steeringForce.LengthSquared() > maxForce * maxForce)
        {
            steeringForce = steeringForce.Normalized() * maxForce;
        }

        Vector3 newVelocity = currentVelocity + steeringForce * delta;

        // Clamp to max speed
        if (newVelocity.LengthSquared() > maxSpeed * maxSpeed)
        {
            newVelocity = newVelocity.Normalized() * maxSpeed;
        }

        return newVelocity;
    }
}
