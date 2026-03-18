using Godot;
using System;

public partial class Graphics : Control
{
	[ExportGroup("Node References")]
	[Export] private OptionButton fpsDropdown;
	[Export] private OptionButton modeDropdown;
	[Export] private CheckBox vsyncCheck;
	[Export] private CheckBox fxaaCheck;
	[Export] private CheckBox taaCheck;
	[Export] private OptionButton msaaDropdown;
	[Export] private OptionButton resolutionDropdown;
	[Export] private Button backBtn;

	private ConfigFileHandler configHandler;
	private readonly int?[] fpsOptions = { null, 30, 60, 90, 144, 240 }; // null = Unlimited

	public override void _Ready()
	{
		fpsDropdown ??= GetNode<OptionButton>("Menu/Options/FPSDropdown");
		modeDropdown ??= GetNode<OptionButton>("Menu/Options/ModeDropdown");
		vsyncCheck ??= GetNode<CheckBox>("Menu/Options/VsyncCheck");
		fxaaCheck ??= GetNode<CheckBox>("Menu/Options/FXAACheck");
		taaCheck ??= GetNode<CheckBox>("Menu/Options/TAACheck");
		msaaDropdown ??= GetNode<OptionButton>("Menu/Options/MSAADropdown");
		resolutionDropdown ??= GetNode<OptionButton>("Menu/Options/ResolutionDropdown");
		backBtn ??= GetNode<Button>("HBoxContainer/BackBtn");

		configHandler = GetTree().Root.GetNode<ConfigFileHandler>("/root/ConfigFileHandler");

		InitializeFPSDropdown();
		InitializeModeDropdown();
		InitializeMSAADropdown();
		InitializeResolutionDropdown();

		FixDropdownPopup(fpsDropdown);
		FixDropdownPopup(modeDropdown);
		FixDropdownPopup(msaaDropdown);
		FixDropdownPopup(resolutionDropdown);

		LoadCurrentSettings();
	}

	private void FixDropdownPopup(OptionButton dropdown)
	{
		var popup = dropdown.GetPopup();
		popup.AddThemeFontSizeOverride("font_size", (int)dropdown.GetThemeFontSize("font_size"));
		if (dropdown.HasThemeFontOverride("font"))
			popup.AddThemeFontOverride("font", dropdown.GetThemeFont("font"));
	}

	private void InitializeFPSDropdown()
	{
		fpsDropdown.Clear();
		fpsDropdown.AddItem("Unlimited", 0);
		for (int i = 1; i < fpsOptions.Length; i++)
		{
			fpsDropdown.AddItem($"{fpsOptions[i]} FPS", fpsOptions[i] ?? 0);
		}

		int currentFps = Engine.MaxFps;
		if (currentFps <= 0)
		{
			fpsDropdown.Selected = 0;
			return;
		}
		for (int i = 1; i < fpsOptions.Length; i++)
		{
			if (fpsOptions[i] == currentFps)
			{
				fpsDropdown.Selected = i;
				return;
			}
		}
		fpsDropdown.Selected = 3;
	}

	private void InitializeModeDropdown()
	{
		modeDropdown.Clear();
		modeDropdown.AddItem("Windowed", 0);
		modeDropdown.AddItem("Borderless", 1);
		modeDropdown.AddItem("Fullscreen", 2);
	}

	private void InitializeMSAADropdown()
	{
		msaaDropdown.Clear();
		msaaDropdown.AddItem("Disabled", 0);
		msaaDropdown.AddItem("2x", 1);
		msaaDropdown.AddItem("4x", 2);
		msaaDropdown.AddItem("8x", 3);
	}

	private void InitializeResolutionDropdown()
	{
		resolutionDropdown.Clear();
		resolutionDropdown.AddItem("854x480", 0);
		resolutionDropdown.AddItem("1280x720", 1);
		resolutionDropdown.AddItem("1920x1080", 2);
		resolutionDropdown.AddItem("2560x1440", 3);
		resolutionDropdown.AddItem("3840x2160", 4);
	}

