using Godot;
using System;
using System.Collections.Generic;

public class EnemySpawner
{
	public PackedScene EnemyScene { get; set; }
	public PackedScene PortalScene { get; set; }
	public int PoolSize { get; set; } = 10;

	public static bool UsePooling = true;

	public event Action<Fighter> EnemyDied;

	private readonly Queue<Fighter> _pool = new();
	private Node3D _spawnParent;
	private readonly RandomNumberGenerator _rng = new();

	public void Initialize(Node3D spawnParent)
	{
		_spawnParent = spawnParent;
		_rng.Randomize();
		for (int i = 0; i < PoolSize; i++)
		{
			var enemy = CreateEnemy();
			Deactivate(enemy);
			_pool.Enqueue(enemy);
		}
	}

	public Fighter SpawnAt(Vector3 position)
	{
		var enemy = GetOrCreate();
		enemy.GlobalPosition = position;
		Activate(enemy);
		return enemy;
	}

	public Fighter SpawnNear(Node3D target, float minDist, float maxDist)
	{
		Vector3 dir = new Vector3(
			_rng.RandfRange(-1f, 1f),
			_rng.RandfRange(-1f, 1f),
			_rng.RandfRange(-1f, 1f)
		).Normalized();
		float dist = _rng.RandfRange(minDist, maxDist);
		return SpawnAt(target.GlobalPosition + dir * dist);
	}

	/// <summary>
	/// Opens a portal at an explicit position facing the given target, then spawns the enemy.
	/// </summary>
	// Builds a transform where +Z faces toward faceToward and +Y stays upright.
	private static Transform3D PortalTransform(Vector3 pos, Node3D faceToward)
	{
		Vector3 z = (faceToward.GlobalPosition - pos).Normalized();
		Vector3 up = Mathf.Abs(z.Dot(Vector3.Up)) < 0.99f ? Vector3.Up : Vector3.Forward;
		Vector3 x = up.Cross(z).Normalized();
		Vector3 y = z.Cross(x).Normalized();
		return new Transform3D(new Basis(x, y, z), pos);
	}

	public void SpawnViaPortal(Vector3 pos, Node3D faceToward)
	{
		if (PortalScene == null) { SpawnAt(pos); return; }

		var portal = PortalScene.Instantiate<Portal>();
		portal.GlobalTransform = PortalTransform(pos, faceToward);
		_spawnParent.AddChild(portal);

		var enemy = GetOrCreate();
		portal.OpenThenClose(() =>
		{
			enemy.GlobalPosition = pos;
			Activate(enemy);
			Vector3 toTarget = (faceToward.GlobalPosition - pos).Normalized();
			Vector3 up = Mathf.Abs(toTarget.Dot(Vector3.Up)) < 0.99f ? Vector3.Up : Vector3.Forward;
			enemy.LookAt(faceToward.GlobalPosition, up);
		});
	}

	/// <summary>
	/// Opens a portal near the target, then spawns the enemy from it once fully open.
	/// Falls back to SpawnNear if no PortalScene is set.
	/// </summary>
	public void SpawnNearViaPortal(Node3D target, float minDist, float maxDist)
	{
		Vector3 dir = new Vector3(
			_rng.RandfRange(-1f, 1f),
			_rng.RandfRange(-1f, 1f),
			_rng.RandfRange(-1f, 1f)
		).Normalized();
		float dist = _rng.RandfRange(minDist, maxDist);
		Vector3 pos = target.GlobalPosition + dir * dist;

		if (PortalScene == null)
		{
			SpawnAt(pos);
			return;
		}

		var portal = PortalScene.Instantiate<Portal>();
		portal.GlobalTransform = PortalTransform(pos, target);
		_spawnParent.AddChild(portal);

		var enemy = GetOrCreate();
		portal.OpenThenClose(() =>
		{
			enemy.GlobalPosition = pos;
			Activate(enemy);
			Vector3 toTarget = (target.GlobalPosition - pos).Normalized();
			Vector3 up = Mathf.Abs(toTarget.Dot(Vector3.Up)) < 0.99f ? Vector3.Up : Vector3.Forward;
			enemy.LookAt(target.GlobalPosition, up);
		});
	}

	public void Recycle(Fighter enemy)
	{
		Deactivate(enemy);
		_pool.Enqueue(enemy);
	}

	private Fighter GetOrCreate()
	{
		if (UsePooling && _pool.Count > 0)
			return _pool.Dequeue();
		return CreateEnemy();
	}

	private Fighter CreateEnemy()
	{
		var enemy = EnemyScene.Instantiate<Fighter>();
		_spawnParent.AddChild(enemy);
		Fighter captured = enemy;
		enemy.Died += () => EnemyDied?.Invoke(captured);
		return enemy;
	}

	private static void Activate(Fighter enemy)
	{
		enemy.Reset();
		enemy.Visible = true;
		enemy.CollisionLayer = 1;
		enemy.CollisionMask = 1;
		enemy.ProcessMode = Node.ProcessModeEnum.Inherit;
		enemy.SetPhysicsProcess(true);
		enemy.SetProcess(true);
	}

	private static void Deactivate(Fighter enemy)
	{
		enemy.Visible = false;
		enemy.CollisionLayer = 0;
		enemy.CollisionMask = 0;
		enemy.SetPhysicsProcess(false);
		enemy.SetProcess(false);
		enemy.ProcessMode = Node.ProcessModeEnum.Disabled;
	}
}
