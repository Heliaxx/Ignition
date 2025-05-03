extends CharacterBody3D

const MAX_SPEED = 300.0
const MAX_ANGULAR_SPEED = 2.0
const ACCELERATION = 20.0  # Jak rychle loď získává rychlost
const ROTATION_ACCELERATION = 2.0  # Jak rychle loď mění úhlovou rychlost

#var velocity = Vector3.ZERO
var angular_velocity = Vector3.ZERO  # Rotace ve všech osách (pitch, yaw, roll)
var thrust = Vector3.ZERO  # Ovládání tahu
var torque = Vector3.ZERO  # Ovládání rotace

const MOUSE_SENSITIVITY = 0.1
var pitch_input = 0.0
var yaw_input = 0.0
var roll_input = 0.0

@onready var barrel_right = $BarrelRight
@onready var barrel_left = $BarrelLeft
@onready var start_shooting = $ShootingStart
@onready var shooting = $Shooting
@onready var end_shooting = $ShootingEnd
@onready var light_left = $LeftLight
@onready var light_right = $RightLight
@onready var dustParticles = $dustParticles

var bullet = load("res://scenes/bullet.tscn")
var instanceRight
var instanceLeft


func _ready():
	pass


func _input(event):
	if event is InputEventMouseMotion:
		pitch_input = -event.relative.y * MOUSE_SENSITIVITY  # Myš nahoru/dolů = pitch
		yaw_input = -event.relative.x * MOUSE_SENSITIVITY  # Myš doleva/doprava = yaw

func _process(delta):
	apply_inputs(delta)

func apply_inputs(delta):
	# Pohybové vektory (thrust)
	thrust = Vector3.ZERO
	if Input.is_action_pressed("thrust_forward"):
		thrust += transform.basis.x * ACCELERATION
	if Input.is_action_pressed("thrust_backward"):
		thrust -= transform.basis.x * ACCELERATION
	if Input.is_action_pressed("strafe_up"):
		thrust += transform.basis.y * ACCELERATION
	if Input.is_action_pressed("straf_down"):
		thrust -= transform.basis.y * ACCELERATION
	if Input.is_action_pressed("strafe_right"):
		thrust += transform.basis.z * ACCELERATION
	if Input.is_action_pressed("strafe_left"):
		thrust -= transform.basis.z * ACCELERATION
	
	# Rotace
	torque = Vector3.ZERO
	
	# Q/E pro roll
	roll_input = Input.get_action_strength("roll_right") - Input.get_action_strength("roll_left")  # Q/E = roll
	torque.x = roll_input * ROTATION_ACCELERATION # Roll Q/E
	torque.y = yaw_input * ROTATION_ACCELERATION  # Yaw myš doleva/doprava
	torque.z = pitch_input * ROTATION_ACCELERATION  # Pitch myš nahoru/dolů

	# Aplikace hybnosti a rotace
	velocity += thrust * delta
	velocity = velocity.limit_length(MAX_SPEED)  # Omezení maximální rychlosti

	angular_velocity += torque * delta
	angular_velocity = angular_velocity.limit_length(MAX_ANGULAR_SPEED)  # Omezení maximální rotace

	# Aplikace pohybu a rotace
	move_and_collide(velocity * delta)
	rotate_object_local(Vector3(1, 0, 0), angular_velocity.x * delta)  # Roll (Q/E)
	rotate_object_local(Vector3(0, 1, 0), angular_velocity.y * delta)  # Yaw (myš doleva/doprava)
	rotate_object_local(Vector3(0, 0, 1), angular_velocity.z * delta)  # Pitch (myš nahoru/dolů)

func primary_fire(): 
	instanceLeft = bullet.instantiate()
	instanceRight = bullet.instantiate()
	instanceLeft.position = barrel_left.global_position
	instanceRight.position = barrel_right.global_position
	instanceLeft.transform.basis = barrel_left.global_transform.basis
	instanceRight.transform.basis = barrel_right.global_transform.basis
	get_parent().add_child(instanceLeft)
	get_parent().add_child(instanceRight)

func get_input(delta):
	var input_vector = Vector3.ZERO
	
	if Input.is_action_just_pressed("light"):
		light_left.visible = !light_left.visible
		light_right.visible = !light_right.visible

	if Input.is_action_pressed("primary_fire"):
		$TraumaCauser.cause_trauma()
		primary_fire()

	if Input.is_action_just_pressed("primary_fire"):
		shooting.play()

	if Input.is_action_just_released("primary_fire"):
		shooting.stop()
		end_shooting.play()

func _physics_process(delta):
	get_input(delta)
