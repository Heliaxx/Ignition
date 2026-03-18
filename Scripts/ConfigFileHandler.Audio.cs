using Godot;
using System.Collections.Generic;

public partial class ConfigFileHandler
{
	public void SaveAudioSettings(string key, Variant value)
	{
		SaveKey("audio", key, value);
	}

	public Dictionary<string, Variant> LoadAudioSettings()
	{
		return LoadSection("audio");
	}
}
