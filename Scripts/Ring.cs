using Godot;

public partial class Ring : Area3D
{
	[Signal]
	public delegate void CollectedEventHandler(Node3D body);

	private StaticBody3D _model;

	public override void _Ready()
	{
		_model = GetNodeOrNull<StaticBody3D>("Model");

		BodyEntered += (body) =>
		{
			if (body is not CharacterBody3D) return;
			EmitSignal(SignalName.Collected, body);
			Deactivate();
		};
	}

	private void Deactivate()
	{
		Hide();
		SetDeferred(PropertyName.Monitoring, false);
		if (_model != null)
			_model.SetDeferred(StaticBody3D.PropertyName.CollisionLayer, 0u);
	}

	public void Activate()
	{
		if (_model != null)
			_model.CollisionLayer = 1;
		Show();
		Monitoring = true;
	}
}
