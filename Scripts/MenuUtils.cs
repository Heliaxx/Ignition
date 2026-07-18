using Godot;

// Shared menu behavior helpers so submenus sound and feel like the main menu
// and the in-game pause menu.
public static class MenuUtils
{
	// Creates the shared hover-sound player and wires every menu control under
	// root. Returns the player so dynamically rebuilt controls can be wired
	// again later via WireControls.
	public static AudioStreamPlayer AttachButtonSounds(Node root)
	{
		var click = new AudioStreamPlayer();
		click.Bus = "SFX";
		click.Stream = GD.Load<AudioStream>("res://Imports/Sounds/click_sound_menu2.mp3");
		root.AddChild(click);
		WireControls(root, click);
		return click;
	}

	// Recursively gives Buttons the menu hover sound, and applies the
	// pointing-hand cursor to Buttons and Sliders alike.
	public static void WireControls(Node node, AudioStreamPlayer click)
	{
		if (node is Button button)
		{
			button.MouseEntered += () => click.Play();
			button.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
		}
		else if (node is Slider slider)
		{
			slider.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
		}
		foreach (Node child in node.GetChildren())
			WireControls(child, click);
	}
}
