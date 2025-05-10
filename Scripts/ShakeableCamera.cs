using Godot;

public partial class ShakeableCamera : Area3D
{
	[Export] public float TraumaReductionRate { get; set; } = 1.0f;
	[Export] public float MaxX { get; set; } = 0.0f;
	[Export] public float MaxY { get; set; } = 0.0f;
	[Export] public float MaxZ { get; set; } = 0.0f;
	[Export] public FastNoiseLite Noise { get; set; }
	[Export] public float NoiseSpeed { get; set; } = 50.0f;

	private float trauma = 0.0f;
	private float time = 0.0f;

	private Camera3D camera;
	private Vector3 initialRotation;

	public override void _Ready()
	{
		camera = FindChild("Camera3D") as Camera3D;
		initialRotation = camera.RotationDegrees;
		Noise = new FastNoiseLite();
	}

	public override void _Process(double delta)
	{
		time += (float)delta;
		trauma = Mathf.Max(trauma - (float)delta * TraumaReductionRate, 0.0f);

		float shakeIntensity = GetShakeIntensity();
		camera.Position = new Vector3(camera.Position.X, camera.Position.Y, initialRotation.X + 0.05f * shakeIntensity * GetNoiseFromSeed(0));
		Vector3 newRotation = new Vector3(
		initialRotation.X + MaxX * shakeIntensity * GetNoiseFromSeed(0),
		initialRotation.Y + MaxY * shakeIntensity * GetNoiseFromSeed(1),
		initialRotation.Z + MaxZ * shakeIntensity * GetNoiseFromSeed(2)
	);

	camera.RotationDegrees = newRotation;
	camera.ForceUpdateTransform();

	}

	public void AddTrauma(float traumaAmount)
	{
		trauma = Mathf.Clamp(trauma + traumaAmount, 0.0f, 1.0f);
	}

	private float GetShakeIntensity()
	{
		return trauma * trauma;
	}

	private float GetNoiseFromSeed(int seed)
	{
		Noise.Seed = seed;
		return Noise.GetNoise1D(time * NoiseSpeed);
	}
}
