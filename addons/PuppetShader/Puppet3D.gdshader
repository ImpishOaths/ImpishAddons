shader_type spatial;
stencil_mode write, compare_always, 1;
render_mode unshaded, cull_disabled;

#include "res://addons/PuppetShader/PuppetShaderInclude.gdshaderinc"

group_uniforms Tex;
uniform sampler2D ViewportTex: filter_nearest, source_color, repeat_disable;
uniform vec2 RegionSize = vec2(0.333, 1.0);
uniform vec2 EdgeEpsilon = vec2(0.001);

group_uniforms Effects;
uniform vec4 FadeColor: source_color = vec4(0.0);
uniform float FadeEdge;

group_uniforms Depth;
uniform sampler2D Gradient: filter_nearest, source_color, repeat_disable;
uniform vec4 DepthColor: source_color = vec4(0.0);
uniform int DepthLayerCount = 2;

varying flat float ScaledDepth;
instance uniform int Layer = 1;
instance uniform float Depth = 0.02;

void vertex()
{
    ScaledDepth = Depth * length(MODEL_MATRIX[2].xyz)/2.0;
}

vec2 getDuv(mat4 viewMatrix, mat4 modelMatrix, vec3 view)
{
	mat4 mv = viewMatrix * modelMatrix;
    vec3 Pu = (mv * vec4(1.0/RegionSize.x, 0.0, 0.0, 0.0)).xyz;
    vec3 Pv = (mv * vec4(0.0, -1.0/RegionSize.y, 0.0, 0.0)).xyz;
	vec3 n = normalize(cross(Pu, Pv));
    if (dot(n, view) < 0.0) n = -n;
	vec3 V = -view;
    float t = ScaledDepth / max(dot(n, view), 0.05);
    vec3 w = V * t + n * ScaledDepth;
    float denom = dot(cross(Pu, Pv), n);
    vec2 duv = vec2(dot(cross(w, Pv), n), dot(cross(Pu, w), n)) / denom;
	return duv;
}

void fragment() {
	vec4 region = vec4(RegionSize*ToLayerRegion(Layer), RegionSize);
	vec2 uv = (UV*region.zw)+region.xy;
	if(any(lessThan(uv, region.xy+EdgeEpsilon)) || any(greaterThanEqual(uv, region.xy+region.zw-EdgeEpsilon)))
	{
		discard;
	}
	
	vec4 baseCol = texture(ViewportTex, uv);
	if(baseCol.a > 0.0)
	{
		vec4 effectCol = FadeColor;
		float fadeStrength = clamp(1.0-(1.0-UV.y)/FadeEdge, 0.0, 1.0);
		effectCol.a *= pow(fadeStrength,2.0);
		ALBEDO = mix(baseCol.rgb, effectCol.rgb, effectCol.a);
	}
	else
	{
		if(DepthLayerCount == 0)
			discard;
		vec2 duv = getDuv(VIEW_MATRIX, MODEL_MATRIX, VIEW);
		float maxA = 0.0;
		if(any(lessThan(uv+duv, region.xy+EdgeEpsilon)) || any(greaterThan(uv+duv, region.xy+region.zw-EdgeEpsilon)))
			discard;
		float depthStep = 1.0/float(DepthLayerCount);
		for(int i = 1; i < DepthLayerCount+1; ++i)
		{
			vec2 checkUV = uv + duv*float(i)*depthStep;
			maxA = max(maxA, texture(ViewportTex, checkUV).a);
		}
		if(maxA < 1.0)
		{
			discard;
		}
		vec4 edgeColor = texture(Gradient, vec2(0.0));
		ALBEDO = mix(edgeColor.rgb, DepthColor.rgb, DepthColor.a);
	}
	
}