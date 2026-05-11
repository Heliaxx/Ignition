using Godot;
using System.Collections.Generic;

/// <summary>
/// Manages a streaming asteroid field using chunked Poisson disk sampling
/// with GPU instancing via MultiMeshInstance3D for high performance.
/// </summary>
public partial class ChunkedAsteroidField : Node3D
{
    private static readonly string[] AsteroidScenePaths =
    {
        "res://Scenes/Asteroid2.tscn",
        "res://Scenes/Asteroid3.tscn"
    };

    #region Exports

    [ExportGroup("Chunk Settings")]
    [Export]
    public float ChunkSize = 400f;

    [Export]
    public int LoadRadius = 10; // Chunks loaded around player in each direction

    [Export]
    public int UnloadBuffer = 1; // Extra distance before unloading

    [Export]
    public int CollisionRadius = 5; // Radius of chunks with collision shapes (should be <= LoadRadius)

    [Export]
    public int ChunksPerFrame = 1; // Chunks loaded per _Process frame (Optimization)

    [ExportSubgroup("Vertical Limits")]
    [Export]
    public bool LimitUpward = false; // Limit chunk generation above Y=0

    [Export]
    public int MaxChunksUp = 2; // Max chunks above Y=0 (if LimitUpward is true)

    [Export]
    public bool LimitDownward = false; // Limit chunk generation below Y=0

    [Export]
    public int MaxChunksDown = 2; // Max chunks below Y=0 (if LimitDownward is true)

    [ExportGroup("Asteroid Settings")]
    [Export(PropertyHint.Range, "25,2000,25")]
    public float MinSpacing = 400f; // Minimum distance between asteroids for Poisson sampling

    [Export(PropertyHint.Range, "5,200,5")]
    public float MinScale = 25f; // Minimum asteroid scale

    [Export(PropertyHint.Range, "5,200,5")]
    public float MaxScale = 50f; // Maximum asteroid scale

    [Export]
    public bool AsteroidsDestroyable = true;

    [Export(PropertyHint.Range, "10,500,10")]
    public int AsteroidHP = 50;

    [ExportGroup("Spawn Clearance")]
    [Export]
    public Node3D[] SpawnExclusionZones; // Asteroids will not spawn within SpawnExclusionRadius of these nodes

    [Export]
    public float SpawnExclusionRadius = 200f; // Minimum spawn distance from exclusion zones

    [ExportGroup("Performance")]
    [Export(PropertyHint.Range, "0.1,50.0,5.0")]
    public float LodBias = 15.0f; // Higher = more detail at distance

    public ulong WorldSeed = 0;

    #endregion

    // Multiple asteroid variants
    private Mesh[] _asteroidMeshes;
    private Material[] _asteroidMaterials;
    private Shape3D[] _collisionShapes;

    // Loaded chunks: key = chunk coordinate hash
    private Dictionary<Vector3I, ChunkData> _loadedChunks = new();

    // Queue of chunk coords waiting to be loaded (processed gradually in _Process)
    private Queue<Vector3I> _loadQueue = new();

    // Destroyed asteroids: persisted across chunk reloads
    private HashSet<ulong> _destroyedAsteroids = new();

    // Current HP per asteroid (only tracked while above 0 and below max)
    private Dictionary<ulong, int> _asteroidHealth = new();

    // Player reference
    private Node3D _player;
    private Vector3I _lastPlayerChunk;

    private class ChunkData
    {
        public List<MultiMeshInstance3D> MultiMeshInstances = new();
        public Dictionary<int, MultiMeshInstance3D> MmiByVariant = new();
        public List<AsteroidInstance> Asteroids;
        public AsteroidBody CollisionBody;
    }

    private struct AsteroidInstance
    {
        public ulong Id;
        public Vector3 Position;
        public Vector3 Rotation;
        public float Scale;
        public int MeshVariant;
    }

    public override void _Ready()
    {
        // Generate random seed if not set
        if (WorldSeed == 0)
        {
            GD.Randomize();
            WorldSeed = (ulong)GD.Randi() << 32 | GD.Randi();
        }
        
        AddToGroup("asteroid_field");
        LoadAsteroidMeshes();

        // Find player - adjust path as needed
        _player = GetTree().GetFirstNodeInGroup("Player") as Node3D;
        if (_player == null)
        {
            GD.PrintErr("ChunkedAsteroidField: No player found in 'Player' group");
            return;
        }

        _lastPlayerChunk = WorldToChunk(_player.GlobalPosition);
        UpdateChunks();

        // Pre-load all initial chunks synchronously so the level starts fully populated
        while (_loadQueue.Count > 0)
        {
            var coord = _loadQueue.Dequeue();
            if (!_loadedChunks.ContainsKey(coord))
                LoadChunk(coord);
        }
    }

