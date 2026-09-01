using Godot;

// Level picker for the singleplayer challenges. Pushed onto the MenuStack, so it closes
// back to the main menu rather than changing scene.
public partial class PveChallenges : Control
{
	private static readonly (string Button, string Scene)[] Levels =
	{
		("FreeFlyButton",  "res://Scenes/LevelFreeFlight.tscn"),
		("RushButton",     "res://Scenes/LevelRush.tscn"),
		("WavesButton",    "res://Scenes/LevelWave.tscn"),
		("SkirmishButton", "res://Scenes/LevelSkirmish.tscn"),
	};

	public override void _Ready()
	{
		MenuUtils.AttachButtonSounds(this);

		foreach ((string button, string scene) in Levels)
		{
			string path = scene;
			((Button)FindChild(button)).Pressed += () => GetTree().ChangeSceneToFile(path);
		}

		((Button)FindChild("BackButton")).Pressed += () => GetParent<MenuStack>().Pop();
	}
}
