using Godot;
using System;

public partial class Level : Node3D
{
	private Node3D player;
	//private Node3D spawns;
	private NavigationRegion3D navigationRegion;
	private PackedScene enemyShip = (PackedScene)GD.Load("res://Scenes/corvette.tscn"); // Preload enemy ship scene
	private Node3D instance;

	public override void _Ready()
	{
		// Get references to nodes
		player = GetNode<Node3D>("NavigationRegion3D/Kaito MCS-71");
		//spawns = GetNode<Node3D>("Spawns");
		navigationRegion = GetNode<NavigationRegion3D>("NavigationRegion3D");
	}

	public override void _PhysicsProcess(double delta)
	{
		// Optional: Uncomment if needed
		// GetTree().CallGroup("enemies", "update_target_location", player.GlobalTransform.origin);
	}

	//private async void OnCorvetteEnemyDead() // Function for spawning new enemies
	//{
		//await ToSignal(GetTree().CreateTimer(1), SceneTreeTimer.SignalName.Timeout); // Wait 1 second before spawning
		//Vector3 spawnPoint = spawns.GetChild<Node3D>(0).GlobalPosition; // Get spawn point position
		//instance = (Node3D)enemyShip.Instantiate(); // Create instance of enemy ship
		//instance.Position = spawnPoint; // Set spawn position
		//navigationRegion.AddChild(instance); // Add enemy to the scene
	//}
}
