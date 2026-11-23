#if TOOLS
using Godot;
using Godot.Collections;

namespace ImpishDraw;

[Tool]
public partial class ImpishDrawPlugin : EditorPlugin
{
	private PackedScene DrawingScenePacked = ResourceLoader.Load<PackedScene>("res://addons/ImpishDraw/DrawingScene.tscn");
	private DrawingScene DrawingScene;
	private EditorDock DrawingDock;

	private Viewport Viewport;

	public override void _EnterTree()
	{
		if(! ProjectSettings.Singleton.HasSetting("Drawing/DefaultPalette"))
        {
            ProjectSettings.Singleton.SetSetting("Drawing/DefaultPalette", new Array<Color>() {Colors.Black, Colors.Red, Colors.Green, Colors.Blue, Colors.White});
        }

		DrawingScene = DrawingScenePacked.Instantiate<DrawingScene>();
		AddControlToContainer(CustomControlContainer.CanvasEditorBottom, DrawingScene);
		DrawingScene.Visible = false;

		Viewport = GetViewport();
	}
	public override void _ExitTree()
	{
		RemoveControlFromContainer(CustomControlContainer.CanvasEditorBottom, DrawingScene);
		DrawingScene.QueueFree();
	}

	public void OnSceneChanged(Node root)
	{
		DrawingScene.OnSceneChanged(root);
	}

    public override bool _Handles(GodotObject obj)
	{
		if(obj is not Node node)
			return false;

		var selected = DrawingSelectable.MakeSelectable(node);
		if(selected == null || selected.Texture == null)
		{
			DrawingScene.ClearSelectedNode();
			return false;
		}
		DrawingScene.SetSelectedNode(selected);
		return true;
	}

    public override void _MakeVisible(bool visible)
	{
		if(! visible)
			DrawingScene.SetDrawingActive(false);
		DrawingScene.Visible = visible;
	}

    public override bool _ForwardCanvasGuiInput(InputEvent ev)
    {
		if(! DrawingScene.DrawingActive)
			return false;
        return DrawingScene.PassInput(ev);
    }

	private bool ResumeDrawingAfterSave = false;
	public override void _ApplyChanges()
	{
		if(! DrawingScene.DrawingActive)
			return;
		ResumeDrawingAfterSave = true;
		DrawingScene.SetDrawingActive(false, true);
	}

    public override void _SaveExternalData()
	{
		if(! ResumeDrawingAfterSave)
			return;
		ResumeDrawingAfterSave = false;
		DrawingScene.SetDrawingActive(true, true);
	}

    public override void _Input(InputEvent ev)
    {
        switch(ev)
		{
			case InputEventKey key:
				if(! key.Pressed)
					break;
				switch(key.Keycode)
				{
					case Key.Quoteleft:
						if(DrawingScene.DrawingActive)
						{
							DrawingScene.SetDrawingActive(false);
						}
						else
						{
							if(DrawingScene.Visible)
								DrawingScene.SetDrawingActive(true);
						}
						break;
					case Key.Key1:
					case Key.Key2:
					case Key.Key3:
					case Key.Key4:
					case Key.Key5:
						if(DrawingScene.PassInput(ev))
						{
							Viewport.SetInputAsHandled();
						}
						break;
					default:
						break;
				}
				break;
			case InputEventMouseButton mb:
				if(! mb.Pressed)
					break;
				if((mb.ButtonIndex == MouseButton.WheelUp || mb.ButtonIndex == MouseButton.WheelDown) && mb.CtrlPressed)
				{
					if(DrawingScene.PassInput(ev))
					{
						Viewport.SetInputAsHandled();
					}
				}
				break;
		}
    }
}
#endif
