shader_type sky;

uniform sampler2D sky_tex: source_color, filter_linear, hint_default_black;
uniform vec3 sun_pos =  vec3(0.0);

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
		COLOR = vec3(0.2, 0.6, 1.0);
	} else {
		COLOR = texture(sky_tex, SKY_COORDS).rgb;
		float ds = max(dot(normalize(sun_pos - POSITION), EYEDIR), 0.0);
		COLOR += pow(vec3(pow(ds, 150.0), pow(ds, 30.0), pow(ds, 20.0)), vec3(2.0));
	}
}
