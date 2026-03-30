using Godot;
using System;
using System.Collections.Generic;

public partial class LevelWave : BaseLevel
{
	private const int POOL_SIZE = 10;
	private const float SPAWN_MIN_DISTANCE = 150f;
	private const float SPAWN_MAX_DISTANCE = 300f;

	private EnemySpawner _spawner;
	private int _killCount = 0;
	private int _highKills = 0;
	private Label _killLabel;
	private Label _highKillLabel;

	public override void _Ready()
	{
		base._Ready();

		_killLabel = GetNodeOrNull<Label>("ScoreHUD/KillLabel");
		_highKillLabel = GetNodeOrNull<Label>("ScoreHUD/HighKillLabel");
		_highKills = (int)(ConfigFileHandler.Instance?.LoadWaveHighScore() ?? 0);
		UpdateKillHUD();

		WirePlayerDeath(OnPlayerDied);

		_spawner = new EnemySpawner
		{
			EnemyScene = GD.Load<PackedScene>("res://Scenes/fighter.tscn"),
			PortalScene = GD.Load<PackedScene>("res://Scenes/Portal.tscn"),
			PoolSize = POOL_SIZE
		};
		_spawner.Initialize(NavigationRegion);
		_spawner.EnemyDied += OnEnemyDied;

		SpawnEnemy();
	}

	private Vector3 GetRandomDirectionInSphere()
	{
		Vector3 randomDir = new Vector3(
			(float)GD.Randf() * 2f - 1f,
			(float)GD.Randf() * 2f - 1f,
			(float)GD.Randf() * 2f - 1f
		);
		return randomDir.Normalized();
	}

	private void SpawnEnemy()
	{
		Vector3 spawnDir = GetRandomDirectionInSphere();
		float distance = (float)GD.RandRange(SPAWN_MIN_DISTANCE, SPAWN_MAX_DISTANCE);
		Vector3 spawnPos = Player.GlobalPosition + spawnDir * distance;
		_spawner.SpawnViaPortal(spawnPos, Player);
	}

	private void OnEnemyDied(Fighter enemy)
	{
		_spawner.Recycle(enemy);

		_killCount++;
		if (_killCount > _highKills)
		{
			_highKills = _killCount;
			ConfigFileHandler.Instance?.SaveWaveHighScore(_highKills);
		}
		UpdateKillHUD();

		SpawnEnemy();
	}

	private void UpdateKillHUD()
	{
		if (_killLabel != null)
			_killLabel.Text = $"Kills: {_killCount}";
		if (_highKillLabel != null)
			_highKillLabel.Text = $"High Score: {_highKills}";
	}

	public int GetKillCount() => _killCount;

	private void OnPlayerDied()
	{
		var deathLabel = Player.GetNodeOrNull<Label>("DeathScreenLayer/DeathScreen/VBoxContainer/DeathLabel");
		if (deathLabel != null)
			deathLabel.Text = $"YOU DIED\nKills: {_killCount}";
	}

	public override void _PhysicsProcess(double delta) { }
}
