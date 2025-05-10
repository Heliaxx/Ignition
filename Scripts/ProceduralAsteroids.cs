using Godot;
using System;
using System.Collections.Generic;

public partial class ProceduralAsteroids : Node3D
{
	[Export]
	public int AsteroidCount = 100;

	[Export]
	public float AreaWidth = 2000f;

	[Export]
	public float AreaHeight = 300f;

	[Export]
	public float AreaDepth = 2000f;

	[Export]
	public float MinScale = 10.0f;

	[Export]
	public float MaxScale = 30.0f;
	
	[Export]
	public bool UseEllipsoidArea = true;

	public PackedScene prefab = (PackedScene)GD.Load("res://Scenes/asteroid_2.tscn");

	private RandomNumberGenerator rng = new RandomNumberGenerator();

	public override void _Ready()
	{
		rng.Randomize();

		for (int i = 0; i < AsteroidCount; i++)
		{
			var asteroidInstance = (Node3D)prefab.Instantiate();
			Vector3 spawnOffset;
			
			if (UseEllipsoidArea)
			{
				Vector3 randomDir;
				do
				{
					randomDir = new Vector3(
						(float)(GD.Randf() * 2.0 - 1.0),
						(float)(GD.Randf() * 2.0 - 1.0),
						(float)(GD.Randf() * 2.0 - 1.0)
					);
				} while (randomDir.LengthSquared() > 1.0f);
				
				spawnOffset = new Vector3(
					randomDir.X * AreaWidth * 0.5f,
					randomDir.Y * AreaHeight * 0.5f,
					randomDir.Z * AreaDepth * 0.5f
				);
			}
			else
			{
				spawnOffset = new Vector3(
				rng.RandfRange(-AreaWidth * 0.5f, AreaWidth * 0.5f),
				rng.RandfRange(-AreaHeight * 0.5f, AreaHeight * 0.5f),
				rng.RandfRange(-AreaDepth * 0.5f, AreaDepth * 0.5f)
				);
			}
			asteroidInstance.Position = spawnOffset;

			float randomScale = rng.RandfRange(MinScale, MaxScale);
			asteroidInstance.Scale = new Vector3(randomScale, randomScale, randomScale);

			Vector3 rotation = new Vector3(
				rng.RandfRange(0f, Mathf.Tau),
				rng.RandfRange(0f, Mathf.Tau),
				rng.RandfRange(0f, Mathf.Tau)
			);
			asteroidInstance.Rotation = rotation;

			AddChild(asteroidInstance);
		}
	}
}
