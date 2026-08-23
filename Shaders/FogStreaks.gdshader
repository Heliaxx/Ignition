shader_type fog;

/**
 * Fog density inside a fully solid patch. The noise mask only ever scales this
 * value down, so it is the hard ceiling for how thick the fog can get.
 * Useful values are far below the slider maximum - at the scale of a whole
 * level, anything past a few thousandths turns into an opaque wall.
 */
uniform float base_density : hint_range(0.0, 1.0) = 0.15;

/**
 * Scattering tint of the fog. This is an albedo, not an emission: the fog is
 * still lit by the scene, so it only ever tints the light passing through it
 * and stays invisible in unlit space.
 */
uniform vec3 base_color : source_color = vec3(0.99, 0.87, 0.7);

/**
 * Noise units per world unit, per axis. Smaller values stretch the pattern out
 * into larger, sparser patches; larger values pack it into smaller ones.
 * The axes are independent, which is where the streaks come from - keeping Y
 * well above X and Z squashes the shapes into thin horizontal layers.
 */
uniform vec3 world_scale = vec3(0.002, 0.0036, 0.002);

/**
 * How much each of the three noise octaves contributes to the final pattern,
 * in order (X = first, Y = second, Z = third).
 * The three add up into the value that gets thresholded, so raising them all
 * widens that range and yields denser, higher-contrast coverage. Typically
 * fades from the strongest first octave to the weakest last one.
 */
uniform vec3 noise_amounts = vec3(0.4, 0.35, 0.25);

/**
 * Frequency multiplier of each noise octave, in order (X = first, Y = second,
 * Z = third). Low values give broad cloud shapes, high values fine detail.
 * Spread them apart - octaves at similar frequencies just blur together.
 */
uniform vec3 noise_scales = vec3(1.0, 2.3, 4.7);

/**
 * Drift of the first noise octave, in noise units per second.
 * The direction sets which way that layer slides, the length sets how fast.
 * Not affected by [code]noise_scales[/code], so a given value drifts every
 * octave at the same absolute rate.
 */
uniform vec3 noise_velocity1 = vec3(0.004, 0.0, 0.002);

/**
 * Drift of the second noise octave, in noise units per second.
 * Keep it different from the other two - matching velocities make the whole
 * pattern slide as one rigid block instead of churning.
 */
uniform vec3 noise_velocity2 = vec3(0.007, 0.001, -0.005);

/**
 * Drift of the third noise octave, in noise units per second.
 * This is the finest layer, so it reads as the shimmer on top of the larger
 * shapes rather than as bulk motion.
 */
uniform vec3 noise_velocity3 = vec3(0.0, 0.004, 0.003);

/**
 * Noise value below which there is no fog at all. Raising it toward
 * [code]patch_edge_high[/code] eats away at the patches and leaves more empty
 * space between them.
 */
uniform float patch_edge_low = -0.15;

/**
 * Noise value at which the fog reaches full [code]base_density[/code].
 * The gap to [code]patch_edge_low[/code] is the falloff: widen it for soft,
 * hazy edges, narrow it for sharply cut-out clumps.
 * Pushing it up past the sum of [code]noise_amounts[/code] means the fog never
 * fully solidifies anywhere.
 */
uniform float patch_edge_high = 0.25;

vec3 hash(vec3 p) {
	p = vec3(dot(p, vec3(127.1, 311.7, 74.7)),
			 dot(p, vec3(269.5, 183.3, 246.1)),
			 dot(p, vec3(113.5, 271.9, 124.6)));
	return -1.0 + 2.0 * fract(sin(p) * 43758.5453123);
}

float noise(vec3 p) {
	vec3 i = floor(p);
	vec3 f = fract(p);
	vec3 u = f * f * (3.0 - 2.0 * f);
	return mix(mix(mix(dot(hash(i + vec3(0.0, 0.0, 0.0)), f - vec3(0.0, 0.0, 0.0)),
					   dot(hash(i + vec3(1.0, 0.0, 0.0)), f - vec3(1.0, 0.0, 0.0)), u.x),
				   mix(dot(hash(i + vec3(0.0, 1.0, 0.0)), f - vec3(0.0, 1.0, 0.0)),
					   dot(hash(i + vec3(1.0, 1.0, 0.0)), f - vec3(1.0, 1.0, 0.0)), u.x), u.y),
			   mix(mix(dot(hash(i + vec3(0.0, 0.0, 1.0)), f - vec3(0.0, 0.0, 1.0)),
					   dot(hash(i + vec3(1.0, 0.0, 1.0)), f - vec3(1.0, 0.0, 1.0)), u.x),
				   mix(dot(hash(i + vec3(0.0, 1.0, 1.0)), f - vec3(0.0, 1.0, 1.0)),
					   dot(hash(i + vec3(1.0, 1.0, 1.0)), f - vec3(1.0, 1.0, 1.0)), u.x), u.y), u.z);
}

void fog() {
	vec3 base_pos = WORLD_POSITION * world_scale;
	float n = noise(base_pos * noise_scales.x + TIME * noise_velocity1) * noise_amounts.x
			+ noise(base_pos * noise_scales.y + TIME * noise_velocity2) * noise_amounts.y
			+ noise(base_pos * noise_scales.z + TIME * noise_velocity3) * noise_amounts.z;
	DENSITY = base_density * smoothstep(patch_edge_low, patch_edge_high, n);
	ALBEDO = base_color;
}
