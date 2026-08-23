shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_disabled, diffuse_lambert;

// Omnidirectional smoke/gas cloud for a space explosion. Driven per-particle by
// COLOR: COLOR.r ramps the glowing core (heat), COLOR.a ramps the dissolve.
// No proximity fade — there is no floor to intersect in open space.

uniform sampler2D cloud_tex : source_color;       // soft cloud mask (density)
uniform sampler2D noise_tex;                       // cellular/marble noise (heat + displacement)
uniform vec3 smoke_color : source_color = vec3(0.09, 0.085, 0.08); // base smoke tint (cool)
uniform float emission_strength = 24.0;
uniform float cloud_speed = 0.15;                  // UV pan speed for the noise
uniform float cloud_intensity = 0.15;             // vertex displacement amount
uniform float cloud_contrast = 1.5;                // boosts the dark mask into visible smoke

// Directional shading so the smoke has a lit and a shadowed side (fake volumetric
// form). Points roughly "down-right" by default; no dependency on scene lights.
uniform vec3 light_dir = vec3(0.4, 0.7, 0.5);
uniform float shade_strength = 0.75;

varying vec3 world_normal;

// Blackbody-ish temperature gradient: 1.0 = white-hot core, cooling through
// yellow and orange to deep red, then to unlit smoke at 0.
vec3 heat_color(float t) {
	vec3 red    = vec3(0.7, 0.06, 0.02);
	vec3 orange = vec3(1.0, 0.35, 0.08);
	vec3 yellow = vec3(1.0, 0.8, 0.3);
	vec3 white  = vec3(1.0, 0.95, 0.85);
	vec3 c = mix(red, orange, smoothstep(0.0, 0.4, t));
	c = mix(c, yellow, smoothstep(0.4, 0.7, t));
	c = mix(c, white, smoothstep(0.7, 1.0, t));
	return c;
}

void vertex() {
	// Panning noise displaces the sphere surface along its normal so the gas
	// cloud roils as it expands.
	vec2 pan_uv = UV + TIME * vec2(cloud_speed, cloud_speed * 0.7);
	float n = texture(noise_tex, pan_uv).r;
	VERTEX += NORMAL * n * cloud_intensity;
	world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
}

void fragment() {
	// The cloud texture is dark, so lift it into a visible density with contrast.
	float density = clamp(texture(cloud_tex, UV).r * cloud_contrast, 0.0, 1.0);

	// Two noise octaves scrolling at different rates so the smoke surface churns
	// and breaks up rather than reading as one flat sprite.
	float cell = texture(noise_tex, UV).r;
	float cell2 = texture(noise_tex, UV * 2.3 - vec2(TIME * 0.05)).r;
	float turbulence = mix(cell, cell2, 0.5);

	// Heat: the particle's RED channel is the base temperature (hot at birth,
	// cooling over life); modulate by noise so the fire is patchy, not uniform.
	float heat = COLOR.r * smoothstep(0.2, 0.9, turbulence);
	vec3 fire = heat_color(clamp(heat, 0.0, 1.0));
	EMISSION = fire * emission_strength * heat * heat; // squared: concentrates the glow in the hottest cells

	// Cool smoke gets simple directional shading for volumetric form. The lit side
	// faces light_dir; the far side falls into shadow.
	float ndl = clamp(dot(normalize(world_normal), normalize(light_dir)) * 0.5 + 0.5, 0.0, 1.0);
	float shade = mix(1.0 - shade_strength, 1.0, ndl);
	ALBEDO = smoke_color * shade;

	// Opacity is the cloud density carried by the particle's ALPHA channel, eroded
	// by turbulence at the edges so it tears apart instead of fading as a clean disc.
	float erode = smoothstep(0.0, 0.6, density * (0.5 + 0.5 * turbulence));
	ALPHA = erode * COLOR.a;
}
