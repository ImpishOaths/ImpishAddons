shader_type canvas_item;
render_mode unshaded;

#include "res://addons/PuppetShader/PuppetShaderInclude.gdshaderinc"

global uniform bool Mode2D;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;

uniform vec2 RegionSize = vec2(128,128);
instance uniform int Layer = 2;

varying vec2 canvas_scale;

void vertex()
{
	canvas_scale = vec2(
        length(CANVAS_MATRIX[0]),
        length(CANVAS_MATRIX[1])
    );
	if(!Mode2D)
	{
		VERTEX += RegionSize*ToLayerRegion(Layer);
	}
}

void fragment() {
	vec2 uv = SCREEN_UV;
	if(!Mode2D)
	{
		uv -= SCREEN_PIXEL_SIZE * canvas_scale * RegionSize * ToLayerRegion(Layer);
	}
	
	vec4 c = texture(screen_texture, uv);
	if (c.a > 0.0001)
	{
		c.rgb /= c.a;
	}
	
	COLOR *= c;
}