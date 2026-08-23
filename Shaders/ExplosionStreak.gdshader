shader_type spatial;
render_mode blend_add, cull_disabled, unshaded, depth_draw_never, fog_disabled;

// Shrapnel streaks shot outward in all directions. The quad
// is aligned to its velocity (Align Y in the particle flags); this shader only
// picks one of four atlas frames and fades it over life.

uniform sampler2D streak_atlas : source_color;    // 4 frames laid out horizontally
uniform vec3 streak_color : source_color = vec3(1.0, 0.6, 0.2);
uniform float emission_strength = 8.0;

// INSTANCE_CUSTOM is written per-particle by the process material:
//   .x = hue variation (our random frame selector, since Hue Variation = 1.0)
//   .y = animation offset   .z = animation frame   .w = lifetime [0..1]
varying vec4 inst_custom;

void vertex() {
	inst_custom = INSTANCE_CUSTOM;
}

void fragment() {
	// Pick one of 4 horizontal frames from the per-particle random hue value,
	// quantized to atlas offsets 0 / 0.25 / 0.5 / 0.75.
	float frame = floor(clamp(inst_custom.x, 0.0, 0.999) * 4.0);
	vec2 uv = vec2(UV.x * 0.25 + frame * 0.25, UV.y);

	vec4 tex = texture(streak_atlas, uv);
	float lum = max(tex.r, max(tex.g, tex.b));

	// Fade the streak in then out over life via the particle ALPHA channel.
	float a = smoothstep(0.0, 0.2, lum) * COLOR.a;

	ALBEDO = streak_color;
	EMISSION = streak_color * emission_strength * lum;
	ALPHA = a;
}
