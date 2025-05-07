using Godot;
using System;
using System.Threading.Tasks;

public partial class LevelWave : Node3D
{
    private Node3D player;
    private Node3D spawns;

    private MusicManager musicManager;
    private NavigationRegion3D navigationRegion;
	private BoundaryArea boundary;
    private PackedScene enemyShip = (PackedScene)GD.Load("res://Scenes/fighter.tscn"); // Preload enemy ship scene
    private Fighter instance;

    // public override void _Ready()
    // {
    //     // Get references to nodes
    //     player = GetNode<Node3D>("NavigationRegion3D/Player");
    //     spawns = GetNode<Node3D>("SpawnLocations");
    //     navigationRegion = GetNode<NavigationRegion3D>("NavigationRegion3D");
    //     boundary = GetNode<BoundaryArea>("BoundaryArea");
    //     musicManager = GetNode<MusicManager>("/root/MusicManager");
        
    //     musicManager.StopMusic();
    //     // Spawn one enemy ship at the start.
    //     SpawnEnemy();
	// 	// boundary.ExitedBoundary += instance.OnExitedBoundary;
    // }

	public override void _Ready()
{
	player = GetNode<Node3D>("NavigationRegion3D/Player");
	spawns = GetNode<Node3D>("SpawnLocations");
	navigationRegion = GetNode<NavigationRegion3D>("NavigationRegion3D");
	boundary = GetNode<BoundaryArea>("BoundaryArea");
	musicManager = GetNode<MusicManager>("/root/MusicManager");

	musicManager.StopMusic();

	// SpawnEnemy();
}


    public override void _PhysicsProcess(double delta)
    {
    }

    private void SpawnEnemy()
    {
        // Get spawn point position (first child of spawns)
        Vector3 spawnPoint = spawns.GetChild<Node3D>(0).GlobalPosition;
        instance = (Fighter)enemyShip.Instantiate();
        instance.Position = spawnPoint;
        navigationRegion.AddChild(instance);
    }
}