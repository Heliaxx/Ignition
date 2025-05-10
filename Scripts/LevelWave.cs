using Godot;
using System;

public partial class LevelWave : Node3D
{
	private Node3D player;
	private MusicManager musicManager;
	private NavigationRegion3D navigationRegion;
	private PackedScene enemyShip = GD.Load<PackedScene>("res://Scenes/fighter.tscn");
	private RandomNumberGenerator rng = new RandomNumberGenerator();

	public override void _Ready()
	{
		player = GetNode<Node3D>("NavigationRegion3D/Player");
		navigationRegion = GetNode<NavigationRegion3D>("NavigationRegion3D");
		musicManager = GetNode<MusicManager>("/root/MusicManager");
		musicManager.StopMusic();
		rng.Randomize();
		SpawnEnemy();
	}

	private void SpawnEnemy()
	{
		// Spawn near the player
		Vector3 basePosition = player.GlobalPosition;
		Vector3 randomOffset = new Vector3(
			rng.RandfRange(-30, 30),
			rng.RandfRange(-30, 30),
			rng.RandfRange(-30, 30)
		);
		Vector3 spawnPosition = basePosition + randomOffset;

		Fighter enemy = (Fighter)enemyShip.Instantiate();
		enemy.GlobalPosition = spawnPosition;
		navigationRegion.AddChild(enemy);
		enemy.Died += SpawnEnemy;
	}

	public override void _PhysicsProcess(double delta) { }
}