	private void LoadCurrentSettings()
	{
		configHandler.ApplyVideoSettings();

		var videoSettings = configHandler.LoadVideoSettings();

		int fps = videoSettings.ContainsKey("fps") ? (int)videoSettings["fps"] : 90;
		for (int i = 0; i < fpsOptions.Length; i++)
		{
			if ((fpsOptions[i] ?? 0) == fps)
			{
				fpsDropdown.Selected = i;
				break;
			}
		}

		vsyncCheck.ButtonPressed = videoSettings.ContainsKey("vsync") && (bool)videoSettings["vsync"];
		fxaaCheck.ButtonPressed  = videoSettings.ContainsKey("fxaa")  && (bool)videoSettings["fxaa"];
		taaCheck.ButtonPressed   = videoSettings.ContainsKey("taa")   && (bool)videoSettings["taa"];

		string mode = videoSettings.ContainsKey("mode") ? videoSettings["mode"].ToString() : "fullscreen";
		modeDropdown.Selected = mode switch { "windowed" => 0, "borderless" => 1, _ => 2 };

		string msaa = videoSettings.ContainsKey("msaa") ? videoSettings["msaa"].ToString() : "4x";
		msaaDropdown.Selected = msaa switch { "2x" => 1, "4x" => 2, "8x" => 3, _ => 0 };

		string res = videoSettings.ContainsKey("resolution") ? videoSettings["resolution"].ToString() : "1920x1080";
		resolutionDropdown.Selected = res switch
		{
			"854x480"   => 0,
			"1280x720"  => 1,
			"2560x1440" => 3,
			"3840x2160" => 4,
			_           => 2
		};
	}

	private void _on_fps_dropdown_item_selected(int index)
	{
		int? fps = fpsOptions[index];
		Engine.MaxFps = fps ?? 0;
		configHandler.SaveVideoSettings("fps", fps ?? 0);
		GD.Print(fps == null ? "FPS limit set to: Unlimited" : $"FPS limit set to: {fps}");
	}

	private void _on_mode_dropdown_item_selected(int index)
	{
		switch (index)
		{
			case 0:
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
				configHandler.SaveVideoSettings("mode", "windowed");
				break;
			case 1:
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
				DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);
				configHandler.SaveVideoSettings("mode", "borderless");
				break;
			case 2:
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
				DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
				configHandler.SaveVideoSettings("mode", "fullscreen");
				break;
		}
	}

	private void _on_vsync_check_toggled(bool toggled)
	{
		DisplayServer.WindowSetVsyncMode(toggled ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
		configHandler.SaveVideoSettings("vsync", toggled);
	}

	private void _on_fxaa_check_toggled(bool toggled)
	{
		GetViewport().ScreenSpaceAA = toggled ? Viewport.ScreenSpaceAAEnum.Fxaa : Viewport.ScreenSpaceAAEnum.Disabled;
		configHandler.SaveVideoSettings("fxaa", toggled);
	}

	private void _on_taa_check_toggled(bool toggled)
	{
		GetViewport().UseTaa = toggled;
		configHandler.SaveVideoSettings("taa", toggled);
	}

	private void _on_msaa_dropdown_item_selected(int index)
	{
		GetViewport().Msaa3D = (Viewport.Msaa)index;
		string msaaStr = index switch
		{
			1 => "2x",
			2 => "4x",
			3 => "8x",
			_ => "disabled"
		};
		configHandler.SaveVideoSettings("msaa", msaaStr);
	}

	private void _on_resolution_dropdown_item_selected(int index)
	{
		string resStr = index switch
		{
			0 => "854x480",
			1 => "1280x720",
			2 => "1920x1080",
			3 => "2560x1440",
			4 => "3840x2160",
			_ => "1920x1080"
		};
		Vector2I resolution = index switch
		{
			0 => new Vector2I(854, 480),
			1 => new Vector2I(1280, 720),
			2 => new Vector2I(1920, 1080),
			3 => new Vector2I(2560, 1440),
			4 => new Vector2I(3840, 2160),
			_ => new Vector2I(1920, 1080)
		};
		DisplayServer.WindowSetSize(resolution);
		configHandler.SaveVideoSettings("resolution", resStr);
	}

	private void _on_back_btn_pressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/Menu.tscn");
	}
}
