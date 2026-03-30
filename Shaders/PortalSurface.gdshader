shader_type spatial;
render_mode unshaded, cull_disabled, blend_mix;

uniform sampler2D noise_texture;
uniform sampler2D background_texture : source_color;
uniform vec4 layer_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float opacity : hint_range(0.0, 1.0, 0.01) = 1.0;
uniform float brightness : hint_range(0.0, 10.0, 0.1) = 2.5;
uniform float edge_softness : hint_range(0.001, 0.5, 0.001) = 0.02;
uniform float inner_ring : hint_range(0.0, 0.6, 0.01) = 0.4;
uniform float edge_width : hint_range(0.0, 0.5, 0.01) = 0.35;

group_uniforms depth;
uniform int layers : hint_range(1, 64, 1) = 12;
uniform float depth_amount = 0.8;
uniform float fade_amount : hint_range(0.0, 16.0, 0.01) = 1.5;

vec2 to_polar(vec2 uv) {
	uv = uv * 2.0 - 1.0;
	float radius = length(uv);
	float angle = atan(uv.x, uv.y);
	return vec2(angle / PI, radius) / 2.0;
}

float overlay(float a, float b) {
	float limit = step(0.5, a);
	return mix(2.0 * a * b, 1.0 - 2.0 * (1.0 - a) * (1.0 - b), limit);
}

vec2 parallax(float depth, vec3 n, vec3 t, vec3 v) {
	vec3 normal = normalize(n);
	vec3 tangent = normalize(t);
	vec3 bitangent = cross(normal, tangent);
	vec3 view = normalize(v);
	vec3 view_tangent = vec3(dot(view, tangent), dot(view, bitangent), dot(view, normal));
	vec2 offset = (view_tangent.xy / max(view_tangent.z, 0.001)) * depth;
	offset = vec2(-offset.x, offset.y);
	return offset;
}

void fragment() {
	float dist = length(UV - vec2(0.5));
	float alpha_mask = 1.0 - smoothstep(0.5 - edge_softness, 0.5, dist);

	float edge_mask = smoothstep(0.5 - edge_width, 0.5, dist);

	float value = 0.0;
	for (int i = 0; i < layers; i++) {
		float t = float(i) / float(layers);
		float depth = t * depth_amount;

		vec2 poffset = parallax(depth, NORMAL, TANGENT, VIEW);

		vec2 circle_uv = UV + poffset;
		float circle = length(circle_uv * 2.0 - 1.0);
		circle = clamp((circle - inner_ring) / (1.0 - inner_ring), 0.0, 1.0);

		vec2 noise_uv = to_polar(UV + poffset);
		noise_uv.y += TIME * 0.1;
		noise_uv += t;
		float noise = texture(noise_texture, noise_uv).r;

		float layer_value = step(0.5, overlay(circle, noise));
		layer_value *= pow(1.0 - t, fade_amount);
		layer_value *= edge_mask;

		value = max(value, layer_value);
	}

	ivec2 bg_size = textureSize(background_texture, 0);
	float bg_aspect = float(bg_size.x) / float(bg_size.y);
	float scale_x = min(1.0, 1.0 / bg_aspect);
	float scale_y = min(1.0, bg_aspect);
	vec2 bg_uv = (UV - vec2(0.5)) * vec2(scale_x, scale_y) + vec2(0.5);

	vec3 bg = texture(background_texture, bg_uv).rgb;
	ALBEDO = mix(bg, layer_color.rgb, value);
	EMISSION = layer_color.rgb * value * brightness;
	ALPHA = alpha_mask * opacity;
}
