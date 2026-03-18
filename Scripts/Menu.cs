using Godot;
using System;

public partial class Menu : Control
{
	[ExportGroup("Node References")]
	[Export] private Button controlsButton;
	[Export] private Button graphicsButton;
	[Export] private Button audioButton;
	[Export] private Button FreeFlyButton;
	[Export] private Button WavesButton;
	[Export] private Button RushButton;
	[Export] private Button exitButton;

	private AudioStreamPlayer hoverSound;
	private AudioStreamPlayer clickSound;

	public override void _Ready()
	{
		GetTree().Paused = false;
		EventBus.ClearAll();

		controlsButton ??= GetNode<Button>("PanelContainer/VBoxContainer/ControlsButton");
		graphicsButton ??= GetNode<Button>("PanelContainer/VBoxContainer/GraphicsButton");
		audioButton ??= GetNode<Button>("PanelContainer/VBoxContainer/AudioButton");
		exitButton ??= GetNode<Button>("PanelContainer/VBoxContainer/ExitButton");
		FreeFlyButton ??= GetNode<Button>("PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/FreeFlyButton");
		RushButton ??= GetNode<Button>("PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/RushButton");
		WavesButton ??= GetNode<Button>("PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/WavesButton");

		hoverSound = new AudioStreamPlayer();
		clickSound = new AudioStreamPlayer();

		AddChild(hoverSound);
		AddChild(clickSound);

		hoverSound.Stream = (AudioStream)GD.Load("res://Imports/Sounds/hover_menu_sound.mp3");
		clickSound.Stream = (AudioStream)GD.Load("res://Imports/Sounds/click_sound_menu2.mp3");

		var musicManager = GetNode<MusicManager>("/root/MusicManager");
		var music = GD.Load<AudioStream>("res://Imports/Sounds/main_menu_2.mp3");
		if (!musicManager.IsPlaying())
			musicManager.PlayMusic(music);

		FreeFlyButton.Pressed += OnPlayFreeFlyButtonPressed;
		RushButton.Pressed += OnPlayRushButtonPressed;
		WavesButton.Pressed += OnPlayWavesButtonPressed;
		exitButton.Pressed += OnExitButtonPressed;
		controlsButton.Pressed += OnControlsPressed;
		graphicsButton.Pressed += OnGraphicsPressed;
		audioButton.Pressed += OnAudioPressed;

		FreeFlyButton.MouseEntered += OnButtonHovered;
		RushButton.MouseEntered += OnButtonHovered;
		WavesButton.MouseEntered += OnButtonHovered;
		exitButton.MouseEntered += OnButtonHovered;
		controlsButton.MouseEntered += OnButtonHovered;
		graphicsButton.MouseEntered += OnButtonHovered;
		audioButton.MouseEntered += OnButtonHovered;
	}

	private void OnPlayFreeFlyButtonPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/LevelFreeFlight.tscn");
	}

	private void OnPlayWavesButtonPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/LevelWave.tscn");
	}

	private void OnPlayRushButtonPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/LevelRush.tscn");
	}

	private void OnExitButtonPressed()
	{
		GetTree().Quit();
	}

	private void OnControlsPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/Controls.tscn");
	}

	private void OnGraphicsPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/Graphics.tscn");
	}

	private void OnAudioPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/Audio.tscn");
	}

	private void OnButtonHovered()
	{
		clickSound.Play();
	}
}
