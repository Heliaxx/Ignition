extends Control

@onready var general_volume_slider = $Menu/Options/VolGeneralSlider
@onready var music_volume_slider = $Menu/Options/VolMusicSlider
@onready var sfx_volume_slider = $Menu/Options/VolSFXSlider

# Called when the node enters the scene tree for the first time.
func _ready():
	var audio_settings = ConfigFileHandler.load_audio_settings()
	general_volume_slider.value = min(audio_settings.general_volume, 1.0) * 100
	music_volume_slider.value = min(audio_settings.music_volume, 1.0) * 100
	sfx_volume_slider.value = min(audio_settings.sfx_volume, 1.0) * 100
	_apply_audio_settings()
	
# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass

func _apply_audio_settings():
	AudioServer.set_bus_volume_db(AudioServer.get_bus_index("Master"), linear_to_db(music_volume_slider.value / 100.0))
	AudioServer.set_bus_volume_db(AudioServer.get_bus_index("Music"), linear_to_db(music_volume_slider.value / 100.0))
	AudioServer.set_bus_volume_db(AudioServer.get_bus_index("SFX"), linear_to_db(sfx_volume_slider.value / 100.0))

func _on_back_btn_pressed():
	get_tree().change_scene_to_file("res://Scenes/Menu.tscn")


func _on_vol_general_slider_drag_ended(value_changed):
	if value_changed:
		var volume = general_volume_slider.value / 100.0
		ConfigFileHandler.save_audio_settings("general_volume", volume)
		AudioServer.set_bus_volume_db(AudioServer.get_bus_index("Master"), linear_to_db(volume))


func _on_vol_music_slider_drag_ended(value_changed):
	if value_changed:
		var volume = music_volume_slider.value / 100.0
		ConfigFileHandler.save_audio_settings("music_volume", volume)
		AudioServer.set_bus_volume_db(AudioServer.get_bus_index("Music"), linear_to_db(volume))


func _on_vol_sfx_slider_drag_ended(value_changed):
	if value_changed:
		var volume = sfx_volume_slider.value / 100.0
		ConfigFileHandler.save_audio_settings("sfx_volume", volume)
		AudioServer.set_bus_volume_db(AudioServer.get_bus_index("SFX"), linear_to_db(volume))
