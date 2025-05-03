using Godot;
using System;
using System.Collections.Generic;

public partial class Controls : Control
{
	[Export]
	public PackedScene InputButtonScene;

	private VBoxContainer ActionList;
	private bool IsRemapping = false;
	private string ActionToRemap = null;
	private Button RemappingButton = null;

	private Dictionary<string, string> InputActions = new()
	{
		{ "thrust_forward", "Thrust forward" },
		{ "thrust_backward", "Thrust backward" },
		{ "roll_right", "Roll right" },
		{ "roll_left", "Roll left" },
		{ "strafe_right", "Strafe right" },
		{ "strafe_left", "Strafe left" },
		{ "strafe_up", "Strafe up" },
		{ "strafe_down", "Strafe down" },
		{ "light", "Light" },
		{ "boost", "Boost" }
	};

	public override void _Ready()
	{
		InputButtonScene ??= GD.Load<PackedScene>("res://Scenes/input_button.tscn");
		ActionList = GetNode<VBoxContainer>("PanelContainer/MarginContainer/VBoxContainer/ScrollContainer/ActionList");

		LoadKeybindingsFromSettings();
		CreateActionList();
	}

	private void LoadKeybindingsFromSettings()
	{
		var keybindings = ConfigFileHandler.Instance.LoadKeybindings();
		foreach (var action in keybindings.Keys)
		{
			InputMap.ActionEraseEvents(action);
			InputMap.ActionAddEvent(action, keybindings[action]);
		}
	}

	private void CreateActionList()
	{
		foreach (var child in ActionList.GetChildren())
			child.QueueFree();

		foreach (var kvp in InputActions)
		{
			string action = kvp.Key;
			string label = kvp.Value;

			var button = InputButtonScene.Instantiate<Button>();
			var actionLabel = button.FindChild("LabelAction") as Label;
			var inputLabel = button.FindChild("LabelInput") as Label;

			actionLabel.Text = label;

			var events = InputMap.ActionGetEvents(action);
			if (events.Count > 0)
				inputLabel.Text = events[0].AsText().TrimSuffix(" (Physical)");
			else
				inputLabel.Text = "";

			ActionList.AddChild(button);
			button.Pressed += () => _on_input_button_pressed(button, action);
		}
	}

	private void _on_input_button_pressed(Button button, string action)
	{
		if (!IsRemapping)
		{
			IsRemapping = true;
			ActionToRemap = action;
			RemappingButton = button;

			if (button.FindChild("LabelInput") is Label label)
				label.Text = "Press key to bind...";
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (!IsRemapping)
			return;

		if (@event is InputEventKey || (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed))
		{
			InputMap.ActionEraseEvents(ActionToRemap);
			InputMap.ActionAddEvent(ActionToRemap, @event);
			ConfigFileHandler.Instance.SaveKeybindings(ActionToRemap, @event);
			UpdateActionList(RemappingButton, @event);

			IsRemapping = false;
			ActionToRemap = null;
			RemappingButton = null;
			GetViewport().SetInputAsHandled();
		}
	}

	private void UpdateActionList(Button button, InputEvent @event)
	{
		if (button.FindChild("LabelInput") is Label label)
			label.Text = @event.AsText().TrimSuffix(" (Physical)");
	}

	private void _on_back_btn_pressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/Menu.tscn");
	}
}