    public override void _Process(double delta)
    {
        if (_player == null) return;

        Vector3I currentChunk = WorldToChunk(_player.GlobalPosition);

        // Only update when player crosses chunk boundary
        if (currentChunk != _lastPlayerChunk)
        {
            _lastPlayerChunk = currentChunk;
            UpdateChunks();
        }

        // Gradually load queued chunks to avoid frame spikes
        ProcessChunkQueue();
    }

    private void LoadAsteroidMeshes()
    {
        var meshes = new List<Mesh>();
        var materials = new List<Material>();
        var collisionShapes = new List<Shape3D>();

        foreach (var scenePath in AsteroidScenePaths)
        {
            var asteroidScene = GD.Load<PackedScene>(scenePath);
            if (asteroidScene == null)
            {
                GD.PrintErr($"ChunkedAsteroidField: Could not load asteroid scene '{scenePath}'");
                continue;
            }

            var instance = asteroidScene.Instantiate<Node3D>();
            var meshInstance = FindMeshInstance(instance);

            if (meshInstance?.Mesh != null)
            {
                meshes.Add(meshInstance.Mesh);

                Material material = meshInstance.GetActiveMaterial(0);
                if (material == null)
                {
                    material = meshInstance.Mesh.SurfaceGetMaterial(0);
                }
                materials.Add(material);

                var collisionShape = FindCollisionShape(instance);
                if (collisionShape?.Shape != null)
                {
                    collisionShapes.Add(collisionShape.Shape);
                }
                else
                {
                    collisionShapes.Add(null);
                    GD.PrintErr($"ChunkedAsteroidField: No collision shape found in '{scenePath}'");
                }
            }
            else
            {
                GD.PrintErr($"ChunkedAsteroidField: Could not extract mesh from asteroid scene '{scenePath}'");
            }

            instance.QueueFree();
        }

        _asteroidMeshes = meshes.ToArray();
        _asteroidMaterials = materials.ToArray();
        _collisionShapes = collisionShapes.ToArray();

        if (_asteroidMeshes.Length == 0)
        {
            GD.PrintErr("ChunkedAsteroidField: No asteroid meshes loaded");
        }

        if (_collisionShapes.Length == 0)
        {
            GD.PrintErr("ChunkedAsteroidField: No collision shapes loaded!");
        }
    }

    private MeshInstance3D FindMeshInstance(Node node)
    {
        if (node is MeshInstance3D mi && mi.Mesh != null)
            return mi;

        foreach (var child in node.GetChildren())
        {
            var result = FindMeshInstance(child);
            if (result != null) return result;
        }
        return null;
    }

    private CollisionShape3D FindCollisionShape(Node node)
    {
        if (node is CollisionShape3D cs && cs.Shape != null)
            return cs;

        foreach (var child in node.GetChildren())
        {
            var result = FindCollisionShape(child);
            if (result != null) return result;
        }
        return null;
    }

