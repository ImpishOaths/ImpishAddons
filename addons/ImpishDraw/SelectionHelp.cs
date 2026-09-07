#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImpishDraw;

public static class SelectionHelper
{
    public static Image GetPolygonMask(Vector2[] PolygonVertecies, Vector2I size, int ySubSamples = 4)
    {
        Image result = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);

        float[] coverage = new float[size.X];
        List<float> intersections = [];
        for(int y = 0; y < size.Y; ++y)
        {
            System.Array.Clear(coverage, 0, coverage.Length);
            for(int s = 0; s < ySubSamples; ++s)
            {
                float sampleY = y + (s + 0.5f) / ySubSamples;
                intersections.Clear();
                for(int i = 0; i < PolygonVertecies.Length - 1; ++i)
                {
                    Vector2 a = PolygonVertecies[i];
                    Vector2 b = PolygonVertecies[i + 1];
                    if(a.Y == b.Y)
                        continue;
                    if(sampleY < Mathf.Min(a.Y, b.Y) || sampleY >= Mathf.Max(a.Y, b.Y))
                        continue;
                    float t = (sampleY - a.Y) / (b.Y - a.Y);
                    intersections.Add(a.X + t * (b.X - a.X));
                }
                intersections.Sort();

                for(int k = 0; k + 1 < intersections.Count; k += 2)
                    AccumulateIntervalCoverage(coverage, intersections[k], intersections[k + 1], size.X);
            }

            for(int x = 0; x < size.X; ++x)
                result.SetPixel(x, y, new Color(1f, 1f, 1f, coverage[x] / ySubSamples));
        }
        return result;
    }

    public static Image ApplyMask(Image input, Image mask)
    {
        Image result = new();
        result.CopyFrom(input);
        Vector2I size = input.GetSize();
        for(int y = 0; y < size.Y; ++y)
        {
            for(int x = 0; x < size.X; ++x)
            {
                Color pixel = result.GetPixel(x, y);
                pixel.A *= mask.GetPixel(x, y).A;
                result.SetPixel(x, y, pixel);
            }
        }
        return result;
    }

    private static void AccumulateIntervalCoverage(float[] coverage, float start, float end, int width)
    {
        start = Mathf.Clamp(start, 0f, width);
        end = Mathf.Clamp(end, 0f, width);
        if(end <= start)
            return;

        int xStart = Mathf.FloorToInt(start);
        int xEnd = Mathf.FloorToInt(end);
        if(xStart == xEnd)
        {
            if(xStart < width)
                coverage[xStart] += end - start;
            return;
        }

        coverage[xStart] += xStart + 1 - start;
        for(int x = xStart + 1; x < xEnd; ++x)
            coverage[x] += 1f;
        if(xEnd < width)
            coverage[xEnd] += end - xEnd;
    }

    public static Image TransformImage(Image startingImage, Transform2D transform, Rect2I region)
    {
        Vector2I srcSize = startingImage.GetSize();
        Image result = Image.CreateEmpty(srcSize.X, srcSize.Y, false, startingImage.GetFormat());
        Transform2D inverse = transform.AffineInverse();

        for(int y = 0; y < srcSize.Y; ++y)
        {
            for(int x = 0; x < srcSize.X; ++x)
            {
                Vector2 dest = region.Position + new Vector2(x + 0.5f, y + 0.5f);
                Vector2 src = inverse * dest - region.Position;
                result.SetPixel(x, y, SampleBilinear(startingImage, src, srcSize));
            }
        }
        return result;
    }

    private static Color SampleBilinear(Image image, Vector2 pixelCenterPos, Vector2I size)
    {
        Vector2 f = pixelCenterPos - new Vector2(0.5f, 0.5f);
        int x0 = Mathf.FloorToInt(f.X);
        int y0 = Mathf.FloorToInt(f.Y);
        float tx = f.X - x0;
        float ty = f.Y - y0;

        Color c00 = GetPixelOrTransparent(image, x0, y0, size);
        Color c10 = GetPixelOrTransparent(image, x0 + 1, y0, size);
        Color c01 = GetPixelOrTransparent(image, x0, y0 + 1, size);
        Color c11 = GetPixelOrTransparent(image, x0 + 1, y0 + 1, size);

        Color top = c00.Lerp(c10, tx);
        Color bottom = c01.Lerp(c11, tx);
        return top.Lerp(bottom, ty);
    }

    private static Color GetPixelOrTransparent(Image image, int x, int y, Vector2I size)
    {
        if(x < 0 || y < 0 || x >= size.X || y >= size.Y)
            return new Color(0f, 0f, 0f, 0f);
        return image.GetPixel(x, y);
    }

    public static void SubtractAlpha(Image target, Image mask)
    {
        Vector2I size = target.GetSize();
        for(int y = 0; y < size.Y; ++y)
        {
            for(int x = 0; x < size.X; ++x)
            {
                Color pixel = target.GetPixel(x, y);
                pixel.A *= 1f - mask.GetPixel(x, y).A;
                target.SetPixel(x, y, pixel);
            }
        }
    }

    public static Rect2I GetPolygonBounds(Vector2[] points)
    {
        Rect2I bounds = new((Vector2I)points[0], Vector2I.Zero);
        foreach(var point in points)
            bounds = bounds.Expand((Vector2I)point);
        return bounds;
    }
}

