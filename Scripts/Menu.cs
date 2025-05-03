using Godot;
using System;

public partial class Menu : Control
{
	private Button controlsButton;
	private Button graphicsButton;
	private Button audioButton;
	private Button playButton;
	private Button exitButton;
	
	private AudioStreamPlayer backgroundMusic;
	private AudioStreamPlayer hoverSound;
	private AudioStreamPlayer clickSound;

	public override void _Ready()
	{
		GetTree().Paused = false;
		// Get button nodes
		controlsButton = GetNode<Button>("PanelContainer/VBoxContainer/ControlsButton");
		graphicsButton = GetNode<Button>("PanelContainer/VBoxContainer/GraphicsButton");
		audioButton = GetNode<Button>("PanelContainer/VBoxContainer/AudioButton");
		playButton = GetNode<Button>("PanelContainer/VBoxContainer/PlayButton");
		exitButton = GetNode<Button>("PanelContainer/VBoxContainer/ExitButton");

		// Create and add audio players
		backgroundMusic = new AudioStreamPlayer();
		hoverSound = new AudioStreamPlayer();
		clickSound = new AudioStreamPlayer();

		AddChild(backgroundMusic);
		AddChild(hoverSound);
		AddChild(clickSound);

		// Load audio files
		backgroundMusic.Stream = (AudioStream)GD.Load("res://Imports/Sounds/main_menu_2.mp3");
		hoverSound.Stream = (AudioStream)GD.Load("res://Imports/Sounds/hover_menu_sound.mp3");
		clickSound.Stream = (AudioStream)GD.Load("res://Imports/Sounds/click_sound_menu2.mp3");

		backgroundMusic.Play(); // Start playing menu music

		// Connect button signals
		playButton.Pressed += OnPlayButtonPressed;
		exitButton.Pressed += OnExitButtonPressed;
		controlsButton.Pressed += OnControlsPressed;
		graphicsButton.Pressed += OnGraphicsPressed;
		audioButton.Pressed += OnAudioPressed;

		// Connect hover sound effect
		playButton.MouseEntered += OnButtonHovered;
		exitButton.MouseEntered += OnButtonHovered;
		controlsButton.MouseEntered += OnButtonHovered;
		graphicsButton.MouseEntered += OnButtonHovered;
		audioButton.MouseEntered += OnButtonHovered;
	}

	private void OnPlayButtonPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/Level.tscn");
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
		GetTree().ChangeSceneToFile("res://Scenes/graphics.tscn");
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
