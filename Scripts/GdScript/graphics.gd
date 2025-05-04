extends Control

@onready var mode = $Menu/Options/ModeDropdown
@onready var vsync = $Menu/Options/VsyncCheck
@onready var fxaa = $Menu/Options/FXAACheck
@onready var taa = $Menu/Options/TAACheck
@onready var msaa = $Menu/Options/MSAADropdown
@onready var resolution = $Menu/Options/ResolutionDropdown

var modeOptions = ["fullscreen", "borderless", "windowed"]
var aaOptions = ["MSAAx2", "MSAAx4", "MSAAx8", "OFF"]
var resolutionOptions = ["Default", "800x600", "1280x720", "1366x768", "1600x900", "1920x1080", "2560x1440"]

# Called when the node enters the scene tree for the first time.
func _ready():
	
	var video_settings = ConfigFileHandler.load_video_settings()
	
	# Nastavení UI prvků podle uložené konfigurace
	#mode.selected = modeOptions.find(video_settings.get("mode", "fullscreen"))
	mode.selected = video_settings.mode
	vsync.button_pressed = video_settings.vsync
	fxaa.button_pressed = video_settings.fxaa
	taa.button_pressed = video_settings.taa
	msaa.selected = video_settings.msaa
	resolution.selected = video_settings.resolution
	#msaa.selected = aaOptions.find(video_settings.get("msaa", "4x"))
	#resolution.selected = resolutionOptions.find(video_settings.get("resolution", "1920x1080"))

	
	#var video_settings = ConfigFileHandler.load_video_settings()
	#
	#vsync.button_pressed = video_settings.vsync
	#fxaa.button_pressed = video_settings.fxaa
	#taa.button_pressed = video_settings.taa
	

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass

func _on_back_btn_pressed():
	get_tree().change_scene_to_file("res://Scenes/Menu.tscn")

func _on_vsync_check_toggled(toggled_on):
	ConfigFileHandler.save_video_settings("vsync", toggled_on)

func _on_fxaa_check_toggled(toggled_on):
	ConfigFileHandler.save_video_settings("fxaa", toggled_on)

func _on_taa_check_toggled(toggled_on):
	ConfigFileHandler.save_video_settings("taa", toggled_on)


func _on_mode_dropdown_item_selected(index):
	var selected_mode = modeOptions[index]
	match selected_mode:
		"fullscreen":
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
		"borderless":
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_MAXIMIZED)
		"windowed":
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	ConfigFileHandler.save_video_settings("mode", selected_mode)


func _on_msaa_dropdown_item_selected(index):
	var selected_aa = aaOptions[index]
	match selected_aa:
		"MSAAx2":
			get_viewport().msaa_2d = Viewport.MSAA_2X
		"MSAAx4":
			get_viewport().msaa_2d = Viewport.MSAA_4X
		"MSAAx8":
			get_viewport().msaa_2d = Viewport.MSAA_8X
		"OFF":
			get_viewport().msaa_2d = Viewport.MSAA_DISABLED
	ConfigFileHandler.save_video_settings("msaa", selected_aa)



func _on_resolution_dropdown_item_selected(index):
	var selected_res = resolutionOptions[index]
	if selected_res != "Default":
		var res = selected_res.split("x")
		DisplayServer.window_set_size(Vector2i(int(res[0]), int(res[1])))
	ConfigFileHandler.save_video_settings("resolution", selected_res)
