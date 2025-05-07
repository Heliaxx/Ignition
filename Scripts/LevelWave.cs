// using Godot;
// using System;
// using System.Threading.Tasks;

// public partial class LevelWave : Node3D
// {
//     private Node3D player;
//     private Node3D spawns;

//     private MusicManager musicManager;
//     private NavigationRegion3D navigationRegion;
// 	private BoundaryArea boundary;
//     private PackedScene enemyShip = (PackedScene)GD.Load("res://Scenes/fighter.tscn"); // Preload enemy ship scene
//     private Fighter instance;

// 	public override void _Ready()
// {
// 	player = GetNode<Node3D>("NavigationRegion3D/Player");
// 	spawns = GetNode<Node3D>("SpawnLocations");
// 	navigationRegion = GetNode<NavigationRegion3D>("NavigationRegion3D");
// 	// boundary = GetNode<BoundaryArea>("BoundaryArea");
// 	musicManager = GetNode<MusicManager>("/root/MusicManager");
// 	musicManager.StopMusic();
// 	// SpawnEnemy();
// }


//     public override void _PhysicsProcess(double delta)
//     {
//     }

//     private void SpawnEnemy()
//     {
//         // Get spawn point position (first child of spawns)
//         Vector3 spawnPoint = spawns.GetChild<Node3D>(0).GlobalPosition;
//         instance = (Fighter)enemyShip.Instantiate();
//         instance.Position = spawnPoint;
//         navigationRegion.AddChild(instance);
//     }
// }




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

        // Connect the enemy's death signal to spawn a new one
        enemy.Died += SpawnEnemy;
    }

    public override void _PhysicsProcess(double delta) { }
}