public class SelectorTool: DrawingTool
{
    public string Name => "Select Tool";
    private bool _Began = false;
    public bool Began => _Began;
    private Rect2I Region;
    private bool LassoMode;
    private Image BaseImage;
    private bool ShiftDown;

    public bool Edited => false;
    public bool ShowSelection => true;

    private Vector2 StartPos;
    private Vector2 EndPos;
    private List<Vector2> LassoSelection = [];
    public void Begin(Vector2 pos, Image baseImage, ImageTexture texture, Rect2I region, BrushResource brush, InputEventMouseButton mouse)
    {
        _Began = true;
        Region = region;
        StartPos = pos;
        LassoMode = mouse.ButtonIndex == MouseButton.Right;
        ShiftDown = mouse.ShiftPressed;
        if(LassoMode)
            LassoSelection.Add(StartPos);
        BaseImage = baseImage.GetRegion(region);
    }
    public void Step(Vector2 pos)
    {
        if(LassoMode)
        {
            if(LassoSelection[^1].DistanceSquaredTo(pos) > 1)
                LassoSelection.Add(pos);
        }
        else
            EndPos = pos;
    }
    public void Abort()
    {
        
    }
    public (Image Output, Rect2I Region, DrawingTool NextTool) End()
    {
        var selection = GetSelection();
        var localSelection = selection.Select(v => v - Region.Position).ToArray();

        Image selectionMask = SelectionHelper.GetPolygonMask(localSelection, BaseImage.GetSize());
        Image selectionImage = SelectionHelper.ApplyMask(BaseImage, selectionMask);
        Image cutoutImage = new();
        cutoutImage.CopyFrom(BaseImage);
        SelectionHelper.SubtractAlpha(cutoutImage, selectionMask);

        var mover = new MoveTool(selection, cutoutImage, selectionImage);
        return (null, Region, mover);
    }
    public Vector2[] GetSelection()
    {
        if(LassoMode)
            return GetLassoSelection();
        else
            return GetBoxSelection();
    }
    private Vector2[] GetBoxSelection()
    {
        Rect2 selection = new(StartPos, Vector2.Zero);
        selection = selection.Expand(EndPos);
        selection = selection.Intersection(Region);
        if(ShiftDown)
        {
            Vector2 pos = selection.Position.Round();
            Vector2 end = selection.End.Round();
            selection = new Rect2(pos, end - pos);
        }
        return [
            selection.Position,
            selection.Position + Vector2.Right*selection.Size.X,
            selection.End,
            selection.Position + Vector2.Down*selection.Size.Y,
            selection.Position
        ];
    }
    private Vector2[] GetLassoSelection()
    {
        Vector2[] selection = new Vector2[LassoSelection.Count + 1];
        LassoSelection.CopyTo(selection);
        selection[^1] = LassoSelection[0];
        for(int i = 0; i < selection.Length; ++i)
        {
            selection[i] = selection[i].Clamp(Region.Position, Region.End);
            if(ShiftDown)
                selection[i] = selection[i].Round();
        }
        return selection;
    }
}

public class MoveTool(Vector2[] selectionRegion, Image cutoutImage, Image selectionImage, Transform2D? initialTransform = null): DrawingTool
{
    [Flags]
    private enum ButtonFlags
    {
        NONE = 0,
        SHIFT = 1,
        CONTROL = 2,
    }
    private enum MoveMode
    {
        TRANSLATE = ButtonFlags.NONE,
        ROTATE = ButtonFlags.SHIFT,
        UNIFORM_SCALE = ButtonFlags.CONTROL,
        SCALE = ButtonFlags.SHIFT + ButtonFlags.CONTROL,
    }

    public string Name => "Transform Selection";
    private bool _Began = false;
    public bool Began => _Began;
    public Rect2I Region;
    public Vector2[] SelectedRegion = selectionRegion;
    public bool RightDown;
    private MoveMode Mode = MoveMode.TRANSLATE;
    public ImageTexture Texture;
    public Image SelectionImage = selectionImage;
    public Image CutoutImage = cutoutImage;
    public Image DrawingImage = new();
    private Vector2 SelectionCenter;
    // Grows across Begin/Step to track the actual bounding box the selection has occupied during
    // this drag, so the committed undo region can be cropped to that instead of the whole sprite.
    private Rect2I DirtyRegion;

    public Transform2D InitialTransform = initialTransform ?? Transform2D.Identity;
    public Transform2D MoveTransform = initialTransform ?? Transform2D.Identity;

    public bool Edited => true;
    public bool ShowSelection => true;