    private void UpdateChunks()
    {
        Vector3I playerChunk = _lastPlayerChunk;

        // Build the set of chunks that should be loaded: all coords within a spherical
        // radius of LoadRadius chunks around the player, respecting optional Y clamping.
        HashSet<Vector3I> shouldBeLoaded = new();

        for (int x = -LoadRadius; x <= LoadRadius; x++)
        {
            for (int y = -LoadRadius; y <= LoadRadius; y++)
            {
                for (int z = -LoadRadius; z <= LoadRadius; z++)
                {
                    // Sphere check: skip corners of the cube that exceed LoadRadius
                    if (x * x + y * y + z * z <= LoadRadius * LoadRadius + 1)
                    {
                        int chunkY = playerChunk.Y + y;

                        // Vertical limits check: skip chunks exceeding upward/downward limits, if enabled
                        if (LimitUpward && chunkY > MaxChunksUp)
                            continue;
                        if (LimitDownward && chunkY < -MaxChunksDown)
                            continue;

                        shouldBeLoaded.Add(playerChunk + new Vector3I(x, y, z));
                    }
                }
            }
        }

        // Unload any chunks that have gone beyond LoadRadius + UnloadBuffer.
        // The buffer prevents thrashing when the player hovers near a chunk boundary.
        List<Vector3I> toUnload = new();
        int unloadDist = LoadRadius + UnloadBuffer;

        foreach (var kvp in _loadedChunks)
        {
            Vector3I diff = kvp.Key - playerChunk;
            if (Mathf.Abs(diff.X) > unloadDist ||
                Mathf.Abs(diff.Y) > unloadDist ||
                Mathf.Abs(diff.Z) > unloadDist)
            {
                toUnload.Add(kvp.Key);
            }
        }

        foreach (var coord in toUnload)
        {
            UnloadChunk(coord);
        }

        // Enqueue not-yet-loaded chunks, nearest first, so the area immediately
        // around the player always populates before distant chunks.
        _loadQueue.Clear();
        var pending = new List<Vector3I>();
        foreach (var coord in shouldBeLoaded)
        {
            if (!_loadedChunks.ContainsKey(coord))
                pending.Add(coord);
        }
        pending.Sort((a, b) =>
        {
            var da = a - playerChunk;
            var db = b - playerChunk;
            return (da.X * da.X + da.Y * da.Y + da.Z * da.Z)
                .CompareTo(db.X * db.X + db.Y * db.Y + db.Z * db.Z);
        });
        foreach (var coord in pending)
            _loadQueue.Enqueue(coord);

        // Add/remove collision bodies on chunks based on CollisionRadius
        UpdateCollisionBodies();
    }

    private void LoadChunk(Vector3I coord)
    {
        // Generate deterministic seed for the chunk
        ulong chunkSeed = GenerateChunkSeed(coord);

        // Generate asteroid positions using Poisson disk sampling
        Vector3 regionSize = new Vector3(ChunkSize, ChunkSize, ChunkSize);
        var points = PoissonDiskSampler.GeneratePoints(regionSize, MinSpacing, chunkSeed);

        // Create asteroid instances
        var rng = new RandomNumberGenerator();
        rng.Seed = chunkSeed;

        List<AsteroidInstance> asteroids = new();
        Vector3 chunkOrigin = ChunkToWorld(coord);

        foreach (var localPos in points)
        {
            ulong asteroidId = GenerateAsteroidId(coord, localPos);

            // Skip destroyed asteroids
            if (_destroyedAsteroids.Contains(asteroidId))
                continue;

            Vector3 worldPos = chunkOrigin + localPos - regionSize * 0.5f;
            if (IsInExclusionZone(worldPos))
                continue;

            var asteroid = new AsteroidInstance
            {
                Id = asteroidId,
                Position = worldPos,
                Rotation = new Vector3(
                    rng.RandfRange(0, Mathf.Tau),
                    rng.RandfRange(0, Mathf.Tau),
                    rng.RandfRange(0, Mathf.Tau)
                ),
                Scale = rng.RandfRange(MinScale, MaxScale),
                MeshVariant = _asteroidMeshes.Length > 0 ? rng.RandiRange(0, _asteroidMeshes.Length - 1) : 0
            };
            asteroids.Add(asteroid);
        }

        // Create MultiMeshInstance3D instances per mesh variant for GPU instancing
        List<MultiMeshInstance3D> mmis = new();
        var mmiByVariant = new Dictionary<int, MultiMeshInstance3D>();

        if (_asteroidMeshes.Length > 0 && asteroids.Count > 0)
        {
            for (int meshIndex = 0; meshIndex < _asteroidMeshes.Length; meshIndex++)
            {
                if (_asteroidMeshes[meshIndex] == null)
                    continue;

                var variantAsteroids = asteroids.FindAll(a => a.MeshVariant == meshIndex);
                if (variantAsteroids.Count == 0)
                    continue;

                var mmi = new MultiMeshInstance3D();
                var multiMesh = new MultiMesh();
                multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
                multiMesh.Mesh = _asteroidMeshes[meshIndex];
                multiMesh.InstanceCount = variantAsteroids.Count;

                for (int i = 0; i < variantAsteroids.Count; i++)
                {
                    var a = variantAsteroids[i];
                    var localPos = a.Position - chunkOrigin;
                    var transform = new Transform3D(Basis.Identity, localPos);
                    transform.Basis = transform.Basis.Rotated(Vector3.Right, a.Rotation.X);
                    transform.Basis = transform.Basis.Rotated(Vector3.Up, a.Rotation.Y);
                    transform.Basis = transform.Basis.Rotated(Vector3.Forward, a.Rotation.Z);
                    transform.Basis = transform.Basis.Scaled(new Vector3(a.Scale, a.Scale, a.Scale));

                    multiMesh.SetInstanceTransform(i, transform);
                }

                mmi.Multimesh = multiMesh;
                mmi.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
                mmi.LodBias = LodBias;
                mmi.Position = chunkOrigin; // node lives at chunk origin; instances use local coords

                if (_asteroidMaterials != null && meshIndex < _asteroidMaterials.Length)
                {
                    mmi.MaterialOverride = _asteroidMaterials[meshIndex];
                }

                AddChild(mmi);
                mmis.Add(mmi);
                mmiByVariant[meshIndex] = mmi;
            }
        }

        var chunkData = new ChunkData
        {
            MultiMeshInstances = mmis,
            MmiByVariant = mmiByVariant,
            Asteroids = asteroids,
            CollisionBody = null
        };

        _loadedChunks[coord] = chunkData;

        // Add collision body immediately if within collision radius
        if (IsWithinCollisionRadius(coord))
            CreateCollisionBody(chunkData);
    }

