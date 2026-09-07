#if TOOLS
using Godot;
using System;

namespace ImpishDraw;

[Tool]
public partial class MouseDraw: Node2D
{
    [Export]
    public DrawingScene Parent;
    public bool DrawingActive = false;
    public bool DrawMode = false;
    public bool OnScreen = false;
    public bool DrawingTool = false;

    private SubViewport CanvasViewport = EditorInterface.Singleton.GetEditorViewport2D();
    private Control CanvasContainerParent;

    public override void _Ready()
    {
        CanvasContainerParent = ((SubViewportContainer)CanvasViewport.GetParent()).GetParentControl();
    }

    public override void _Draw()
    {
        float radius = Parent.Brush.Size*CanvasViewport.GlobalCanvasTransform.X.X/2f;
        DrawCircle(Vector2.Zero, radius, Parent.Brush.Color, false, 1, true);
    }

    public override void _Process(double delta)
    {
        GlobalPosition = GetGlobalMousePosition();
        OnScreen = CanvasContainerParent.GetGlobalRect().HasPoint(GlobalPosition);
        Visible = DrawingActive && DrawMode && OnScreen && DrawingTool;
        if(Visible)
            QueueRedraw();
    }

}
#endif