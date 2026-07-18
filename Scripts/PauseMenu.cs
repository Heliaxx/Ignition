using Godot;

public partial class PauseMenu : ColorRect
{
	[ExportGroup("Node References")]
	[Export] private AnimationPlayer animator;
	[Export] private Button continueButton;
	[Export] private Button quitButton;
	[Export] private Button mainMenuButton;

	private AudioStreamPlayer hoverSound;
	private AudioStreamPlayer clickSound;

	public override void _Ready()
	{
		animator ??= GetNode<AnimationPlayer>("AnimationPlayer");
		continueButton ??= FindChild("ContinueButton") as Button;
		quitButton ??= FindChild("QuitButton") as Button;
		mainMenuButton ??= FindChild("MainMenuButton") as Button;

		if (continueButton == null || quitButton == null || mainMenuButton == null)
		{
			GD.PrintErr("PauseMenu: one or more buttons not found!");
			return;
		}

		hoverSound = new AudioStreamPlayer();
		hoverSound.Bus = "SFX";
		AddChild(hoverSound);
		hoverSound.Stream = (AudioStream)GD.Load("res://Imports/Sounds/hover_menu_sound.mp3");

		clickSound = new AudioStreamPlayer();
		clickSound.Bus = "SFX";
		AddChild(clickSound);
		clickSound.Stream = (AudioStream)GD.Load("res://Imports/Sounds/click_sound_menu2.mp3");

		continueButton.Pressed += () => Unpause();
		quitButton.Pressed += () => GetTree().Quit();
		mainMenuButton.Pressed += () => OnMainMenuButtonPressed();

		continueButton.MouseEntered += Click;
		quitButton.MouseEntered += Click;
		mainMenuButton.MouseEntered += Click;

		Unpause();
	}

	public void Unpause()
	{
		animator.Play("Unpause");
		GetTree().Paused = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;
		SetProcess(false);
		Visible = false;
	}

	public void Pause()
	{
		animator.Play("Pause");
		GetTree().Paused = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		SetProcess(true);
		Visible = true;
	}

	private void OnMainMenuButtonPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/Menu.tscn");
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("menu"))
		{
			if (GetTree().Paused)
				Unpause();
			else
				Pause();
		}
	}

	private void Click()
	{
		clickSound.Play();
	}
}