    private void UnloadChunk(Vector3I coord)
    {
        if (_loadedChunks.TryGetValue(coord, out var chunk))
        {
            foreach (var mmi in chunk.MultiMeshInstances)
            {
                mmi?.QueueFree();
            }
            
            chunk.CollisionBody?.QueueFree();
            
            _loadedChunks.Remove(coord);
        }
    }

    private bool IsInExclusionZone(Vector3 worldPos)
    {
        float radiusSq = SpawnExclusionRadius * SpawnExclusionRadius;

        // Always keep clear of the player
        if (_player != null && worldPos.DistanceSquaredTo(_player.GlobalPosition) < radiusSq)
            return true;

        if (SpawnExclusionZones == null) return false;
        foreach (var zone in SpawnExclusionZones)
        {
            if (zone == null) continue;
            if (worldPos.DistanceSquaredTo(zone.GlobalPosition) < radiusSq)
                return true;
        }
        return false;
    }

    private Vector3I WorldToChunk(Vector3 worldPos)
    {
        return new Vector3I(
            Mathf.FloorToInt(worldPos.X / ChunkSize),
            Mathf.FloorToInt(worldPos.Y / ChunkSize),
            Mathf.FloorToInt(worldPos.Z / ChunkSize)
        );
    }

    private Vector3 ChunkToWorld(Vector3I chunkCoord) => (Vector3)chunkCoord * ChunkSize;

    private ulong GenerateChunkSeed(Vector3I coord)
    {
        // Combine world seed with chunk coordinates for deterministic generation
        unchecked
        {
            ulong hash = WorldSeed;
            hash ^= (ulong)(coord.X * 73856093) ^ ((ulong)coord.Y * 19349663) ^ ((ulong)coord.Z * 83492791);
            hash ^= hash >> 33;
            hash *= 0xff51afd7ed558ccdUL;
            hash ^= hash >> 33;
            return hash;
        }
    }

    private ulong GenerateAsteroidId(Vector3I chunk, Vector3 localPos)
    {
        // Create unique ID for persistence tracking
        unchecked
        {
            ulong id = GenerateChunkSeed(chunk);
            id ^= (ulong)(localPos.X * 12345.67f).GetHashCode();
            id ^= (ulong)(localPos.Y * 98765.43f).GetHashCode() << 16;
            id ^= (ulong)(localPos.Z * 54321.09f).GetHashCode() << 32;
            return id;
        }
    }

    /// <summary>
    /// Call when asteroid is destroyed to persist the destruction
    /// </summary>
    public void MarkAsteroidDestroyed(ulong asteroidId)
    {
        _destroyedAsteroids.Add(asteroidId);
    }

    /// <summary>
    /// Called by bullets when they hit a chunk collision body.
    /// Respects AsteroidsDestroyable and AsteroidHP; triggers destruction when HP reaches 0.
    /// </summary>
    public void HitAsteroid(ulong asteroidId, int meshVariant, int variantIndex, CollisionShape3D hitShape, int damage = 1)
    {
        if (!AsteroidsDestroyable) return;
        if (_destroyedAsteroids.Contains(asteroidId)) return;

        if (!_asteroidHealth.TryGetValue(asteroidId, out int hp))
            hp = AsteroidHP;

        hp -= damage;

        if (hp <= 0)
        {
            _asteroidHealth.Remove(asteroidId);
            DestroyAsteroid(asteroidId, meshVariant, variantIndex, hitShape);
        }
        else
        {
            _asteroidHealth[asteroidId] = hp;
        }
    }

