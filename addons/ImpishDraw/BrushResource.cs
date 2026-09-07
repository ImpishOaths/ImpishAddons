#if TOOLS
using Godot;
using System;

namespace ImpishDraw;

[Tool]
[GlobalClass]
public partial class BrushResource: Resource
{
    [Export]
    public float Size;
    [Export]
    public float Softness;
    [Export]
    public Color Color;
    public float GetRadius() => Size/2f;
    public float GetSoftness() => Softness/Size;
}
#endif