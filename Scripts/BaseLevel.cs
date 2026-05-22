using Godot;

public partial class BaseLevel : Node3D
{
	protected Node3D Player;
	protected Kaito PlayerKaito;
	protected MusicManager MusicManager;
	protected Node3D NavigationRegion;

	public override void _Ready()
	{
		EventBus.ClearAll();
		Player = GetNode<Node3D>("Player");
		PlayerKaito = Player as Kaito;
		NavigationRegion = this;
		MusicManager = GetNode<MusicManager>("/root/MusicManager");
		MusicManager.StopMusic();
		SyncDirectionalLight();
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
