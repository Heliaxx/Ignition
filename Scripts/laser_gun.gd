extends RayCast3D

@onready var beam_mesh := $BeamMesh
@onready var end_particles := $EndParticles
@onready var beam_particles := $BeamParticles

# Damage per second when the beam is at full radius
@export var damage_max:float = 20.0
@export var ray_length:float = 400.0

@export var power_on_time:float = 5.0 # Seconds
@export var power_off_time:float = 5.0 # Seconds
# Measures how far into powering on/off the
# beam is, so that if there's an interrupt,
# the inverse operation can be correspondingly
# fast. For example:
# If we've only been powering up for 0.5 seconds
# then we should only power down for 0.5 seconds,
# otherwise a teeny tiny beam lingers on the screen.
var progress_time:float # Seconds

var tween:Tween
var beam_radius:float = 0.5

var stay_on:bool = false

var state := LaserState.OFF
enum LaserState {
	OFF,
	ON,
	POWERING_OFF,
	POWERING_ON
}


# Ca I led when the node enters the scene tree for the first tíne.
func _ready():
	pass
	#await get_tree().create_timer(2.0).timeout
	#deactivate(1)
	#await get_tree().create_timer(2.0).timeout
	#activate(1)


func _process(delta: float) -> void:
	force_raycast_update()

	if is_colliding():
		var cast_point: Vector3 = to_local(get_collision_point())

		# Nastavení výšky a pozice beam meshe
		beam_mesh.mesh.height = -cast_point.y
		beam_mesh.position.y = cast_point.y / 2.0

		# Nastavení pozice částic
		end_particles.position.y = cast_point.y
		beam_particles.position.y = cast_point.y / 2.0

		# Výpočet množství částic
		var particle_amount: int = snapped(abs(cast_point.y) * 50.0, 1)

		# Přiřazení množství částic (minimálně 1)
		beam_particles.amount = max(particle_amount, 1)

		# Nastavení velikosti emise částic
		var radius = beam_mesh.mesh.top_radius
		var height = abs(cast_point.y) / 2.0
		beam_particles.process_material.set_emission_box_extents(Vector3(radius, height, radius))

#func deal_damage(collider, delta:float) -> void:
	#if collider.is_in_group("damageable"):
		##print("dealing damage %f" % (damage*delta))
		#collider.damage(damage*delta, data.shooter)

func activate(time: float) -> void:
	tween = get_tree().create_tween()
	visible = true
	beam_particles.emitting = true
	end_particles.emitting = true

	tween.set_parallel(true)
	tween.tween_property(beam_mesh.mesh, "top_radius", beam_radius, time)
	tween.tween_property(beam_mesh.mesh, "bottom_radius", beam_radius, time)
	tween.tween_property(beam_particles.process_material, "scale_min", 1.0, time)
	tween.tween_property(end_particles.process_material, "scale_min", 1.0, time)

	await tween.finished


func deactivate(time: float) -> void:
	tween = get_tree().create_tween()

	tween.set_parallel(true)
	tween.tween_property(beam_mesh.mesh, "top_radius", 0.0, time)
	tween.tween_property(beam_mesh.mesh, "bottom_radius", 0.0, time)
	tween.tween_property(beam_particles.process_material, "scale_min", 0.0, time)
	tween.tween_property(end_particles.process_material, "scale_min", 0.0, time)

	await tween.finished

	visible = false
	beam_particles.emitting = false
	end_particles.emitting = false
