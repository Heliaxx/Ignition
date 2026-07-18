using Godot;

// Faithful C# port of the flight-model prototype's thrust.gd (Missile_thruster).
// A single RCS thruster visual: shows/hides a quad and plays a looping thrust
// sound when firing. Driven by the missile guidance script via How()/Shutdown().
public partial class MissileThruster : Node3D
{
	[Export] public bool ConstantThrust = false;
	[Export] public float Volume = 0.0f;

	private int _timeSinceLast = 0;
	private bool _off = false;
	private AudioStreamPlayer3D _audio;

	public override void _Ready()
	{
		_audio = GetNode<AudioStreamPlayer3D>("AudioStreamPlayer3D");
		_audio.VolumeDb = Volume;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (ConstantThrust && !_off)
		{
			PlayOrResume();
			return;
		}

		_timeSinceLast++;
		if (!Visible && _timeSinceLast > 10)
			_audio.Stop();
	}

	// Godot's GDExtension/animation hook in the original returned an Error; kept as
	// a no-op so the missile's start-up loop can call it uniformly.
	public Error AnimationStart() => Error.Ok;

	// Fire this thruster: reveal the quad and (occasionally) restart the sound.
	public void How()
	{
		_off = false;
		if (_timeSinceLast > GD.RandRange(1, 30) && !Visible)
		{
			PlayOrResume();
			_timeSinceLast = 0;
		}
		Show();
	}

	public void Shutdown()
	{
		_audio.Stop();
		_off = true;
	}

	private void PlayOrResume()
	{
		if (_audio.Playing)
			return;
		_audio.Play();
	}
}
