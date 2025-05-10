using Godot;
using System;

public partial class Menu : Control
{
	private Button controlsButton;
	private Button graphicsButton;
	private Button audioButton;
	// private Button playButton;
	private Button freeFlyButton;
	private Button skirmishButton;
	private Button wavesButton;
	private Button exitButton;
	
	private AudioStreamPlayer hoverSound;
	private AudioStreamPlayer clickSound;

	public override void _Ready()
	{
		GetTree().Paused = false;
		controlsButton = GetNode<Button>("PanelContainer/VBoxContainer/ControlsButton");
		graphicsButton = GetNode<Button>("PanelContainer/VBoxContainer/GraphicsButton");
		audioButton = GetNode<Button>("PanelContainer/VBoxContainer/AudioButton");
		// playButton = GetNode<Button>("PanelContainer/VBoxContainer/PlayButton");
		exitButton = GetNode<Button>("PanelContainer/VBoxContainer/ExitButton");
		freeFlyButton = GetNode<Button>("PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/FreeFlyButton");
		skirmishButton = GetNode<Button>("PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/SkirmishButton");
		wavesButton = GetNode<Button>("PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/WavesButton");

		// Create and add audio players
		hoverSound = new AudioStreamPlayer();
		clickSound = new AudioStreamPlayer();

		AddChild(hoverSound);
		AddChild(clickSound);

		// Load audio files
		hoverSound.Stream = (AudioStream)GD.Load("res://Imports/Sounds/hover_menu_sound.mp3");
		clickSound.Stream = (AudioStream)GD.Load("res://Imports/Sounds/click_sound_menu2.mp3");

		var musicManager = GetNode<MusicManager>("/root/MusicManager");
		var music = GD.Load<AudioStream>("res://Imports/Sounds/main_menu_2.mp3");
		if (!musicManager.IsPlaying())
			musicManager.PlayMusic(music);

		// Connect button signals
		// playButton.Pressed += OnPlayButtonPressed;
		freeFlyButton.Pressed += OnPlayFreeFlyButtonPressed;
		skirmishButton.Pressed += OnPlaySkirmishButtonPressed;
		wavesButton.Pressed += OnPlayWavesButtonPressed;
		exitButton.Pressed += OnExitButtonPressed;
		controlsButton.Pressed += OnControlsPressed;
		graphicsButton.Pressed += OnGraphicsPressed;
		audioButton.Pressed += OnAudioPressed;

		// Connect hover sound effect
		// playButton.MouseEntered += OnButtonHovered;
		freeFlyButton.MouseEntered += OnButtonHovered;
		skirmishButton.MouseEntered += OnButtonHovered;
		wavesButton.MouseEntered += OnButtonHovered;
		exitButton.MouseEntered += OnButtonHovered;
		controlsButton.MouseEntered += OnButtonHovered;
		graphicsButton.MouseEntered += OnButtonHovered;
		audioButton.MouseEntered += OnButtonHovered;
	}

	// private void OnPlayButtonPressed()
	// {
	// 	GetTree().ChangeSceneToFile("res://Scenes/LevelWave.tscn");
	// }

	private void OnPlayFreeFlyButtonPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/LevelFreeFlight.tscn");
	}

	private void OnPlaySkirmishButtonPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/LevelSkirmish.tscn");
	}

	private void OnPlayWavesButtonPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/LevelWave.tscn");
	}

	private void OnExitButtonPressed()
	{
		GetTree().Quit();
	}

	private void OnControlsPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/controls.tscn");
	}

	private void OnGraphicsPressed()
	{
		return;
		// GetTree().ChangeSceneToFile("res://Scenes/graphics.tscn");
	}

	private void OnAudioPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/audio.tscn");
	}

	private void OnButtonHovered()
	{
		clickSound.Play();
	}
}
