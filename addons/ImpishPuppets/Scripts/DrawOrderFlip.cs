using Godot;
using System;
using System.Collections.Generic;

namespace ImpishPuppets;

[Tool]
[GlobalClass]
public partial class DrawOrderFlip : Node2D
{
    [Export]
    public bool Flipped
    {
        get => _Flipped;
        set
        {
            _Flipped = value;
            ApplyOrder(this);
        }
    }
    public bool _Flipped = false;

    private bool OtherFlipped = false;

    public override void _EnterTree()
    {
        CallDeferred("ApplyOrder", this);
    }

    private void ApplyOrder(Node node)
    {
        var children = new List<CanvasItem>();
        foreach (var child in node.GetChildren())
            if (child is CanvasItem ci) children.Add(ci);

        int count = children.Count;
        for (int i = 0; i < count; i++)
        {
            var ci = children[i];
            if(ci is DrawOrderFlip drawFlip)
            {
                drawFlip.OtherFlipped = Flipped;
                drawFlip.ApplyOrder(drawFlip);
                continue;
            }
            if(ci.HasMeta("DontOrderFlip"))
            {
                ApplyOrder(ci);
                continue;
            }

            var rid = ci.GetCanvasItem();

            int drawIndex = _Flipped ^ OtherFlipped ? (count - 1 - i) : i;
            int zIndex = _Flipped ^ OtherFlipped ? -ci.ZIndex : ci.ZIndex;

            RenderingServer.CanvasItemSetDrawIndex(rid, drawIndex);
            RenderingServer.CanvasItemSetZIndex(rid, zIndex);

            ApplyOrder(ci);
        }
    }
}