    private Vector2 StartPos;
    public void Begin(Vector2 pos, Image baseImage, ImageTexture texture, Rect2I region, BrushResource brush, InputEventMouseButton mouse)
    {
        _Began = true;
        StartPos = pos;
        Region = region;
        RightDown = mouse.ButtonIndex == MouseButton.Right;

        ButtonFlags flags = ButtonFlags.NONE;
        if(mouse.ShiftPressed)
            flags |= ButtonFlags.SHIFT;
        if(mouse.CtrlPressed)
            flags |= ButtonFlags.CONTROL;
        Mode = (MoveMode)flags;

        Texture = texture;
        DrawingImage.CopyFrom(baseImage);

        SelectionCenter = Vector2.Zero;
        foreach(var point in SelectedRegion[..^1])
            SelectionCenter += point;
        SelectionCenter /= SelectedRegion.Length-1;

        DirtyRegion = SelectionHelper.GetPolygonBounds(GetSelection());
    }
    public void Step(Vector2 pos)
    {
        if(RightDown)
            return;
        MoveTransform = Mode switch
        {
            MoveMode.TRANSLATE => Translate(InitialTransform, pos - StartPos),
            MoveMode.ROTATE => Rotate(InitialTransform, StartPos, pos, InitialTransform * SelectionCenter),
            MoveMode.SCALE => Scale(InitialTransform, StartPos, pos, InitialTransform * SelectionCenter),
            MoveMode.UNIFORM_SCALE => UniformScale(InitialTransform, StartPos, pos, InitialTransform * SelectionCenter),
            _ => Transform2D.Identity
        };
        AddToDrawing();
        Texture.Update(DrawingImage);
    }
    private void AddToDrawing()
    {
        Image transformedImage = SelectionHelper.TransformImage(SelectionImage, MoveTransform, Region);
        Image tempImage = new();
        tempImage.CopyFrom(CutoutImage);
        tempImage.BlendRect(transformedImage, Region.SizeRect(), Vector2I.Zero);
        DrawingImage.BlitRect(tempImage, Region.SizeRect(), Region.Position);
        DirtyRegion = DirtyRegion.Merge(SelectionHelper.GetPolygonBounds(GetSelection()));
    }
    public void Abort()
    {
        RightDown = true;
    }
    public (Image Output, Rect2I Region, DrawingTool NextTool) End()
    {
        _Began = false;
        AddToDrawing();
        // Undo only needs to cover the area the selection actually swept through this drag, not
        // the whole sprite. Grow by a couple pixels first: TransformImage samples bilinearly, so
        // the transformed selection can bleed slightly past its exact polygon bounds at both the
        // destination and the cutout edge left behind - without this margin that bleed falls
        // outside the committed region and shows up as thin leftover streaks. Fall back to the
        // full region in the degenerate case where a chained drag left the selection entirely
        // off-canvas, so downstream image ops never see a zero-area rect.
        Rect2I committedRegion = DirtyRegion.Grow(2).Intersection(Region);
        if(! committedRegion.HasArea())
            committedRegion = Region;
        if(RightDown)
        {
            return (DrawingImage, committedRegion, null);
        }
        var mover = new MoveTool(SelectedRegion, CutoutImage, SelectionImage, MoveTransform);
        return (DrawingImage, committedRegion, mover);
    }
    public Vector2[] GetSelection()
    {
        return [.. SelectedRegion.Select((v)=>{return MoveTransform * v;})];
    }

    private static Transform2D Translate(Transform2D initial, Vector2 offset)
    {
        return initial.Translated(offset);
    }

    private static Transform2D Rotate(Transform2D initial, Vector2 start, Vector2 current, Vector2 center)
    {
        float angle = (start - center).AngleTo(current - center);
        Transform2D pivotRotation = new(angle, center - center.Rotated(angle));
        return pivotRotation * initial;
    }

    private static Transform2D Scale(Transform2D initial, Vector2 start, Vector2 current, Vector2 center)
    {
        Vector2 startOffset = start - center;
        Vector2 currentOffset = current - center;

        float scaleX = Mathf.Abs(startOffset.X) < Mathf.Epsilon ? 1f : currentOffset.X / startOffset.X;
        float scaleY = Mathf.Abs(startOffset.Y) < Mathf.Epsilon ? 1f : currentOffset.Y / startOffset.Y;

        Vector2 origin = center - new Vector2(center.X * scaleX, center.Y * scaleY);
        Transform2D pivotScale = new(new Vector2(scaleX, 0f), new Vector2(0f, scaleY), origin);
        return pivotScale * initial;
    }

    private static Transform2D UniformScale(Transform2D initial, Vector2 start, Vector2 current, Vector2 center)
    {
        float startDist = start.DistanceTo(center);
        if(startDist < Mathf.Epsilon)
            return initial;

        float scaleFactor = current.DistanceTo(center) / startDist;
        Transform2D pivotScale = new(new Vector2(scaleFactor, 0f), new Vector2(0f, scaleFactor), center - center * scaleFactor);
        return pivotScale * initial;
    }
}

#endif