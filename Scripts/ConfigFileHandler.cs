using Godot;
using System;
using System.Collections.Generic;

public partial class ConfigFileHandler : Node
{
	private ConfigFile config = new ConfigFile();
	private const string SETTINGS_FILE_PATH = "user://settings.ini";

	public static ConfigFileHandler Instance;

	public override void _Ready()
	{
		if (!FileAccess.FileExists(SETTINGS_FILE_PATH))
		{
			SetDefaultKeybindings();
			SetDefaultVideoSettings();
			SetDefaultAudioSettings();
			SetDefaultControlSettings();
			config.Save(SETTINGS_FILE_PATH);
		}
		else
		{
			config.Load(SETTINGS_FILE_PATH);
		}

		EnsureControlDefaults();
		Instance = this;
		ApplyVideoSettings();
	}

	private void SetDefaultKeybindings()
	{
		config.SetValue("keybinding", "thrust_forward", "W");
		config.SetValue("keybinding", "thrust_backward", "S");
		config.SetValue("keybinding", "roll_left", "Q");
		config.SetValue("keybinding", "roll_right", "E");
		config.SetValue("keybinding", "strafe_up", "space");
		config.SetValue("keybinding", "strafe_down", "alt");
		config.SetValue("keybinding", "strafe_left", "A");
		config.SetValue("keybinding", "strafe_right", "D");
		config.SetValue("keybinding", "boost", "tab");
		config.SetValue("keybinding", "primary_fire", "mouse_1");
		config.SetValue("keybinding", "secondary_fire", "mouse_2");
		config.SetValue("keybinding", "light", "W");
	}

	private void SetDefaultVideoSettings()
	{
		config.SetValue("video", "mode", "fullscreen");
		config.SetValue("video", "vsync", false);
		config.SetValue("video", "fxaa", true);
		config.SetValue("video", "taa", false);
		config.SetValue("video", "msaa", "4x");
		config.SetValue("video", "resolution", "1920x1080");
	}

	private void SetDefaultAudioSettings()
	{
		config.SetValue("audio", "general_volume", 1.0);
		config.SetValue("audio", "music_volume", 1.0);
		config.SetValue("audio", "sfx_volume", 1.0);
	}

	private void SetDefaultControlSettings()
	{
		config.SetValue("controls", "relative_mouse", true);
		config.SetValue("controls", "aim_sensitivity", 1.2f);
		config.SetValue("controls", "aim_deadzone", 0.05f);
		config.SetValue("controls", "auto_center_speed", 8.0f);
	}

	private void EnsureControlDefaults()
	{
		bool changed = false;

		if (!config.HasSectionKey("controls", "relative_mouse"))
		{
			config.SetValue("controls", "relative_mouse", true);
			changed = true;
		}
		if (!config.HasSectionKey("controls", "aim_sensitivity"))
		{
			config.SetValue("controls", "aim_sensitivity", 1.2f);
			changed = true;
		}
		if (!config.HasSectionKey("controls", "aim_deadzone"))
		{
			config.SetValue("controls", "aim_deadzone", 0.05f);
			changed = true;
		}
		if (!config.HasSectionKey("controls", "auto_center_speed"))
		{
			config.SetValue("controls", "auto_center_speed", 8.0f);
			changed = true;
		}

		if (changed)
			config.Save(SETTINGS_FILE_PATH);
	}

	/// <summary>
	/// Helper: load all keys from a config section into a dictionary.
	/// </summary>
	private Dictionary<string, Variant> LoadSection(string section)
	{
		Dictionary<string, Variant> result = new();
		if (!config.HasSection(section))
			return result;

		foreach (string key in config.GetSectionKeys(section))
			result[key] = config.GetValue(section, key);

		return result;
	}

	/// <summary>
	/// Helper: save a single key in a section and persist to disk.
	/// </summary>
	private void SaveKey(string section, string key, Variant value)
	{
		config.SetValue(section, key, value);
		config.Save(SETTINGS_FILE_PATH);
	}
}
