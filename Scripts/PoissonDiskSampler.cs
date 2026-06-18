using Godot;
using System;
using System.Collections.Generic;

// 3D Poisson disk sampling with a guaranteed minimum spacing between points.
// Uses System.Random rather than Godot RNG, so it is safe on worker threads.
public static class PoissonDiskSampler
{
    private const int MaxAttempts = 30;

    // Generates points at least minDistance apart within a cubic region.
    public static List<Vector3> GeneratePoints(Vector3 regionSize, float minDistance, ulong seed)
    {
        // Fold the 64-bit chunk seed into a 32-bit int for System.Random
        var rng = new Random((int)(seed ^ (seed >> 32)));

        float cellSize = minDistance / Mathf.Sqrt(3f); // 3D diagonal

        int gridWidth  = Mathf.CeilToInt(regionSize.X / cellSize);
        int gridHeight = Mathf.CeilToInt(regionSize.Y / cellSize);
        int gridDepth  = Mathf.CeilToInt(regionSize.Z / cellSize);

        int[,,] grid = new int[gridWidth, gridHeight, gridDepth];
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                for (int z = 0; z < gridDepth; z++)
                    grid[x, y, z] = -1;

        List<Vector3> points     = new List<Vector3>();
        List<Vector3> activeList = new List<Vector3>();

        Vector3 firstPoint = new Vector3(
            (float)(rng.NextDouble() * regionSize.X),
            (float)(rng.NextDouble() * regionSize.Y),
            (float)(rng.NextDouble() * regionSize.Z)
        );

        points.Add(firstPoint);
        activeList.Add(firstPoint);

        var gridPos = WorldToGrid(firstPoint, cellSize);
        if (IsValidGridPos(gridPos, gridWidth, gridHeight, gridDepth))
            grid[gridPos.X, gridPos.Y, gridPos.Z] = 0;

        while (activeList.Count > 0)
        {
            int randomIndex = rng.Next(0, activeList.Count);
            Vector3 point = activeList[randomIndex];
            bool foundValid = false;

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                Vector3 dir = new Vector3(
                    (float)(rng.NextDouble() * 2 - 1),
                    (float)(rng.NextDouble() * 2 - 1),
                    (float)(rng.NextDouble() * 2 - 1)
                ).Normalized();

                float dist = (float)(minDistance + rng.NextDouble() * minDistance); // [min, 2*min]
                Vector3 candidate = point + dir * dist;

                if (candidate.X < 0 || candidate.X >= regionSize.X ||
                    candidate.Y < 0 || candidate.Y >= regionSize.Y ||
                    candidate.Z < 0 || candidate.Z >= regionSize.Z)
                    continue;

                if (IsValidPoint(candidate, grid, points, cellSize, minDistance, gridWidth, gridHeight, gridDepth))
                {
                    points.Add(candidate);
                    activeList.Add(candidate);

                    var gp = WorldToGrid(candidate, cellSize);
                    if (IsValidGridPos(gp, gridWidth, gridHeight, gridDepth))
                        grid[gp.X, gp.Y, gp.Z] = points.Count - 1;

                    foundValid = true;
                    break;
                }
            }

            if (!foundValid)
                activeList.RemoveAt(randomIndex);
        }

        return points;
    }

    private static (int X, int Y, int Z) WorldToGrid(Vector3 worldPos, float cellSize)
    {
        return (
            Mathf.FloorToInt(worldPos.X / cellSize),
            Mathf.FloorToInt(worldPos.Y / cellSize),
            Mathf.FloorToInt(worldPos.Z / cellSize)
        );
    }

    private static bool IsValidGridPos(in (int X, int Y, int Z) pos, int w, int h, int d)
    {
        return pos.X >= 0 && pos.X < w && pos.Y >= 0 && pos.Y < h && pos.Z >= 0 && pos.Z < d;
    }

    private static bool IsValidPoint(Vector3 candidate, int[,,] grid, List<Vector3> points,
        float cellSize, float minDistance, int gridWidth, int gridHeight, int gridDepth)
    {
        var gp = WorldToGrid(candidate, cellSize);

        float minDistSq = minDistance * minDistance;

        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dz = -2; dz <= 2; dz++)
                {
                    int nx = gp.X + dx;
                    int ny = gp.Y + dy;
                    int nz = gp.Z + dz;

                    if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight || nz < 0 || nz >= gridDepth)
                        continue;

                    int pointIndex = grid[nx, ny, nz];
                    if (pointIndex >= 0)
                    {
                        if (candidate.DistanceSquaredTo(points[pointIndex]) < minDistSq)
                            return false;
                    }
                }
            }
        }

        return true;
    }
}
