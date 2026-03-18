using Godot;
using System;

public partial class LevelSkirmish : BaseLevel
{
	private Node3D spawns;
	private PackedScene enemyShip = GD.Load<PackedScene>("res://Scenes/fighter.tscn");

	public override void _Ready()
	{
		base._Ready();
		spawns = GetNode<Node3D>("SpawnLocations");
		SpawnEnemies();
	}

	private void SpawnEnemies()
	{
		for (int i = 0; i < spawns.GetChildCount(); i++)
		{
			Node3D spawnPoint = spawns.GetChild<Node3D>(i);
			Node3D enemyInstance = (Node3D)enemyShip.Instantiate();
			NavigationRegion.AddChild(enemyInstance);
			enemyInstance.GlobalPosition = spawnPoint.GlobalPosition;
		}
	}
}
