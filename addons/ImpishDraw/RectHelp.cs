using Godot;

namespace ImpishDraw;

public static class RectHelp
{
    public static Rect2I SizeRect(this Rect2I rect)
    {
        return new(Vector2I.Zero, rect.Size);
    }
}