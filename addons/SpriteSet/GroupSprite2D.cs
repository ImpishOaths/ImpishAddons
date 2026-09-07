using Godot;
using Godot.Collections;

namespace SpriteSet;

[Tool]
[GlobalClass]
[Icon("res://addons/SpriteSet/GroupSprite.png")]
public partial class GroupSprite2D: Sprite2D
{
    public GroupSprite2D()
    {
        RegionEnabled = true;
        RegionFilterClipEnabled = true;
    }

    public StringName SpriteGroup
    {
        get => _SpriteGroup;
        set
        {
            if(_SpriteGroup == value)
                return;
            _SpriteGroup = value;
            _SpriteName = GetFirstSpriteInGroup();
            DeferredTryUpdateSprite();
            NotifyPropertyListChanged();
        }
    }
    private StringName _SpriteGroup = "";

    public StringName SpriteName
    {
        get => _SpriteName;
        set
        {
            if(_SpriteName == value)
                return;
            _SpriteName = value;
            DeferredTryUpdateSprite();
            NotifyPropertyListChanged();
        }
    }
    private StringName _SpriteName = "";

    [Export]
    public SpriteSet SpriteSet
    {
        get => _SpriteSet;
        set
        {
            if(_SpriteSet == value)
                return;
            _SpriteSet = value;
            if(_SpriteSet == null || ! _SpriteSet.TryGetSprite(_SpriteGroup, _SpriteName, out _))
            {
                _SpriteGroup = GetFirstGroup();
                _SpriteName = GetFirstSpriteInGroup();
            }
            DeferredTryUpdateSprite();
            NotifyPropertyListChanged();
        }
    }
    private SpriteSet _SpriteSet = null;

    private void DeferredTryUpdateSprite()
        => Callable.From(() =>
        {
            if(! TryUpdateSprite())
            {
                _SpriteGroup = "";
                _SpriteName = "";
            }
            NotifyPropertyListChanged();
        }).CallDeferred();

    public override bool _Set(StringName property, Variant value)
    {
        if(property == "SpriteGroup")
        {
            SpriteGroup = value.As<StringName>();
            return true;
        }
        if(property == "SpriteName")
        {
            SpriteName = value.As<StringName>();
            return true;
        }
        return false;
    }

    public override Variant _Get(StringName property)
    {
        if(property == "SpriteGroup")
            return _SpriteGroup;
        if(property == "SpriteName")
            return _SpriteName;
        return default;
    }

    public override Array<Dictionary> _GetPropertyList()
    {
        Array<Dictionary> properties = [];

        string groups = "";
        if(SpriteSet != null)
            groups = string.Join(',', SpriteSet.GetGroups());
        
        properties.Add(new(){
            { "name", "SpriteGroup" },
            { "type", (int)Variant.Type.StringName },
            { "hint", (int)PropertyHint.Enum },
            { "hint_string", groups }
        });

        string names = "";
        if(SpriteSet != null && ! _SpriteGroup.IsEmpty)
            names = string.Join(',', SpriteSet.GetNames(_SpriteGroup));

        properties.Add(new(){
            { "name", "SpriteName" },
            { "type", (int)Variant.Type.StringName },
            { "hint", (int)PropertyHint.Enum },
            { "hint_string", names }
        });

        return properties;
    }

    private StringName GetFirstGroup()
    {
        if(_SpriteSet == null)
            return "";
        var groups = _SpriteSet.GetGroups();
        return groups.Length > 0 ? groups[0] : "";
    }

    private StringName GetFirstSpriteInGroup()
    {
        if(_SpriteGroup.IsEmpty || _SpriteSet == null)
            return "";
        return _SpriteSet.GetFirstName(_SpriteGroup);
    }

    private bool TryUpdateSprite()
    {
        if(_SpriteSet == null || _SpriteGroup.IsEmpty || _SpriteName.IsEmpty)
        {
            ClearSprite();
            return false;
        }
        if(! _SpriteSet.TryGetSprite(_SpriteGroup, _SpriteName, out var data))
        {
            ClearSprite();
            return false;
        }
        UpdateSprite(data);
        return true;
    }

    private void UpdateSprite(SpriteData spriteData)
    {
        Texture = SpriteSet.GetTexture(spriteData.TextureIndex);
        RegionRect = new(spriteData.TileCoord * SpriteSet.TileSize, spriteData.TileSize * SpriteSet.TileSize);
        TileData tileData = SpriteSet.GetData(spriteData);

        if(tileData.Material != null)
            Material = tileData.Material;
        else if(SpriteSet.DefaultMaterial != null)
            Material = SpriteSet.DefaultMaterial;

        FlipH = tileData.FlipH;
        FlipV = tileData.FlipV;
        Offset = tileData.GetCustomData("Offset").As<Vector2>();
#if TOOLS
        PreviousOffset = Offset;
#endif

        NotifyPropertyListChanged();
    }

    private void ClearSprite()
    {
        Texture = null;
        RegionRect = new();
        Material = null;
    }

#if TOOLS
    private Vector2 PreviousOffset;
    private bool SetDirty = false;
    public override void _Process(double delta)
    {
        if(SpriteSet == null || _SpriteGroup.IsEmpty || _SpriteName.IsEmpty)
            return;
        
        if(SetDirty)
        {
            SpriteSet.ResetDirty(_SpriteGroup, _SpriteName);
            SetDirty = false;
            return;
        }

        if(SpriteSet.IsDirty(_SpriteGroup, _SpriteName))
        {
            Vector2? offset = SpriteSet.GetOffset(_SpriteGroup, _SpriteName);
            if(offset != null)
            {
                Offset = offset.Value;
                PreviousOffset = Offset;
            }
            //TryUpdateSprite();
        }
        else if(Offset != PreviousOffset)
        {
            //Vector2 diff = PreviousOffset - Offset;
            //Position += diff;
            SpriteSet.SetOffset(_SpriteGroup, _SpriteName, Offset);
            PreviousOffset = Offset;
            SetDirty = true;
            //TryUpdateSprite();
        }
    }
#endif
}
