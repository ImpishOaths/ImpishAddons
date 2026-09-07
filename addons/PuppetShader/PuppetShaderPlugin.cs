#if TOOLS
using Godot;
using System;

[Tool]
public partial class PuppetShaderPlugin : EditorPlugin
{
    private void CheckAddGlobal(StringName name, RenderingServer.GlobalShaderParameterType type, Variant variant)
    {
        if(RenderingServer.GlobalShaderParameterGet(name).VariantType == Variant.Type.Nil)
            RenderingServer.GlobalShaderParameterAdd(
                name,
                type,
                variant);
    }
	public override void _EnterTree()
	{
        CheckAddGlobal(
            "Puppet_JiggleNoise",
            RenderingServer.GlobalShaderParameterType.Sampler2D,
            ResourceLoader.Load<Texture2D>("res://addons/PuppetShader/JiggleNoise.tres"));
        CheckAddGlobal(
            "Puppet_LightColor",
            RenderingServer.GlobalShaderParameterType.Color,
            new Color(0.5f, 0.5f, 0.5f, 1));
        CheckAddGlobal(
            "Puppet_ShadowColor",
            RenderingServer.GlobalShaderParameterType.Color,
            new Color(0.5f, 0.5f, 0.5f, 1));
        CheckAddGlobal(
            "Puppet_LightDirection",
            RenderingServer.GlobalShaderParameterType.Vec2,
            new Vector2(0.5f, -1.0f));
	}
}
#endif
