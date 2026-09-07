shader_type canvas_item;

const float BlackWhiteCutoff = 0.33;
const float AlphaCutoff = 0.66;

const float JiggleScale = 0.0005;
const float JiggleRate = 1.5;

const float TexRepeatSize = 100.0;

const float LightScale = 0.005;

global uniform sampler2D Puppet_JiggleNoise: repeat_enable;
global uniform vec2 Puppet_LightDirection;
global uniform vec4 Puppet_LightColor;
global uniform vec4 Puppet_ShadowColor;

uniform sampler2D Gradient: filter_nearest, source_color, repeat_disable;
uniform bool DoShading = true;
uniform bool FlipShading = false;

varying vec2 true_normed_light_direction;
varying mat2 screen_down_direction;

void vertex()
{
	true_normed_light_direction = normalize((vec4(Puppet_LightDirection, 0.0, 1.0) * MODEL_MATRIX).xy);
	//screen_down_direction = inverse(MODEL_MATRIX)*mat2(vec2(1.0,0.0), vec2(0.0, 1.0));
}

vec2 chooseMaxX(vec2 a, vec2 b)
{
	return mix(a, b, step(a.x, b.x));
}

vec2 colorChoice(vec4 col)
{
	vec2 choice1 = chooseMaxX(vec2(col.r, 0.2), vec2(col.g, 0.4));
	vec2 choice2 = chooseMaxX(vec2(col.b, 0.6), choice1);
	float choice = choice2.y;
	float cutoffCheck = step(BlackWhiteCutoff, col.r) + step(BlackWhiteCutoff, col.g) + step(BlackWhiteCutoff, col.b);
	float isBlack = step(cutoffCheck, 0.001);
	float isWhite = step(3.0 - cutoffCheck, 0.001);
	choice = mix(choice, 0.0, isBlack);
	choice = mix(choice, 0.8, isWhite);
	return vec2(choice, col.a);
}

void fragment()
{
	vec2 timeOffset = fract(vec2(floor(TIME*JiggleRate)*PI));
	vec2 jiggleDir = texture(Puppet_JiggleNoise, SCREEN_UV+timeOffset).rg*2.0-1.0;
	vec2 jiggleUV = UV + jiggleDir*JiggleScale;
	
	vec2 lightDir = true_normed_light_direction*LightScale*float(DoShading);
	lightDir = mix(lightDir, lightDir*vec2(-1.0, 1.0), float(FlipShading));
	vec2 lightNorm = normalize(lightDir)*TEXTURE_PIXEL_SIZE;
	
	vec4 colCenter = texture(TEXTURE, jiggleUV + mat2(vec2(0,0),vec2(0,0))*lightNorm);
	vec4 colRight = texture(TEXTURE, jiggleUV + mat2(vec2(1,0),vec2(0,1))*lightNorm);
	vec4 colRight2 = texture(TEXTURE, jiggleUV + mat2(vec2(2,0),vec2(0,2))*lightNorm);
	vec4 colLeft = texture(TEXTURE, jiggleUV + mat2(vec2(-1,0),vec2(0,-1))*lightNorm);
	vec4 colLeft2 = texture(TEXTURE, jiggleUV + mat2(vec2(-2,0),vec2(0,-2))*lightNorm);
	vec4 colUp = texture(TEXTURE, jiggleUV + mat2(vec2(0,1),vec2(-1,0))*lightNorm);
	vec4 colUpRight = texture(TEXTURE, jiggleUV + mat2(vec2(1,1),vec2(-1,1))*lightNorm);
	vec4 colUpLeft = texture(TEXTURE, jiggleUV + mat2(vec2(-1,1),vec2(-1,-1))*lightNorm);
	vec4 colDown = texture(TEXTURE, jiggleUV + mat2(vec2(0,-1),vec2(1,0))*lightNorm);
	vec4 colDownRight = texture(TEXTURE, jiggleUV + mat2(vec2(1,-1),vec2(1,1))*lightNorm);
	vec4 colDownLeft = texture(TEXTURE, jiggleUV + mat2(vec2(-1,-1),vec2(1,-1))*lightNorm);
	
	vec4 baseCol = (colCenter*2.0+colRight+colLeft+colUp+colDown)/6.0;
	//vec4 baseCol = colCenter;
	
	vec2 choice = colorChoice(baseCol);
	
	vec2 scaledScreenUV = fract(SCREEN_UV / SCREEN_PIXEL_SIZE / TexRepeatSize);
	vec2 gradientUV = vec2(choice.x + (scaledScreenUV.x/5.0), scaledScreenUV.y);
	vec4 color = texture(Gradient, gradientUV);
	
	vec4 lightCol = (colRight*2.0+colUpRight+colCenter+colDownRight+colRight2)/6.0;
	//vec4 lightCol = colRight;
	vec2 lightChoice = colorChoice(lightCol);
	float isLit = step(lightChoice.x, 0.001)*step(AlphaCutoff, lightChoice.y)*step(0.001, choice.x);
	
	vec4 shadowCol = (colLeft*2.0+colUpLeft+colCenter+colDownLeft+colLeft2)/6.0;
	//vec4 shadowCol = colLeft;
	vec2 shadowChoice = colorChoice(shadowCol);
	float isShadow = step(shadowChoice.x, 0.001)*step(AlphaCutoff, shadowChoice.y)*step(0.001, choice.x);
	
	color *= mix(vec4(1.0), Puppet_ShadowColor, isShadow);
	color += Puppet_LightColor*isLit;
	color = clamp(color, 0.0, 1.0);
	
	COLOR = vec4(color.rgb, step(AlphaCutoff, choice.y));
}