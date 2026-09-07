shader_type canvas_item;

uniform vec4 colA: source_color;
uniform vec4 colB: source_color;
uniform float rate;

void fragment()
{
	COLOR = mix(colA, colB, sin(TIME*rate));
}
