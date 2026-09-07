using System.Collections.Generic;
using System.Linq;
using Godot;

namespace SpriteSet;

[Tool]
[GlobalClass]
public partial class SpriteSet : TileSet
{
    private readonly static HashSet<SpriteSet> SetsToRegister = [];
    public static void RegisterSets()
    {
        foreach(var set in SetsToRegister)
        {
            set.RegisterSprites();
            GD.Print($"Saved {set.ResourcePath}");
        }
        SetsToRegister.Clear();
    }

    [Export]
    private Godot.Collections.Dictionary<StringName, Godot.Collections.Dictionary<StringName, SpriteData>> Sprites = [];
    [Export]
    private Godot.Collections.Dictionary<int, Texture2D> Textures = [];
    [Export]
    public Material DefaultMaterial;

    public HashSet<(StringName group, StringName name)> DirtyTiles = [];

    public SpriteSet()
    {
        if(! HasCustomDataLayerByName("Group"))
        {
            int groupLayerId = GetCustomDataLayersCount();
            AddCustomDataLayer();
            SetCustomDataLayerName(groupLayerId, "Group");
            SetCustomDataLayerType(groupLayerId, Variant.Type.StringName);
        }
        if(! HasCustomDataLayerByName("Name"))
        {
            int nameLayerId = GetCustomDataLayersCount();
            AddCustomDataLayer();
            SetCustomDataLayerName(nameLayerId, "Name");
            SetCustomDataLayerType(nameLayerId, Variant.Type.StringName);
        }
        if(! HasCustomDataLayerByName("Offset"))
        {
            int offsetLayerId = GetCustomDataLayersCount();
            AddCustomDataLayer();
            SetCustomDataLayerName(offsetLayerId, "Offset");
            SetCustomDataLayerType(offsetLayerId, Variant.Type.Vector2);
        }
        RegisterSprites();
        Changed += AddToRegisterSets;
    }

    private void AddToRegisterSets()
    {
        SetsToRegister.Add(this);
    }

    private void RegisterSprites()
    {
        void AddSprite(SpriteData data)
        {
            if(! Sprites.TryGetValue(data.Group, out var sprites))
            {
                sprites = [];
                Sprites[data.Group] = sprites;
            }
            sprites[data.Name] = data;
        }

        Sprites.Clear();
        Textures.Clear();
        for(int s = 0; s < GetSourceCount(); ++s)
        {
            var source = GetSource(s);
            if(source is not TileSetAtlasSource atlas)
                continue;
            Textures[s] = atlas.Texture;
            for(int t = 0; t < atlas.GetTilesCount(); ++t)
            {
                var coord = atlas.GetTileId(t);
                for(int a = 0; a < atlas.GetAlternativeTilesCount(coord); ++a)
                {
                    TileData tileData = atlas.GetTileData(coord, a);
                    SpriteData spriteData = new()
                    {
                        TileCoord = coord,
                        TileSize = atlas.GetTileSizeInAtlas(coord),
                        AlternateIndex = a,
                        TextureIndex = s,
                        Group = tileData.GetCustomData("Group").As<StringName>(),
                        Name = tileData.GetCustomData("Name").As<StringName>(),
                    };
                    AddSprite(spriteData);
                }
            }
        }
    }

    public TileData GetData(SpriteData data)
    {
        var atlas = GetSource(data.TextureIndex) as TileSetAtlasSource;
        return atlas.GetTileData(data.TileCoord, data.AlternateIndex);
    }

    public bool TryGetSprite(StringName group, StringName name, out SpriteData data)
    {
        if(! Sprites.TryGetValue(group, out var sprites))
        {
            data = null;
            return false;
        }
        return sprites.TryGetValue(name, out data);
    }
    public Texture2D GetTexture(int index) => Textures[index];
    public string[] GetGroups() => [..Sprites.Keys];
    public string[] GetNames(StringName group) => [..Sprites[group].Keys];
    public StringName GetFirstName(StringName group)
    {
        if(! Sprites.TryGetValue(group, out var sprites) || sprites.Count == 0)
            return "";
        return sprites.Keys.First();
    }

    public void SetOffset(StringName group, StringName name, Vector2 value)
    {
        if(!TryGetSprite(group, name, out var data))
            return;
        var tileData = GetData(data);
        tileData.SetCustomData("Offset", value);
        DirtyTiles.Add((group, name));
    }
    public Vector2? GetOffset(StringName group, StringName name)
    {
        if(!TryGetSprite(group, name, out var data))
            return null;
        var tileData = GetData(data);
        return tileData.GetCustomData("Offset").As<Vector2>();
    }
    public void ResetDirty(StringName group, StringName name)
    {
        DirtyTiles.Remove((group, name));
    }
    public bool IsDirty(StringName group, StringName name)
    {
        return DirtyTiles.Contains((group,name));
    }
}
