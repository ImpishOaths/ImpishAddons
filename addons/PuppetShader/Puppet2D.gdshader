shader_type canvas_item;
render_mode unshaded, world_vertex_coords;

#include "res://addons/PuppetShader/PuppetShaderInclude.gdshaderinc"

uniform sampler2D Gradient: filter_nearest, source_color, repeat_disable;

varying flat vec2 local_light_dir;
varying flat vec2 region_offset;
varying vec2 world_pos;
varying flat vec4 modulate;

global uniform bool Mode2D;
uniform vec2 RegionSize = vec2(128,128);

instance uniform int Layer = 0;

void vertex()
{
	modulate = COLOR;
	region_offset = Mode2D ? vec2(0) : RegionSize * ToLayerRegion(Layer);
	VERTEX += region_offset;
	
	world_pos = VERTEX;

	mat2 world_to_local = inverse(mat2(MODEL_MATRIX[0].xy, MODEL_MATRIX[1].xy));
	local_light_dir = world_to_local * Puppet_LightDirection;
}

void fragment()
{
	vec2 p = world_pos - region_offset;
	if (any(lessThan(p, vec2(0.0))) || any(greaterThanEqual(p, RegionSize)))
	{
		discard;
	}
	vec2 timeOffset = fract(vec2(floor(TIME*JiggleRate)*PI));
	vec2 jiggleDir = texture(Puppet_JiggleNoise, SCREEN_UV/4.0+timeOffset).rg*2.0-1.0;
	vec2 jiggleUV = UV + jiggleDir*JiggleScale;
	
	vec4 baseCol = texture(TEXTURE, jiggleUV);
	if(baseCol.a < AlphaCutoff)
		discard;
		
	float choice = colorChoice(baseCol);
	vec2 scaledScreenUV = fract(SCREEN_UV / SCREEN_PIXEL_SIZE / TexRepeatSize);
	vec2 gradientUV = vec2(choice + (scaledScreenUV.x/5.0), scaledScreenUV.y);
	vec4 color = texture(Gradient, gradientUV);
	COLOR = color * modulate;
	if(choice > 0.0)
	{
		vec2 lightDir = local_light_dir*TEXTURE_PIXEL_SIZE;
		vec4 lightCol = texture(TEXTURE, jiggleUV + lightDir);
		vec4 shadowCol = texture(TEXTURE, jiggleUV - lightDir);
		if(shadowCol.a > AlphaCutoff)
		{
			if(abs(colorChoice(shadowCol)) < 0.0001)
				color *= Puppet_ShadowColor;
		}
		
		if(lightCol.a > AlphaCutoff)
		{
			if(abs(colorChoice(lightCol)) < 0.0001)
				color += Puppet_LightColor;
		}
		
		color = clamp(color, 0.0, 1.0);
		
		COLOR = color * modulate;
	}
}