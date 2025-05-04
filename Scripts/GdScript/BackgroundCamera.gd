extends Camera3D

var callable = Callable(self, "update_bg_cam")

# Called when the node enters the scene tree for the first time.
func _ready():
	GlobalSignals.connect("camera_update", callable, 0)
	
func update_bg_cam(rot):
	rotation = rot
