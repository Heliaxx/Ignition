shader_type spatial;
render_mode unshaded;

uniform float fade_power : hint_range(0.1, 10.0) = 1.0;
uniform float uv_middle_point = 0.5;

uniform sampler2D noise_texture : filter_linear_mipmap, repeat_enable;
uniform vec2 noise_direction = vec2(0.0, 0.5);
uniform vec2 noise_scale = vec2(1.0, 2.0);
uniform float noise_strength : hint_range(0.0, 2.0) = 0.5;

uniform vec4 primary_color : source_color;
uniform float neon_intensity : hint_range(1.0, 15.0) = 3.0;

uniform float fresnel_power : hint_range(0.1, 10.0) = 1.5;
uniform float smoothness : hint_range(0.0, 1.0) = 0.5;
uniform float intensity : hint_range(0.0, 2.0) = 1.0;

void fragment(){
	vec2 noise_uv = (UV * noise_scale) + (noise_direction * TIME);
	float noise_val = texture(noise_texture, noise_uv).r;
	float gradient = (UV.y - uv_middle_point);
	gradient += (noise_val - 0.5) * noise_strength * (1.0 - smoothness);
	float base_alpha = pow(clamp(gradient, 0.0, 1.0), fade_power);
	float fresnel = pow(clamp(dot(NORMAL, VIEW), 0.0, 1.0), fresnel_power);
	float smooth_alpha = mix(base_alpha, smoothstep(0.0, 1.0, base_alpha), smoothness);
	ALPHA = clamp(smooth_alpha * fresnel * intensity, 0.0, 1.0);

	vec3 current_color = primary_color.rgb;
	current_color = mix(primary_color.rgb, vec3(1.0), base_alpha * intensity);
	ALBEDO = current_color * neon_intensity * max(intensity, 0.3);
}
