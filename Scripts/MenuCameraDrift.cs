using Godot;
public partial class MenuCameraDrift : Node3D
{
	// World units per second along the rig's facing.
	[Export] public float Speed = 40.0f;
	[Export] public float DriftYaw = 0.6f;
	[Export] public float DriftRoll = 1.2f;

	public override void _Process(double delta)
	{
		float d = (float)delta;

		RotateObjectLocal(Vector3.Up, Mathf.DegToRad(DriftYaw) * d);
		RotateObjectLocal(Vector3.Back, Mathf.DegToRad(DriftRoll) * d);

		// Travel along the rig's facing, so the turn above curves the flight path.
		GlobalPosition += -GlobalTransform.Basis.Z * Speed * d;
	}
}
