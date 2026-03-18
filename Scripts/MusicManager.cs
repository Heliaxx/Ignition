using Godot;
using System;

public partial class MusicManager : Node
{
    private AudioStreamPlayer _player;

    public override void _Ready()
    {
        _player = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
        _player.Bus = "Music";
        _player.Autoplay = false;
        _player.StreamPaused = false;

        ApplyAudioSettings();
        EventBus.AudioSettingsChanged += ApplyAudioSettings;
    }

    public override void _ExitTree()
    {
        EventBus.AudioSettingsChanged -= ApplyAudioSettings;
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

    private void ApplyAudioSettings()
    {
        var settings = ConfigFileHandler.Instance?.LoadAudioSettings();
        if (settings == null) return;

        float generalVolume = settings.TryGetValue("general_volume", out var gv) ? gv.AsSingle() : 1.0f;
        float musicVolume = settings.TryGetValue("music_volume", out var mv) ? mv.AsSingle() : 1.0f;
        float sfxVolume = settings.TryGetValue("sfx_volume", out var sv) ? sv.AsSingle() : 1.0f;

        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), LinearToDb(generalVolume));
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), LinearToDb(musicVolume));
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"), LinearToDb(sfxVolume));
    }

    private static float LinearToDb(float linear)
    {
        if (linear <= 0.0001f)
            return -80f;
        return 20f * (float)Math.Log10(linear);
    }
}
