shader_type canvas_item;

void fragment()
{
	vec4 modulate = COLOR;
	modulate.a = 1.0;
	COLOR = texture(TEXTURE, UV) * modulate;
}
