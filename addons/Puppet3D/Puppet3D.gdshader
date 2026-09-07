shader_type spatial;
render_mode cull_disabled, unshaded;

uniform sampler2D albedo_texture : source_color, filter_nearest;
uniform vec4 region = vec4(0, 0, 128, 128);
uniform float scale = 0.25;
uniform float layer_gap : hint_range(0.0, 10.0) = 4.0;
uniform vec4 back_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform int layer_count : hint_range(1, 8) = 3;

const int MAX_LAYERS = 8; // bump this (and layer_count's hint_range max) if you need more

varying vec3 view_dir_local;

void vertex() {
	vec3 cam_local = (inverse(MODELVIEW_MATRIX) * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
	view_dir_local = cam_local - VERTEX;
}

void fragment() {
	vec2 render_size = vec2(textureSize(albedo_texture,0))*scale;
	vec4 relativeRegion = region/vec4(render_size,render_size);
	vec2 uv = UV*relativeRegion.zw+relativeRegion.xy;

	float vz = sign(view_dir_local.z) * max(abs(view_dir_local.z), 0.05);
	// This is now the offset at the FARTHEST layer (layer_gap's full distance),
	// not a per-layer step — individual layers interpolate a fraction of this below.
	vec2 max_offset = (view_dir_local.xy / vz) * layer_gap / render_size;

	max_offset.y = -max_offset.y;
	
	bool mirrored = determinant(mat3(MODEL_MATRIX)) < 0.0;
	bool true_front_facing = FRONT_FACING != mirrored;
	if(!true_front_facing)
		max_offset = -max_offset;

	vec3 color = vec3(0.0);
	float alpha = 0.0;

	// Composite back-to-front: farthest layer first, real sprite (i == 0) last/on top
	for (int i = MAX_LAYERS - 1; i >= 0; i--) {
		if (i >= layer_count) {
			continue;
		}

		// t = 0 at the front sprite, t = 1 at the farthest layer, evenly spaced between.
		float t = (layer_count > 1) ? float(i) / float(layer_count - 1) : 0.0;
		vec2 layer_offset = max_offset * t;

		vec2 layer_uv = uv - layer_offset;
		if (layer_uv.x < relativeRegion.x || layer_uv.x > 1.0-relativeRegion.z || layer_uv.y < relativeRegion.y || layer_uv.y > 1.0-relativeRegion.w) {
			continue;
		}

		vec4 tex = texture(albedo_texture, layer_uv);
		vec3 layer_rgb;
		float layer_a;

		if (i == 0) {
			layer_rgb = tex.rgb;   // frontmost layer: real sprite colors
			layer_a = tex.a;
		} else {
			layer_rgb = back_color.rgb; // every layer behind it: flat tint
			layer_a = tex.a * back_color.a;
		}

		color = mix(color, layer_rgb, layer_a);
		alpha = layer_a + alpha * (1.0 - layer_a);
	}
	
	ALBEDO = color;
	ALPHA = alpha;
	ALPHA_SCISSOR_THRESHOLD = 0.5;
}
