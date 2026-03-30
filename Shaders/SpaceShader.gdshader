shader_type sky;

group_uniforms Sky;
uniform sampler2D sky_tex: source_color, filter_linear, hint_default_black;

group_uniforms Star;
uniform bool star_enabled = true;
uniform vec3 star_pos = vec3(0.0);
uniform vec3 star_color: source_color = vec3(1.0, 0.62, 0.28);
uniform vec3 star_core_color: source_color = vec3(1.0, 0.95, 0.86);
uniform float star_disc_size: hint_range(0.0005, 0.08, 0.0001) = 0.008;
uniform float star_disc_softness: hint_range(0.05, 2.0, 0.01) = 0.45;
uniform float star_halo_strength: hint_range(0.0, 8.0, 0.01) = 1.4;
uniform float star_halo_falloff: hint_range(2.0, 256.0, 0.1) = 18.0;
uniform float star_glare_strength: hint_range(0.0, 4.0, 0.01) = 0.22;

group_uniforms Planet;
uniform bool planet_enabled = true;
uniform sampler2D planet_albedo_tex: source_color, filter_linear, hint_default_black;
uniform sampler2D planet_night_tex: source_color, filter_linear, hint_default_black;
uniform vec3 planet_direction = vec3(-0.55, 0.12, -0.82);
uniform float planet_distance = 24.0;
uniform float planet_radius = 6.2;
uniform float planet_rotation = 0.0;
uniform vec3 planet_tint: source_color = vec3(1.0);
uniform float planet_night_strength: hint_range(0.0, 4.0, 0.01) = 0.75;

group_uniforms Planet.Atmosphere;
uniform vec3 atmosphere_color: source_color = vec3(0.24, 0.48, 1.0);
uniform float atmosphere_strength: hint_range(0.0, 4.0, 0.01) = 0.9;
uniform float atmosphere_falloff: hint_range(0.5, 16.0, 0.01) = 5.2;

group_uniforms Rings;
uniform bool ring_enabled = false;
uniform sampler2D ring_texture: filter_linear, hint_default_white;
uniform vec3 ring_normal = vec3(0.0, 1.0, 0.0);
uniform float ring_inner_radius: hint_range(0.1, 20.0, 0.1) = 7.0;
uniform float ring_outer_radius: hint_range(0.1, 30.0, 0.1) = 12.0;
uniform vec3 ring_color: source_color = vec3(0.7, 0.65, 0.55);
uniform float ring_opacity: hint_range(0.0, 1.0, 0.01) = 0.6;

struct RayHit {
	bool is_hit;
	vec3 normal;
	vec3 hit_point;
};

RayHit hit_sphere(vec3 ray_origin, vec3 ray_dir, vec3 sphere_center, float sphere_radius) {
	RayHit ray_hit;
	ray_hit.is_hit = false;
	vec3 oc = ray_origin - sphere_center;
	float a =  dot(ray_dir, ray_dir);
	float b = 2.0 * dot(oc, ray_dir);
	float c = dot (oc, oc) - sphere_radius * sphere_radius;
	float delta = b * b - 4.0 * a * c;
	if (delta < 0.0) {
		return ray_hit;
	}
	float root = sqrt(delta);
	float t1 = (-b - root) / (2.0 * a);
	float t2 = (-b + root) / (2.0 * a);
	float t_near = min(t1, t2);
	float t_far = max(t1, t2);
	if (t_near < 0.0) {
		if (t_far >= 0.0) {
			t_near = 0.0;
		} else {
			return ray_hit;
		}
	}
	ray_hit.hit_point = fma(ray_dir, vec3(t_near), ray_origin);
	ray_hit.normal = normalize(ray_hit.hit_point - sphere_center);
	ray_hit.is_hit = true;
	return ray_hit;
}

// Ring intersection: returns hit info for a flat disc/ring around a center point
struct RingHit {
	bool is_hit;
	vec3 hit_point;
	float ring_u; // 0-1 from inner to outer radius
};

RingHit hit_ring(vec3 ray_origin, vec3 ray_dir, vec3 ring_center, vec3 ring_normal_dir, float inner_r, float outer_r) {
	RingHit ring_hit;
	ring_hit.is_hit = false;

	// Ray-plane intersection
	float denom = dot(ring_normal_dir, ray_dir);
	if (abs(denom) < 0.0001) {
		return ring_hit; // Ray parallel to ring plane
	}

	float t = dot(ring_center - ray_origin, ring_normal_dir) / denom;
	if (t < 0.0) {
		return ring_hit; // Ring behind camera
	}

	vec3 hit_point = ray_origin + ray_dir * t;
	float dist = length(hit_point - ring_center);

	if (dist >= inner_r && dist <= outer_r) {
		ring_hit.is_hit = true;
		ring_hit.hit_point = hit_point;
		ring_hit.ring_u = (dist - inner_r) / (outer_r - inner_r);
	}
	return ring_hit;
}

