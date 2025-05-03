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
			// Keybindings
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

			// Video
			config.SetValue("video", "mode", "fullscreen");
			config.SetValue("video", "vsync", false);
			config.SetValue("video", "fxaa", true);
			config.SetValue("video", "taa", false);
			config.SetValue("video", "msaa", "4x");
			config.SetValue("video", "resolution", "1920x1080");

			// Audio
			config.SetValue("audio", "general_volume", 1.0);
			config.SetValue("audio", "music_volume", 1.0);
			config.SetValue("audio", "sfx_volume", 1.0);

			config.Save(SETTINGS_FILE_PATH);
		}
		else
		{
			config.Load(SETTINGS_FILE_PATH);
		}

        Instance = this;
	}

	public void SaveVideoSettings(string key, Variant value)
	{
		config.SetValue("video", key, value);
		config.Save(SETTINGS_FILE_PATH);
	}

	public Dictionary<string, Variant> LoadVideoSettings()
	{
		Dictionary<string, Variant> videoSettings = new();

		foreach (string key in config.GetSectionKeys("video"))
			videoSettings[key] = config.GetValue("video", key);

		return videoSettings;
	}

	public void SaveAudioSettings(string key, Variant value)
	{
		config.SetValue("audio", key, value);
		config.Save(SETTINGS_FILE_PATH);
	}

	public Dictionary<string, Variant> LoadAudioSettings()
	{
		Dictionary<string, Variant> audioSettings = new();

		foreach (string key in config.GetSectionKeys("audio"))
			audioSettings[key] = config.GetValue("audio", key);

		return audioSettings;
	}

	public void SaveKeybindings(StringName action, InputEvent inputEvent)
	{
		string eventStr = "";

		if (inputEvent is InputEventKey keyEvent)
			eventStr = OS.GetKeycodeString(keyEvent.PhysicalKeycode);
		else if (inputEvent is InputEventMouseButton mouseEvent)
			eventStr = $"mouse_{mouseEvent.ButtonIndex}";

		config.SetValue("keybinding", action, eventStr);
		config.Save(SETTINGS_FILE_PATH);
	}

	public Dictionary<string, InputEvent> LoadKeybindings()
	{
		Dictionary<string, InputEvent> keybindings = new();
		var keys = config.GetSectionKeys("keybinding");

		foreach (string key in keys)
		{
			string eventStr = config.GetValue("keybinding", key).AsString();
			InputEvent inputEvent;

			if (eventStr.Contains("mouse_"))
			{
				inputEvent = new InputEventMouseButton
				{
					ButtonIndex = (MouseButton)int.Parse(eventStr.Split('_')[1])
				};
			}
			else
			{
				inputEvent = new InputEventKey
				{
					Keycode = OS.FindKeycodeFromString(eventStr)
				};
			}

			keybindings[key] = inputEvent;
		}

		return keybindings;
	}
}