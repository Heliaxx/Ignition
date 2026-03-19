using Godot;
using System.Collections.Generic;

public partial class ConfigFileHandler
{
	private const float DefaultGeneralVolume = 0.4f;
	private const float DefaultMusicVolume   = 0.2f;
	private const float DefaultSfxVolume     = 0.4f;

	public void SaveAudioSettings(string key, Variant value)
	{
		SaveKey("audio", key, value);
	}

	public Dictionary<string, Variant> LoadAudioSettings()
	{
		return LoadSection("audio");
	}

	public void ResetAudioSettings()
	{
		config.SetValue("audio", "general_volume", DefaultGeneralVolume);
		config.SetValue("audio", "music_volume",   DefaultMusicVolume);
		config.SetValue("audio", "sfx_volume",     DefaultSfxVolume);
		config.Save(SETTINGS_FILE_PATH);
	}

	private void EnsureAudioDefaults()
	{
		bool changed = false;
		void Ensure(string key, Variant value)
		{
			if (config.HasSectionKey("audio", key)) return;
			config.SetValue("audio", key, value);
			changed = true;
		}
		Ensure("general_volume", DefaultGeneralVolume);
		Ensure("music_volume",   DefaultMusicVolume);
		Ensure("sfx_volume",     DefaultSfxVolume);
		if (changed) config.Save(SETTINGS_FILE_PATH);
	}
}
