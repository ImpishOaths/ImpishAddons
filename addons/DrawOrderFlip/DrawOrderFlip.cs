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
            Scale = new(_Flipped ? -1 : 1, 1);
        }
    }
    public bool _Flipped = false;

    public override void _Ready() => ProcessPriority = 4096; // run after animations

    public override void _Process(double delta)
    {
        if(_Flipped)
            ApplyOrder(this);
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
            var rid = ci.GetCanvasItem();

            int drawIndex = _Flipped ? (count - 1 - i) : i;
            int zIndex = _Flipped ? -ci.ZIndex : ci.ZIndex;

            RenderingServer.CanvasItemSetDrawIndex(rid, drawIndex);
            RenderingServer.CanvasItemSetZIndex(rid, zIndex);

            ApplyOrder(ci); // recurse into grandchildren
        }
    }
}
