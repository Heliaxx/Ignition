using Godot;
using System.Collections.Generic;

public partial class ConfigFileHandler
{
	private const int    DefaultFps        = 90;
	private const string DefaultMode       = "fullscreen";
	private const bool   DefaultVsync      = false;
	private const bool   DefaultFxaa       = true;
	private const bool   DefaultTaa        = false;
	private const string DefaultMsaa       = "4x";
	private const string DefaultResolution = "1920x1080";

	public void SaveVideoSettings(string key, Variant value)
	{
		SaveKey("video", key, value);
	}

	public Dictionary<string, Variant> LoadVideoSettings()
	{
		return LoadSection("video");
	}

	public void ResetVideoSettings()
	{
		config.SetValue("video", "fps",        DefaultFps);
		config.SetValue("video", "mode",       DefaultMode);
		config.SetValue("video", "vsync",      DefaultVsync);
		config.SetValue("video", "fxaa",       DefaultFxaa);
		config.SetValue("video", "taa",        DefaultTaa);
		config.SetValue("video", "msaa",       DefaultMsaa);
		config.SetValue("video", "resolution", DefaultResolution);
		config.Save(SETTINGS_FILE_PATH);
		ApplyVideoSettings();
	}

	private void EnsureVideoDefaults()
	{
		bool changed = false;
		void Ensure(string key, Variant value)
		{
			if (config.HasSectionKey("video", key)) return;
			config.SetValue("video", key, value);
			changed = true;
		}
		Ensure("fps",        DefaultFps);
		Ensure("mode",       DefaultMode);
		Ensure("vsync",      DefaultVsync);
		Ensure("fxaa",       DefaultFxaa);
		Ensure("taa",        DefaultTaa);
		Ensure("msaa",       DefaultMsaa);
		Ensure("resolution", DefaultResolution);
		if (changed) config.Save(SETTINGS_FILE_PATH);
	}

	public void ApplyVideoSettings()
	{
		var videoSettings = LoadVideoSettings();
		var viewport = GetViewport();

		// FPS
		int fps = videoSettings.ContainsKey("fps") ? (int)videoSettings["fps"] : 90;
		Engine.MaxFps = fps;

		// VSync
		bool vsync = videoSettings.ContainsKey("vsync") && (bool)videoSettings["vsync"];
		DisplayServer.WindowSetVsyncMode(vsync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);

		// FXAA
		bool fxaa = videoSettings.ContainsKey("fxaa") && (bool)videoSettings["fxaa"];
		viewport.ScreenSpaceAA = fxaa ? Viewport.ScreenSpaceAAEnum.Fxaa : Viewport.ScreenSpaceAAEnum.Disabled;

		// TAA
		bool taa = videoSettings.ContainsKey("taa") && (bool)videoSettings["taa"];
		viewport.UseTaa = taa;

		// Window mode
		string mode = videoSettings.ContainsKey("mode") ? videoSettings["mode"].ToString() : "fullscreen";
		switch (mode)
		{
			case "windowed":
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
				break;
			case "borderless":
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
				DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);
				break;
			case "fullscreen":
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
				DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
				break;
		}

		// MSAA
		string msaa = videoSettings.ContainsKey("msaa") ? videoSettings["msaa"].ToString() : "4x";
		int msaaIndex = msaa switch { "2x" => 1, "4x" => 2, "8x" => 3, _ => 0 };
		viewport.Msaa3D = msaaIndex == 0 ? Viewport.Msaa.Disabled : (Viewport.Msaa)msaaIndex;

		// Resolution
		string res = videoSettings.ContainsKey("resolution") ? videoSettings["resolution"].ToString() : "1920x1080";
		Vector2I resolution = res switch
		{
			"854x480"   => new Vector2I(854,  480),
			"1280x720"  => new Vector2I(1280, 720),
			"2560x1440" => new Vector2I(2560, 1440),
			"3840x2160" => new Vector2I(3840, 2160),
			_           => new Vector2I(1920, 1080)
		};
		DisplayServer.WindowSetSize(resolution);
	}
}
