shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_lambert, specular_schlick_ggx;

// Color used for albedo and emissive glow.
uniform vec4 base_color : source_color = vec4(1.0, 0.2, 0.050980, 1.0);

uniform float inflate_clamp_min = 300.0; // distance clamp lower bound (also the divisor)
uniform float inflate_clamp_max = 1000.0; // distance clamp upper bound (growth freezes here)
uniform float inflate_power = 2.0; // exponent applied to the clamped ratio

uniform float near_power = 2.0;
uniform float near_clamp_max = 1000.0;
uniform float far_fade_distance = 2500.0;
uniform float far_gain = 1.5;
// Overall emission multiplier (original baked-in 0.4).
uniform float emission_strength = 0.4;

// Near-distance dimming
uniform float near_dim_floor : hint_range(0.0, 1.0) = 0.5;
uniform float near_dim_distance = 250.0;

void vertex() {
	float dist = length(NODE_POSITION_WORLD - CAMERA_POSITION_WORLD);
	float clamped = clamp(dist, inflate_clamp_min, inflate_clamp_max);
	float inflate = pow(clamped / inflate_clamp_min, inflate_power);
	VERTEX = VERTEX + NORMAL * inflate;
}

void fragment() {
	float dist = length(NODE_POSITION_WORLD - CAMERA_POSITION_WORLD);

	float near = clamp(pow(dist, near_power), 0.0, near_clamp_max);
	float far = clamp(1.0 - dist / far_fade_distance, 0.0, 1.0) * far_gain;
	float near_dim = mix(near_dim_floor, 1.0, smoothstep(0.0, near_dim_distance, dist));
	float brightness = near * far * emission_strength * near_dim;

	ALBEDO = base_color.rgb;
	METALLIC = 0.0;
	ROUGHNESS = 1.0;
	EMISSION = base_color.rgb * brightness;
}
