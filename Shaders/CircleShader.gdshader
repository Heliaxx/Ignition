shader_type spatial;
render_mode unshaded, cull_disabled, blend_mix;

uniform vec4 color : source_color = vec4(0.2, 0.8, 1.0, 0.5);
uniform float edge_softness : hint_range(0.0, 0.5) = 0.02;

void fragment() {
	float dist = length(UV - vec2(0.5));
	float alpha = 1.0 - smoothstep(0.5 - edge_softness, 0.5, dist);
	ALBEDO = color.rgb;
	ALPHA = color.a * alpha;
	EMISSION = color.rgb * color.a;
}
