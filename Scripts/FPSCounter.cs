using Godot;
using System;

public partial class FPSCounter : Label
{
    public override void _Ready()
    {
        var cfh = GetTree().Root.GetNodeOrNull<ConfigFileHandler>("/root/ConfigFileHandler");
        Visible = cfh?.GetShowFpsMeter() ?? true;
    }

    public override void _Process(double delta)
    {
        double fps = Engine.GetFramesPerSecond();
        Text = $"FPS: {fps}";
    }
}