    /// <summary>
    /// Destroys the asteroid identified by the hit CollisionShape3D:
    /// disables collision, hides the MultiMesh instance, and persists destruction.
    /// </summary>
    private void DestroyAsteroid(ulong asteroidId, int meshVariant, int variantIndex, CollisionShape3D hitShape)
    {
        MarkAsteroidDestroyed(asteroidId);

        hitShape.Disabled = true;

        var chunkBody = hitShape.GetParent() as StaticBody3D;
        foreach (var kvp in _loadedChunks)
        {
            if (kvp.Value.CollisionBody != chunkBody) continue;

            if (kvp.Value.MmiByVariant.TryGetValue(meshVariant, out var mmi))
            {
                var t = mmi.Multimesh.GetInstanceTransform(variantIndex);
                t.Basis = Basis.Identity.Scaled(Vector3.Zero);
                mmi.Multimesh.SetInstanceTransform(variantIndex, t);
            }
            break;
        }
    }


    private bool IsWithinCollisionRadius(Vector3I coord)
    {
        Vector3I diff = coord - _lastPlayerChunk;
        return diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z
               <= CollisionRadius * CollisionRadius;
    }

    /// <summary>
    /// Builds a StaticBody3D with one CollisionShape3D per live asteroid and
    /// adds it to the scene tree. Does nothing if the chunk already has a collision body.
    /// </summary>
    private void CreateCollisionBody(ChunkData chunk)
    {
        if (_collisionShapes == null || _collisionShapes.Length == 0) return;
        if (chunk.CollisionBody != null) return;

        var collisionBody = new AsteroidBody { Field = this };
        collisionBody.AddToGroup("asteroids");

        var variantCounts = new int[_asteroidMeshes.Length];
        foreach (var a in chunk.Asteroids)
        {
            // Always advance the counter so variantIndex stays in sync with the MultiMesh
            int variantIndex = (a.MeshVariant >= 0 && a.MeshVariant < variantCounts.Length)
                ? variantCounts[a.MeshVariant]++
                : 0;

            // No collision shape needed for already-destroyed asteroids
            if (_destroyedAsteroids.Contains(a.Id))
                continue;

            Shape3D shape = null;
            if (a.MeshVariant >= 0 && a.MeshVariant < _collisionShapes.Length)
                shape = _collisionShapes[a.MeshVariant];
            if (shape == null && _collisionShapes.Length > 0)
                shape = _collisionShapes[0];
            if (shape == null)
                continue;

            var basis = Basis.Identity;
            basis = basis.Rotated(Vector3.Right, a.Rotation.X);
            basis = basis.Rotated(Vector3.Up, a.Rotation.Y);
            basis = basis.Rotated(Vector3.Forward, a.Rotation.Z);

            var collisionShape = new CollisionShape3D();
            collisionShape.Shape = shape;
            collisionShape.Position = a.Position;
            collisionShape.Basis = basis;
            collisionShape.Scale = new Vector3(a.Scale, a.Scale, a.Scale);
            collisionShape.SetMeta("asteroid_id", (long)a.Id);
            collisionShape.SetMeta("mesh_variant", a.MeshVariant);
            collisionShape.SetMeta("variant_index", variantIndex);

            collisionBody.AddChild(collisionShape);
        }

        // Add after all shapes - single physics-server notification
        AddChild(collisionBody);
        chunk.CollisionBody = collisionBody;
    }

    /// <summary>
    /// Adds or removes collision bodies on loaded chunks based on CollisionRadius.
    /// Called whenever the player crosses a chunk boundary.
    /// </summary>
    private void UpdateCollisionBodies()
    {
        foreach (var kvp in _loadedChunks)
        {
            var chunk = kvp.Value;
            bool within = IsWithinCollisionRadius(kvp.Key);

            if (within && chunk.CollisionBody == null)
                CreateCollisionBody(chunk);
            else if (!within && chunk.CollisionBody != null)
            {
                chunk.CollisionBody.QueueFree();
                chunk.CollisionBody = null;
            }
        }
    }

    // Gradual chunk loading

    /// <summary>
    /// Dequeues and loads up to ChunksPerFrame chunks per game frame.
    /// </summary>
    private void ProcessChunkQueue()
    {
        for (int i = 0; i < ChunksPerFrame && _loadQueue.Count > 0; i++)
        {
            var coord = _loadQueue.Dequeue();
            if (!_loadedChunks.ContainsKey(coord))
                LoadChunk(coord);
        }
    }
}
