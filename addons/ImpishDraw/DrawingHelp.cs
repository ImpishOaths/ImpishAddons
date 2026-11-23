#if TOOLS
using Godot;
using System.Collections.Generic;

namespace ImpishDraw;

public static class LineHelper
{
    private static float GetPointDistanceToLine(Vector2 start, Vector2 end, Vector2 point)
    {
        float lengthSquared = end.DistanceSquaredTo(start);
    
        if (lengthSquared == 0.0f)
            return point.DistanceTo(start);

        float t = (point - start).Dot(end - start) / lengthSquared;
        t = Mathf.Clamp(t, 0.0f, 1.0f);
        Vector2 projection = start + t * (end - start);
        float dist = point.DistanceTo(projection);
        return dist;
    }

    public static Rect2I GetMinimumRegion(Vector2 start, Vector2 end, Rect2I region, float radius)
    {
        return new Rect2I((Vector2I)start,0,0)
            .Expand((Vector2I)end)
            .Grow((int)Mathf.Ceil(radius)+2)
            .Intersection(region);

    }

    public static Dictionary<Vector2I, float> GetRegionStrengths(Vector2 start, Vector2 end, Rect2I drawRegion, float radius, float softness)
    {
        Dictionary<Vector2I, float> strengths = [];

        for(int x = 0; x < drawRegion.Size.X; ++x)
        {
            for(int y = 0; y < drawRegion.Size.Y; ++y)
            {
                Vector2I cell = new Vector2I(x, y) + drawRegion.Position;
                float distance = GetPointDistanceToLine(start, end, cell);
                float strength = distance / radius;
                if(softness > 0)
                {
                    strength = Mathf.Lerp(1, 0, (strength - (1f-softness))/softness);
                }
                else
                {
                    strength = strength > 1? 0: 1;
                }
                strength = Mathf.Clamp(strength, 0, 1);
                strengths[cell] = strength;
            }
        }

        return strengths;
    }
}

public interface DrawingTool
{
    public string Name {get;}
    public bool Began {get;}
    public void Begin(Vector2 pos, Image baseImage, ImageTexture texture, Rect2I region, BrushResource brush, InputEventMouseButton mouse);
    public void Step(Vector2 pos);
    public void Abort();
    public (Image Output, Rect2I Region, DrawingTool NextTool) End();
    public bool Edited {get;}
    public bool ShowSelection {get;}
    public Vector2[] GetSelection();
    public bool Interrupt(InputEventMouseButton mouse) => false;
}

public abstract class BrushLikeTool: DrawingTool
{
    protected Image BaseImage = new();
    protected Image DrawingImage = new();
    protected bool RightDown;
    protected ImageTexture Texture;
    protected Rect2I Region;
    protected Vector2 StartingPos;
    protected BrushResource Brush;
    protected bool _Began = false;
    public bool Began => _Began;

    public abstract string Name {get;}

    public bool Edited => true;
    public bool ShowSelection => false;
    public Vector2[] GetSelection() => null;

    protected void ErasePixel(Vector2I pix, float strength)
    {
        var color = DrawingImage.GetPixelv(pix);
        color *= 1f-strength;
        DrawingImage.SetPixelv(pix, color);
    }

    protected void DrawPixel(Vector2I pix, float strength)
    {
        Color originalColor = DrawingImage.GetPixelv(pix);
        Color finalColor = Brush.Color;
        finalColor = originalColor.Lerp(finalColor, Mathf.Sqrt(strength));
        DrawingImage.SetPixelv(pix, finalColor);
    }

    public virtual void Begin(Vector2 pos, Image baseImage, ImageTexture texture, Rect2I region, BrushResource brush, InputEventMouseButton mouse)
    {
        pos -= Vector2.One * 0.5f;
        StartingPos = pos;
        Texture = texture;
        RightDown = mouse.ButtonIndex == MouseButton.Right;
        Region = region;
        BaseImage.CopyFrom(baseImage);
        DrawingImage.CopyFrom(baseImage);
        Brush = brush;
        _Began = true;
    }

    public void Abort()
    {
        Texture.Update(BaseImage);
    }

    public abstract void Step(Vector2 ev);
    public abstract (Image Output, Rect2I Region, DrawingTool NextTool) End();
    public virtual bool Interrupt(InputEventMouseButton mouse) => false;
}

public class BrushTool: BrushLikeTool
{
    public override string Name => RightDown ? "Texture Erase" : "Texture Draw";
    
