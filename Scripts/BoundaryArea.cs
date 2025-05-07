using Godot;
using System;

public partial class BoundaryArea : Area3D
{
    [Signal]
    public delegate void ExitedBoundaryEventHandler(Node3D body);

    public override void _Ready()
    {
        BodyExited += OnBodyExited;
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is Fighter)
        {
            EmitSignal(SignalName.ExitedBoundary, body);
        }
    }
}