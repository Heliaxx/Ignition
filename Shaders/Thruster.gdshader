shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never, fog_disabled;

uniform float intensity : hint_range(0.0, 2.0) = 0.8;
uniform vec4 core_color : source_color = vec4(1.0, 0.95, 0.8, 1.0);
uniform vec4 outer_color : source_color = vec4(1.0, 0.6, 0.1, 1.0);
uniform float noise_scale : hint_range(1.0, 20.0) = 6.0;
uniform float noise_speed : hint_range(0.5, 10.0) = 3.0;
uniform float flame_sharpness : hint_range(0.5, 5.0) = 2.0;
uniform float flicker_strength : hint_range(0.0, 1.0) = 0.3;

// Simple hash-based noise
float hash(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f); // smoothstep

	float a = hash(i);
	float b = hash(i + vec2(1.0, 0.0));
	float c = hash(i + vec2(0.0, 1.0));
	float d = hash(i + vec2(1.0, 1.0));

	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float fbm(vec2 p) {
	float value = 0.0;
	float amplitude = 0.5;
	for (int i = 0; i < 4; i++) {
		value += amplitude * noise(p);
		p *= 2.0;
		amplitude *= 0.5;
	}
	return value;
}

void fragment() {
	// UV.y = 0 at base (nozzle), 1 at tip
	// UV.x wraps around the cone (0..1)
	float dist_along = UV.y;
	float angle = UV.x * TAU; // wrap UV.x around the cone

	// Radial coordinate (distance from center axis)
	// On a cone mesh, all fragments at the same UV.y height have the same radial distance
	// We use the angle to create variation around the circumference
	vec2 polar = vec2(cos(angle), sin(angle));

	// Animated noise for flame distortion
	float time = TIME * noise_speed;
	vec2 noise_uv = vec2(angle * noise_scale / TAU, dist_along * noise_scale) + vec2(0.0, -time);
	float n = fbm(noise_uv);

	// Secondary noise layer for flicker
	float flicker = fbm(vec2(angle * 2.0, time * 2.0)) * flicker_strength;

	// Flame shape: wider at base, narrower at tip
	// The cone mesh geometry already handles the shape, so we focus on alpha
	float base_alpha = 1.0 - dist_along; // linear fade toward tip
	base_alpha = pow(base_alpha, flame_sharpness); // sharpen the falloff

	// Noise-based edge erosion (more erosion toward the tip)
	float erosion = mix(0.1, 0.8, dist_along); // more erosion at tip
	float flame_mask = smoothstep(erosion, erosion + 0.3, n + (1.0 - dist_along) * 0.5);

	// Combine alpha
	float alpha = base_alpha * flame_mask * (1.0 - flicker * dist_along);
	alpha *= intensity;

	// Color gradient: core (white-hot) at base, outer (orange) at tip
	float color_t = pow(dist_along, 0.7); // bias toward core color near base
	vec3 flame_color = mix(core_color.rgb, outer_color.rgb, color_t);

	// Brighten the core (center has more energy)
	float core_boost = (1.0 - dist_along) * 2.0;
	flame_color *= (1.0 + core_boost);

	// Output
	ALBEDO = flame_color * alpha;
	ALPHA = clamp(alpha, 0.0, 1.0);
}