    private readonly Dictionary<Vector2I, float> StrengthMap = [];

    private Rect2I? UsedRegion = null;
    private Vector2? PreviousPos = null;

    private void AddToMap(Dictionary<Vector2I, float> ToAdd)
    {
        foreach(var (position, strength) in ToAdd)
        {
            StrengthMap.TryGetValue(position, out float existing);
            StrengthMap[position] = Mathf.Max(existing, strength);
        }
    }

    private void AddToDrawing(Vector2 previous, Vector2 current)
    {
        DrawingImage.BlitRect(BaseImage, Region, Region.Position);
        float radius = Brush.GetRadius();
        float softness = Brush.GetSoftness();
        var drawRegion = LineHelper.GetMinimumRegion(previous, current, Region, radius);
        UsedRegion ??= drawRegion;
        UsedRegion.Value.Merge(drawRegion);
        var line = LineHelper.GetRegionStrengths(previous, current, drawRegion, radius, softness);
        AddToMap(line);
        if(RightDown)
        {
            foreach(var (position, strength) in StrengthMap)
                ErasePixel(position, strength);
        }
        else
        {
            foreach(var (position, strength) in StrengthMap)
                DrawPixel(position, strength);
        }
    }

    public override void Step(Vector2 pos)
    {
        pos -= Vector2.One * 0.5f;
        if(PreviousPos == null)
            PreviousPos = pos;
        AddToDrawing(PreviousPos.Value, pos);
        PreviousPos = pos;
        Texture.Update(DrawingImage);
    }

    public override (Image Output, Rect2I Region, DrawingTool NextTool) End()
    {
        _Began = false;
        return (DrawingImage, Region, null);
    }
}

public class LineTool: BrushLikeTool
{
    public override string Name => RightDown ? "Texture Erase Spline" : "Texture Draw Spline";

    private class Anchor
    {
        public Vector2 Position;
        public Vector2 HandleOffset;
    }

    private const int CurveSteps = 16;

    private List<Anchor> Anchors;
    private Vector2 CurrentPos;
    private bool EditingHandle;

    public override void Begin(Vector2 pos, Image baseImage, ImageTexture texture, Rect2I region, BrushResource brush, InputEventMouseButton mouse)
    {
        base.Begin(pos, baseImage, texture, region, brush, mouse);
        Anchors = [new Anchor { Position = StartingPos, HandleOffset = Vector2.Zero }];
        CurrentPos = StartingPos;
        EditingHandle = false;
    }

    public override void Step(Vector2 pos)
    {
        pos -= Vector2.One * 0.5f;
        CurrentPos = pos;
        if(EditingHandle)
            Anchors[^1].HandleOffset = CurrentPos - Anchors[^1].Position;
        DrawSpline();
        Texture.Update(DrawingImage);
    }

    public override (Image Output, Rect2I Region, DrawingTool NextTool) End()
    {
        EditingHandle = false;
        DrawSpline();
        _Began = false;
        return (DrawingImage, Region, null);
    }

    public override bool Interrupt(InputEventMouseButton mouse)
    {
        MouseButton alternate = RightDown ? MouseButton.Left : MouseButton.Right;
        if(mouse.ButtonIndex != alternate)
            return false;

        if(mouse.Pressed)
        {
            Anchors.Add(new Anchor { Position = CurrentPos, HandleOffset = Vector2.Zero });
            EditingHandle = true;
        }
        else
        {
            EditingHandle = false;
        }
        DrawSpline();
        Texture.Update(DrawingImage);
        return true;
    }

    private void DrawSpline()
    {
        DrawingImage.BlitRect(BaseImage, Region, Region.Position);
        float radius = Brush.GetRadius();
        float softness = Brush.GetSoftness();

        var polyline = BuildPolyline();

        Dictionary<Vector2I, float> combined = [];
        for(int i = 0; i < polyline.Count - 1; ++i)
        {
            var drawRegion = LineHelper.GetMinimumRegion(polyline[i], polyline[i + 1], Region, radius);
            var strengths = LineHelper.GetRegionStrengths(polyline[i], polyline[i + 1], drawRegion, radius, softness);
            foreach(var (position, strength) in strengths)
            {
                combined.TryGetValue(position, out float existing);
                combined[position] = Mathf.Max(existing, strength);
            }
        }

        if(RightDown)
        {
            foreach(var (position, strength) in combined)
                ErasePixel(position, strength);
        }
        else
        {
            foreach(var (position, strength) in combined)
                DrawPixel(position, strength);
        }
    }

