using Godot;
using System;

public partial class MusicManager : Node
{
	private AudioStreamPlayer _player;

	private float generalVolume = 1.0f;
	private float musicVolume = 1.0f;
	private float sfxVolume = 1.0f;

	public override void _Ready()
	{
		_player = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
		_player.Bus = "Music";
		_player.Autoplay = false;
		_player.StreamPaused = false;

		_ApplyAudioSettings();
	}

	public void PlayMusic(AudioStream stream, bool loop = true)
	{
		_player.Stream = stream;
		_player.StreamPaused = false;
		_player.Play();

		if (stream is AudioStreamOggVorbis ogg)
			ogg.Loop = loop;
		else if (stream is AudioStreamMP3 mp3)
			mp3.Loop = loop;
	}

	public void StopMusic()
	{
		_player.Stop();
	}

	public bool IsPlaying() => _player.Playing;

	private void _ApplyAudioSettings()
	{
		ConfigFileHandler.Instance.LoadAudioSettings().TryGetValue("general_volume", out Variant generalVol);
		ConfigFileHandler.Instance.LoadAudioSettings().TryGetValue("music_volume", out Variant musicVol);
		ConfigFileHandler.Instance.LoadAudioSettings().TryGetValue("sfx_volume", out Variant sfxVol);

		generalVolume = (float)(double)generalVol;
		musicVolume = (float)(double)musicVol;
		sfxVolume = (float)(double)sfxVol;

		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), LinearToDb(generalVolume));
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), LinearToDb(musicVolume));
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"), LinearToDb(sfxVolume));
	}

	private float LinearToDb(float linear)
	{
		if (linear <= 0.0001f)
			return -80f;
		return 20f * (float)Math.Log10(linear);
	}
}
