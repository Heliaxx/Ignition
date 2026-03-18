using Godot;
using System;
using System.Collections.Generic;

public class EnemySpawner
{
	public PackedScene EnemyScene { get; set; }
	public int PoolSize { get; set; } = 10;

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

	public void Recycle(Fighter enemy)
	{
		Deactivate(enemy);
		_pool.Enqueue(enemy);
	}

	private Fighter GetOrCreate()
	{
		if (_pool.Count > 0)
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
		enemy.ProcessMode = Node.ProcessModeEnum.Inherit;
		enemy.SetPhysicsProcess(true);
		enemy.SetProcess(true);
	}

	private static void Deactivate(Fighter enemy)
	{
		enemy.Visible = false;
		enemy.SetPhysicsProcess(false);
		enemy.SetProcess(false);
		enemy.ProcessMode = Node.ProcessModeEnum.Disabled;
	}
}
