using Godot;

public partial class BaseLevel : Node3D
{
	protected Node3D Player;
	protected Kaito PlayerKaito;
	protected MusicManager MusicManager;
	protected Node3D NavigationRegion;

	// Floating-point origin rebasing: when the player drifts this far from world
	// origin, every scene object is shifted back so the player returns to (0,0,0).
	// ChunkedAsteroidField IS included in the shift — WorldToChunk uses
	// (worldPos - GlobalPosition) so relative positions stay correct regardless of
	// where the field node sits in world space.
	[Export] public float OriginShiftThreshold = 5000f;

	private float _originShiftThresholdSq;

	public override void _Ready()
	{
		EventBus.ClearAll();
		Player = GetNode<Node3D>("Player");
		PlayerKaito = Player as Kaito;
		NavigationRegion = this;
		MusicManager = GetNode<MusicManager>("/root/MusicManager");
		MusicManager.StopMusic();
		SyncDirectionalLight();
		_originShiftThresholdSq = OriginShiftThreshold * OriginShiftThreshold;
	}

	public override void _Process(double delta)
	{
		if (Player != null && Player.GlobalPosition.LengthSquared() > _originShiftThresholdSq)
			ShiftWorldOrigin();
	}

	private void ShiftWorldOrigin()
	{
		Vector3 offset = Player.GlobalPosition;

		foreach (Node child in GetChildren())
		{
			if (child is Node3D node && child != Player)
				node.GlobalPosition -= offset;
		}

		Player.GlobalPosition = Vector3.Zero;
	}

	protected void SyncDirectionalLight()
	{
		var worldEnv = GetNode<WorldEnvironment>("WorldEnvironment");
		var light = GetNode<DirectionalLight3D>("DirectionalLight3D");

		if (worldEnv?.Environment?.Sky?.SkyMaterial is ShaderMaterial skyMat)
		{
			var sunPos = (Vector3)skyMat.GetShaderParameter("star_pos");
			var sunDir = sunPos.Normalized();
			light.LookAtFromPosition(Vector3.Zero, -sunDir, Vector3.Up);
		}
	}

	protected void WirePlayerDeath(System.Action onDied)
	{
		if (PlayerKaito != null)
		{
			var health = PlayerKaito.GetNode<HealthComponent>("HealthComponent");
			health.Died += () => onDied();
		}
	}
}
