using Godot;
using System;

public partial class LevelSkirmish : Node3D
{
	private Node3D player;
	private Node3D spawns;
	private MusicManager musicManager;
	private NavigationRegion3D navigationRegion;
	private PackedScene enemyShip = GD.Load<PackedScene>("res://Scenes/fighter.tscn");

	public override void _Ready()
	{
		player = GetNode<Node3D>("NavigationRegion3D/Player");
		spawns = GetNode<Node3D>("SpawnLocations");
		navigationRegion = GetNode<NavigationRegion3D>("NavigationRegion3D");
		musicManager = GetNode<MusicManager>("/root/MusicManager");

		musicManager.StopMusic();
		SpawnEnemies();
	}

	private void SpawnEnemies()
	{
		for (int i = 0; i < spawns.GetChildCount(); i++)
		{
			Node3D spawnPoint = spawns.GetChild<Node3D>(i);
			Node3D enemyInstance = (Node3D)enemyShip.Instantiate();
			enemyInstance.GlobalPosition = spawnPoint.GlobalPosition;
			navigationRegion.AddChild(enemyInstance);
		}
	}
}