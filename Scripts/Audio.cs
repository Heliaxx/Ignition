using Godot;
using System;
using System.Collections.Generic;

public partial class Audio : Control
{
    private Slider generalVolumeSlider;
    private Slider musicVolumeSlider;
    private Slider sfxVolumeSlider;

    public override void _Ready()
{
    generalVolumeSlider = GetNode<Slider>("Menu/Options/VolGeneralSlider");
    musicVolumeSlider = GetNode<Slider>("Menu/Options/VolMusicSlider");
    sfxVolumeSlider = GetNode<Slider>("Menu/Options/VolSFXSlider");

    Dictionary<string, Variant> audioSettings = ConfigFileHandler.Instance.LoadAudioSettings();

    if (audioSettings.TryGetValue("general_volume", out Variant generalVol))
        generalVolumeSlider.Value = Math.Min(generalVol.As<float>(), 1.0f) * 100.0f;
    if (audioSettings.TryGetValue("music_volume", out Variant musicVol))
        musicVolumeSlider.Value = Math.Min(musicVol.As<float>(), 1.0f) * 100.0f;
    if (audioSettings.TryGetValue("sfx_volume", out Variant sfxVol))
        sfxVolumeSlider.Value = Math.Min(sfxVol.As<float>(), 1.0f) * 100.0f;
    _ApplyAudioSettings();
}

    private void _ApplyAudioSettings()
    {
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), LinearToDb((float)generalVolumeSlider.Value / 100f));
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), LinearToDb((float)musicVolumeSlider.Value / 100f));
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"), LinearToDb((float)sfxVolumeSlider.Value / 100f));
    }

    private float LinearToDb(float linear)
    {
        if (linear <= 0.0001f)
            return -80f;
        return 20f * (float)Math.Log10(linear);
    }

    private void _on_back_btn_pressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Menu.tscn");
    }

    private void _on_reset_btn_pressed()
    {
        generalVolumeSlider.Value = 100f;
        musicVolumeSlider.Value = 100f;
        sfxVolumeSlider.Value = 100f;

        ConfigFileHandler.Instance.SaveAudioSettings("general_volume", 1.0f);
        ConfigFileHandler.Instance.SaveAudioSettings("music_volume", 1.0f);
        ConfigFileHandler.Instance.SaveAudioSettings("sfx_volume", 1.0f);

        _ApplyAudioSettings();
    }

    private void _on_vol_general_slider_drag_ended(bool valueChanged)
    {
        if (valueChanged)
        {
            float volume = (float)generalVolumeSlider.Value / 100f;
            ConfigFileHandler.Instance.SaveAudioSettings("general_volume", volume);
            AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), LinearToDb(volume));
        }
    }

    private void _on_vol_music_slider_drag_ended(bool valueChanged)
    {
        if (valueChanged)
        {
            float volume = (float)musicVolumeSlider.Value / 100f;
            ConfigFileHandler.Instance.SaveAudioSettings("music_volume", volume);
            AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), LinearToDb(volume));
        }
    }

    private void _on_vol_sfx_slider_drag_ended(bool valueChanged)
    {
        if (valueChanged)
        {
            float volume = (float)sfxVolumeSlider.Value / 100f;
            ConfigFileHandler.Instance.SaveAudioSettings("sfx_volume", volume);
            AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"), LinearToDb(volume));
        }
    }
}