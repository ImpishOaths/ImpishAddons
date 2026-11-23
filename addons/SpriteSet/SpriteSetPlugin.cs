using Godot;

namespace SpriteSet;

[Tool]
public partial class SpriteSetPlugin : EditorPlugin
{
    public override void _SaveExternalData()
	{
		SpriteSet.RegisterSets();
	}
}