    private List<Vector2> BuildPolyline()
    {
        List<Vector2> polyline = [Anchors[0].Position];
        for(int i = 0; i < Anchors.Count - 1; ++i)
        {
            Anchor a = Anchors[i];
            Anchor b = Anchors[i + 1];
            AppendBezier(polyline, a.Position, a.Position + a.HandleOffset, b.Position - b.HandleOffset, b.Position);
        }
        if(! EditingHandle && Anchors.Count == 1)
        {
            Anchor last = Anchors[^1];
            AppendBezier(polyline, last.Position, last.Position + last.HandleOffset, CurrentPos, CurrentPos);
        }
        return polyline;
    }

    private static void AppendBezier(List<Vector2> polyline, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        for(int i = 1; i <= CurveSteps; ++i)
        {
            float t = (float)i / CurveSteps;
            polyline.Add(EvaluateBezier(p0, p1, p2, p3, t));
        }
    }

    private static Vector2 EvaluateBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        return u*u*u*p0 + 3f*u*u*t*p1 + 3f*u*t*t*p2 + t*t*t*p3;
    }
}

public class FillTool: BrushLikeTool
{
    public override string Name => RightDown ? "Fill Erase" : "Fill";

    public override void Begin(Vector2 pos, Image baseImage, ImageTexture texture, Rect2I region, BrushResource brush, InputEventMouseButton mouse)
    {
        base.Begin(pos, baseImage, texture, region, brush, mouse);
        PerformFill();
        Texture.Update(DrawingImage);
    }

    public override void Step(Vector2 pos) {}

    public override (Image Output, Rect2I Region, DrawingTool NextTool) End()
    {
        _Began = false;
        return (DrawingImage, Region, null);
    }

    private static readonly Vector2I[] NeighborOffsets = [Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right];

    private void PerformFill()
    {
        Vector2I seed = (Vector2I)StartingPos;
        if(! Region.HasPoint(seed))
            return;

        float tolerance = Mathf.Clamp(Brush.Size / 10f, 0f, 1f);
        float softness = Mathf.Clamp(Brush.Softness / 10f, 0f, 1f);
        Color seedColor = BaseImage.GetPixelv(seed);

        HashSet<Vector2I> included = [];
        HashSet<Vector2I> visited = [seed];
        Queue<Vector2I> frontier = new();
        frontier.Enqueue(seed);
        while(frontier.Count > 0)
        {
            Vector2I current = frontier.Dequeue();
            float diffRatio = ColorDifference(seedColor, BaseImage.GetPixelv(current)) / tolerance;
            if(diffRatio > 1f)
                continue;
            included.Add(current);
            foreach(var offset in NeighborOffsets)
            {
                Vector2I next = current + offset;
                if(visited.Contains(next) || ! Region.HasPoint(next))
                    continue;
                visited.Add(next);
                frontier.Enqueue(next);
            }
        }

        Dictionary<Vector2I, float> strengths = [];
        foreach(var pixel in included)
        {
            bool isBoundary = false;
            foreach(var offset in NeighborOffsets)
            {
                Vector2I next = pixel + offset;
                if(! Region.HasPoint(next) || ! included.Contains(next))
                {
                    isBoundary = true;
                    break;
                }
            }

            float strength = 1f;
            if(isBoundary)
            {
                float diffRatio = ColorDifference(seedColor, BaseImage.GetPixelv(pixel)) / tolerance;
                if(softness > 0)
                    strength = Mathf.Lerp(1, 0, (diffRatio - (1f-softness))/softness);
                else
                    strength = diffRatio > 1 ? 0 : 1;
                strength = Mathf.Clamp(strength, 0, 1);
            }
            strengths[pixel] = strength;
        }

        if(RightDown)
        {
            foreach(var (position, strength) in strengths)
                ErasePixel(position, strength);
        }
        else
        {
            foreach(var (position, strength) in strengths)
                DrawPixel(position, strength);
        }
    }

    private static float ColorDifference(Color a, Color b)
    {
        float dr = a.R - b.R;
        float dg = a.G - b.G;
        float db = a.B - b.B;
        float da = a.A - b.A;
        return Mathf.Sqrt(dr*dr + dg*dg + db*db + da*da) / 2f;
    }
}

#endif