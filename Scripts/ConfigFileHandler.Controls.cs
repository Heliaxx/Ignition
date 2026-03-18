using Godot;
using System.Collections.Generic;

public partial class ConfigFileHandler
{
	public void SaveControlSettings(string key, Variant value)
	{
		SaveKey("controls", key, value);
	}

	public Dictionary<string, Variant> LoadControlSettings()
	{
		return LoadSection("controls");
	}

	public void SaveKeybindings(StringName action, InputEvent inputEvent)
	{
		string eventStr = "";

		if (inputEvent is InputEventKey keyEvent)
			eventStr = OS.GetKeycodeString(keyEvent.PhysicalKeycode);
		else if (inputEvent is InputEventMouseButton mouseEvent)
			eventStr = $"mouse_{mouseEvent.ButtonIndex}";

		SaveKey("keybinding", action, eventStr);
	}

	public Dictionary<string, InputEvent> LoadKeybindings()
	{
		Dictionary<string, InputEvent> keybindings = new();

		if (!config.HasSection("keybinding"))
			return keybindings;

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