vec2 get_spherical_uv(vec3 point_on_sphere, float rotation) {
vec3 n = normalize(point_on_sphere);
float theta = atan(n.z, n.x);
float phi = acos(n.y);
theta += rotation;
float u = (theta + PI) / TAU;
float v = phi / PI;
return vec2(u, v);
}

vec3 compute_Lighting(vec3 normal, vec3 light_dir) {
	return vec3(max(dot(normal, -light_dir), 0.0));
}

void sky() {
	if(AT_CUBEMAP_PASS) {
		COLOR = texture(sky_tex, SKY_COORDS).rgb;
	} else {
		COLOR = texture(sky_tex, SKY_COORDS).rgb;
		vec3 light_dir = normalize(star_pos);
		bool hit_planet = false;
		float planet_t = 1e10; // Distance to planet hit
		vec3 planet_color_result = vec3(0.0);

		if (planet_enabled) {
			vec3 planet_dir = normalize(planet_direction);
			vec3 planet_center = planet_dir * planet_distance;
			RayHit planet_hit = hit_sphere(vec3(0.0), normalize(EYEDIR), planet_center, planet_radius);
			if (planet_hit.is_hit) {
				hit_planet = true;
				planet_t = length(planet_hit.hit_point);
				vec2 planet_uv = get_spherical_uv(planet_hit.normal, planet_rotation);
				vec3 albedo = texture(planet_albedo_tex, planet_uv).rgb * planet_tint;
				float ndotl = max(dot(planet_hit.normal, -light_dir), 0.0);
				vec3 day_color = albedo * (0.07 + ndotl * 1.25);
				vec3 night_mask = texture(planet_night_tex, planet_uv).rgb;
				vec3 night_color = night_mask * (1.0 - ndotl) * planet_night_strength;
				float rim = pow(1.0 - max(dot(planet_hit.normal, -EYEDIR), 0.0), atmosphere_falloff);
				vec3 atmosphere = atmosphere_color * rim * atmosphere_strength;
				planet_color_result = day_color + night_color + atmosphere;
			}
		}

		// Ring rendering
		bool hit_ring_result = false;
		float ring_t = 1e10;
		vec3 ring_color_result = vec3(0.0);
		float ring_alpha = 0.0;

		if (ring_enabled && planet_enabled) {
			vec3 planet_dir = normalize(planet_direction);
			vec3 planet_center = planet_dir * planet_distance;
			vec3 norm_ring_normal = normalize(ring_normal);

			RingHit r_hit = hit_ring(vec3(0.0), normalize(EYEDIR), planet_center, norm_ring_normal, ring_inner_radius, ring_outer_radius);
			if (r_hit.is_hit) {
				hit_ring_result = true;
				ring_t = length(r_hit.hit_point);

				// Sample ring texture (u = radial position 0-1)
				vec2 ring_uv = vec2(r_hit.ring_u, 0.5);
				vec4 ring_tex = texture(ring_texture, ring_uv);

				// Apply simple lighting
				float ring_ndotl = abs(dot(norm_ring_normal, -light_dir)) * 0.6 + 0.4;
				ring_color_result = ring_color * ring_tex.rgb * ring_ndotl;
				ring_alpha = ring_opacity * ring_tex.a;

				// Check if ring is in planet's shadow (simple check)
				// If the ring point is on the opposite side of the planet from the sun, darken it
				vec3 ring_to_planet = planet_center - r_hit.hit_point;
				float shadow_check = dot(normalize(ring_to_planet), -light_dir);
				if (shadow_check > 0.0 && length(ring_to_planet) < planet_radius * 1.1) {
					ring_color_result *= 0.3; // In shadow
				}
			}
		}

		// Composite: planet and ring with depth ordering
		if (hit_planet && hit_ring_result) {
			if (ring_t < planet_t) {
				// Ring in front of planet
				COLOR = mix(planet_color_result, ring_color_result, ring_alpha);
			} else {
				// Ring behind planet - planet fully occludes
				COLOR = planet_color_result;
			}
		} else if (hit_planet) {
			COLOR = planet_color_result;
		} else if (hit_ring_result) {
			// Ring visible, blend with sky
			COLOR = mix(COLOR, ring_color_result, ring_alpha);
		}

		if (star_enabled && !hit_planet) {
			vec3 view_dir = normalize(EYEDIR);
			float to_sun = max(dot(view_dir, -light_dir), 0.0);
			float disc_edge = 1.0 - star_disc_size;
			float disc = smoothstep(disc_edge, min(1.0, disc_edge + star_disc_size * max(star_disc_softness, 0.001)), to_sun);
			float core = pow(disc, 2.2);
			float halo = pow(to_sun, star_halo_falloff) * star_halo_strength;
			float glare = pow(to_sun, max(2.0, star_halo_falloff * 0.35)) * star_glare_strength;
			vec3 star_render = halo * star_color + core * star_core_color + glare * star_color;
			COLOR += star_render;
		}
	}
}
