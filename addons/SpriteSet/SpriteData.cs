using Godot;

namespace SpriteSet;

[Tool]
[GlobalClass]
public partial class SpriteData : Resource
{
    [Export]
    public Vector2I TileCoord;
    [Export]
    public Vector2I TileSize;
    [Export]
    public int AlternateIndex;
    [Export]
    public int TextureIndex;

    [Export]
    public StringName Group;
    [Export]
    public StringName Name;
}
