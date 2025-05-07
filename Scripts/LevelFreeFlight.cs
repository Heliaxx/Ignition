using Godot;
using System;

public partial class LevelFreeFlight : Node3D
{
	private Node3D player;

	private MusicManager musicManager;
	private NavigationRegion3D navigationRegion;
	private PackedScene enemyShip = (PackedScene)GD.Load("res://Scenes/Fighter.tscn"); // Preload enemy ship scene
	private Node3D instance;

	public override void _Ready()
	{
		player = GetNode<Node3D>("NavigationRegion3D/Player");
		navigationRegion = GetNode<NavigationRegion3D>("NavigationRegion3D");
		musicManager = GetNode<MusicManager>("/root/MusicManager");
		musicManager.StopMusic();
	}

	public override void _PhysicsProcess(double delta) {}
}
