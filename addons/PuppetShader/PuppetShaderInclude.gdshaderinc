const float BlackWhiteCutoff = 0.33;
const float OutlineCutoff = 0.66;
const float AlphaCutoff = 0.5;

const float JiggleScale = 0.001;
const float JiggleRate = 2.0;

const float TexRepeatSize = 20.0;

global uniform sampler2D Puppet_JiggleNoise: repeat_enable;
global uniform vec2 Puppet_LightDirection;
global uniform vec4 Puppet_LightColor;
global uniform vec4 Puppet_ShadowColor;

vec2 ToLayerRegion(int layer)
{
	return vec2(float(layer),0);
}

float colorChoice(vec4 col)
{
	float cutoffCheck = step(BlackWhiteCutoff, col.r) + step(BlackWhiteCutoff, col.g) + step(BlackWhiteCutoff, col.b);
	
	if(abs(cutoffCheck) < 0.001)
		return 0.0;
	if(abs(cutoffCheck - 3.0) < 0.001)
		return 0.8;
		
	float maxCol = col.r;
	float retIndex = 0.2;
	if(col.g > maxCol)
	{
		maxCol = col.g;
		retIndex = 0.4;
	}
	if(col.b > maxCol)
	{
		retIndex = 0.6;
	}
	return retIndex;
}