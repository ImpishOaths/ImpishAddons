#if TOOLS
using Godot;
using System.Collections.Generic;

namespace ImpishDraw;

[Tool]
public partial class DrawingScene: Control, ISerializationListener
{
    private Button DrawButton;
    private Button SaveButton;

    private Slider SizeSlider;
    private Label SizeReadout;
    private Slider SoftSlider;
    private Label SoftReadout;

    private ColorPickerButton ColorSelectButton;
    private TextureButton[] PaletteButtons = new TextureButton[5];

    private Button BrushToolButton;
    private Button SelectToolButton;
    private Button FillToolButton;
    [Export]
    private ButtonGroup ToolButtonGroup;

    private ReferenceRect ActiveReference;
    private MouseDraw MouseDraw;

    private SelectionDrawing SelectionDrawing;

    [Export]
    public BrushResource Brush;

    private CheckBox FilterBox;
    private CheckBox GridBox;
    private CheckBox MaterialBox;
    private CheckBox TopBox;

    private void OnReady()
    {
        DrawButton = GetNode<Button>("%DrawButton");
        SaveButton = GetNode<Button>("%SaveButton");

        SizeSlider = GetNode<Slider>("%SizeSlider");
        SizeReadout = GetNode<Label>("%SizeReadout");
        SoftSlider = GetNode<Slider>("%SoftSlider");
        SoftReadout = GetNode<Label>("%SoftReadout");

        ColorSelectButton = GetNode<ColorPickerButton>("%ColorSelectButton");
        PaletteButtons[0] = GetNode<TextureButton>("%PaletteButton1");
        PaletteButtons[1] = GetNode<TextureButton>("%PaletteButton2");
        PaletteButtons[2] = GetNode<TextureButton>("%PaletteButton3");
        PaletteButtons[3] = GetNode<TextureButton>("%PaletteButton4");
        PaletteButtons[4] = GetNode<TextureButton>("%PaletteButton5");

        BrushToolButton = GetNode<Button>("%BrushToolButton");
        SelectToolButton = GetNode<Button>("%SelectToolButton");
        FillToolButton = GetNode<Button>("%FillToolButton");

        ActiveReference = GetNode<ReferenceRect>("%ActiveReference");

        MouseDraw = GetNode<MouseDraw>("%MouseDraw");
        SelectionDrawing = GetNode<SelectionDrawing>("%SelectionDrawing");

        FilterBox = GetNode<CheckBox>("%FilterBox");
        MaterialBox = GetNode<CheckBox>("%MaterialBox");
        TopBox = GetNode<CheckBox>("%TopBox");
    }

    public override void _Ready()
    {
        OnReady();
        
        var defaultPalette = (Godot.Collections.Array<Color>)ProjectSettings.Singleton.GetSetting("Drawing/DefaultPalette");

        for(int i = 0; i < 5; ++i)
            PaletteButtons[i].Modulate = defaultPalette[i];
        MouseDraw.Parent = this;
        ToolButtonGroup.Pressed += CheckMouseDraw;
        CheckMouseDraw(ToolButtonGroup.GetPressedButton());

        OnSceneChanged(EditorInterface.Singleton.GetEditedSceneRoot());

        UpdateBrush();
        SetColorFromPalette(0);
    }

    public void OnSceneChanged(Node root)
    {
        if(root == null)
            return;
        SelectionDrawing.Reparent(root);
        SelectionDrawing.GlobalPosition = Vector2.Zero;
        SelectionDrawing.QueueRedraw();
    }

    private int SelectedPalette = 0;
    public void SetColorFromPalette(int value)
    {
        if(SelectedTool != null)
            return;
        SelectedPalette = value;
        Brush.Color = PaletteButtons[value].Modulate;
        UpdateBrushVisuals();
    }
    public void UpdateBrush()
    {
        Brush.Size = (float)SizeSlider.Value;
        Brush.Softness = (float)SoftSlider.Value;
        Brush.Color = ColorSelectButton.Color;
        UpdateBrushVisuals();
    }
    private void UpdateBrushVisuals()
    {
        SizeSlider.SetValueNoSignal(Brush.Size);
        SizeReadout.Text = $"{Brush.Size:00.0}";
        SoftSlider.SetValueNoSignal(Brush.Softness);
        SoftReadout.Text = $"{Brush.Softness:00.0}";
        ColorSelectButton.Color = Brush.Color;
    }
    private void ModifyPaletteColor(Color color)
    {
        Brush.Color = color;
        PaletteButtons[SelectedPalette].Modulate = color;
        UpdateBrushVisuals();
    }

    public DrawingSelectable SelectedNode {get; private set;}
    public void SetSelectedNode(DrawingSelectable selectable)
    {
        if(selectable.Texture == null || selectable.Equals(SelectedNode))
            return;
        bool wasDrawing = DrawingActive;
        if(SelectedNode != null)
            ClearSelectedNode();
        SelectedNode = selectable;
        if(wasDrawing)
            SetDrawingActive(true);

    }
    public void ClearSelectedNode()
    {
        SetDrawingActive(false);
        SelectedNode = null;
    }

    public bool DrawingActive {get; private set;} = false;
    public void SetDrawingActive(bool active, bool saving = false)
    {
        if(active && SelectedNode == null)
            return;
        if(active == DrawingActive)
            return;
        DrawingActive = active;
        if(DrawingActive && ! ManualEditorSelectionControl)
        {
            if(! saving)
            {
                UndoRedo.CreateAction("Started Drawing");
                UndoRedo.AddDoMethod(this, "SelectAndStartDrawing", SelectedNode.Node);
                UndoRedo.AddUndoMethod(this, "StopDrawingSelected");
                UndoRedo.CommitAction();
            }
            else
            {
                SelectAndStartDrawing(SelectedNode.Node);
            }
        }
        else
        {
            if(! saving)
            {
                UndoRedo.CreateAction("Stopped Drawing");
                UndoRedo.AddDoMethod(this, "StopDrawingSelected");
                UndoRedo.AddUndoMethod(this, "SelectAndStartDrawing", SelectedNode.Node);
                UndoRedo.CommitAction();
            }
            else
            {
                StopDrawingSelected();
            }
        }
    }
    private void UpdateDrawVisible()
    {
        ActiveReference.Visible = DrawingActive;
        DrawButton.SetPressedNoSignal(DrawingActive);
        MouseDraw.DrawingActive = DrawingActive;

    }

    [Export]
    private Texture2D OriginalTexture;
    private readonly Dictionary<string, Image> ImagesHistory = [];
    private readonly HashSet<string> DirtyImages = [];
    private ImageTexture DrawingTexture;
    private Image DrawingImage;
    private Image ClipboardImage;
    private void StartDrawingSelected()
    {
        OriginalTexture = SelectedNode.Texture;
        DrawingTexture ??= new();
        DrawingImage ??= new();
        
        StoredMaterial = SelectedNode.Material;
        WasFiltering = SelectedNode.Filter;
        StoredOrder = SelectedNode.Order;
        
        ToggleFilter(FilterBox.ButtonPressed);
        ToggleMaterial(MaterialBox.ButtonPressed);
        ToggleTop(TopBox.ButtonPressed);

        if(ImagesHistory.TryGetValue(OriginalTexture.ResourcePath, out var image))
        {
            DrawingImage.CopyFrom(image);
            DrawingTexture.SetImage(DrawingImage);
        }
        else
        {
            image = OriginalTexture.GetImage();
            image.Decompress();
            ImagesHistory.Add(OriginalTexture.ResourcePath, image);
            DrawingImage.CopyFrom(image);
            DrawingTexture.SetImage(DrawingImage);
        }
        SelectedNode.Texture = DrawingTexture;
        UpdateDrawVisible();
    }
    private void MarkDirty()
    {
        DirtyImages.Add(OriginalTexture.ResourcePath);
        SaveButton.Text = "Save*";
    }
    private void StopDrawingSelected()
    {
        SelectedNode.Filter = WasFiltering;
        SelectedNode.Material = StoredMaterial;
        SelectedNode.Order = StoredOrder;

        if(SelectedTool != null)
        {
            CommitSelectedTool();
            StopSelectionVisuals();
            SelectedTool = null;
        }
        QueueFileSave();
        DrawingActive = false;
        SelectedNode.Texture = OriginalTexture;
        OriginalTexture = null;
        UpdateDrawVisible();
    }

    private void CommitSelectedTool()
    {
        Vector2 pos = SelectedNode.GetTexturePosFromMouse();
        Rect2I region = (Rect2I)SelectedNode.GetRegion();

        while(SelectedTool != null)
        {
            if(! SelectedTool.Began)
            {
                var rightClick = new InputEventMouseButton { ButtonIndex = MouseButton.Right, Pressed = true };
                SelectedTool.Begin(pos, DrawingImage, DrawingTexture, region, Brush, rightClick);
                SelectedTool.Step(pos);
            }
            else if(SelectedTool is MoveTool mover)
            {
                mover.RightDown = true;
            }

            var (Output, Region, NextTool) = SelectedTool.End();
            if(SelectedTool.Edited)
            {
                DrawingImage.BlitRect(Output, Region, Region.Position);
                CommitDrawing(SelectedTool.Name, Region, NextTool);
            }
            else
            {
                SelectedTool = NextTool;
            }
        }
    }

    private void CopySelection(MoveTool mover, Rect2I region)
    {
        Rect2I bbox = SelectionHelper.GetPolygonBounds(mover.GetSelection()).Intersection(region);
        if(! bbox.HasArea())
            return;
        ClipboardImage = DrawingImage.GetRegion(bbox);
    }

    private void CutSelection(MoveTool mover, Rect2I region)
    {
        Rect2I bbox = SelectionHelper.GetPolygonBounds(mover.GetSelection()).Intersection(region);
        if(! bbox.HasArea())
            return;
        ClipboardImage = DrawingImage.GetRegion(bbox);

        var baseImage = ImagesHistory[OriginalTexture.ResourcePath];
        var undoImage = baseImage.GetRegion(bbox);
        var changeImage = Image.CreateEmpty(bbox.Size.X, bbox.Size.Y, false, baseImage.GetFormat());

        UndoRedo.CreateAction("Cut Selection");
        UndoRedo.AddDoMethod(this, "DoTextureChange", baseImage, changeImage, bbox.Position);
        AddSetToolStep(true, null);
        AddSetToolStep(false, mover);
        UndoRedo.AddUndoMethod(this, "DoTextureChange", baseImage, undoImage, bbox.Position);
        UndoRedo.CommitAction();
    }

    private void PasteSelection(Rect2I region)
    {
        CommitSelectedTool();

        Vector2I pasteSize = ClipboardImage.GetSize();
        Vector2I pastePosition = region.Position + (region.Size - pasteSize) / 2;

        Image cutoutImage = new();
        cutoutImage.CopyFrom(DrawingImage);

        Image selectionImage = Image.CreateEmpty(DrawingImage.GetSize().X, DrawingImage.GetSize().Y, false, DrawingImage.GetFormat());
        selectionImage.BlitRect(ClipboardImage, new Rect2I(Vector2I.Zero, pasteSize), pastePosition);

        Vector2[] pasteRegion = [
            (Vector2)pastePosition,
            (Vector2)pastePosition + Vector2.Right*pasteSize.X,
            (Vector2)(pastePosition + pasteSize),
            (Vector2)pastePosition + Vector2.Down*pasteSize.Y,
            (Vector2)pastePosition
        ];
        var pasteTool = new MoveTool(pasteRegion, cutoutImage, selectionImage);

        DrawingImage.CopyFrom(cutoutImage);
        DrawingImage.BlendRect(selectionImage, new Rect2I(Vector2I.Zero, DrawingImage.GetSize()), Vector2I.Zero);
        DrawingTexture.Update(DrawingImage);

        UndoRedo.CreateAction("Paste");
        AddSetToolStep(true, pasteTool);
        AddSetToolStep(false, null);
        UndoRedo.CommitAction();
    }

    private EditorSelection EditorSelection = null;
    private bool ManualEditorSelectionControl = false;
    private void SelectAndStartDrawing(Node toSelect)
    {
        DrawingActive = true;
        EditorSelection ??= EditorInterface.Singleton.GetSelection();
        if(! EditorSelection.GetSelectedNodes().Contains(toSelect))
        {
            EditorSelection.Clear();
            EditorSelection.AddNode(toSelect);
            SelectedNode = DrawingSelectable.MakeSelectable(toSelect);
        }
        StartDrawingSelected();
    }

    public bool SaveImagesToOrigin()
    {
        bool saved = false;
        foreach(var path in DirtyImages)
        {
            var image = ImagesHistory[path];
            image.SavePng(path);
            saved = true;
        }
        if(saved)
        {
            EditorInterface.Singleton.GetResourceFilesystem().ReimportFiles([..DirtyImages]);
            DirtyImages.Clear();
        }

        SaveButton.Text = "Save";
        return saved;
    }

    private MouseButton SavedInput;
    public bool PassInput(InputEvent ev)
    {
        if(! DrawingActive)
            return false;

        MouseDraw.DrawMode = true;
        
        bool handled = false;
        Vector2 pos = SelectedNode.GetTexturePosFromMouse();
        Rect2I region = (Rect2I)SelectedNode.GetRegion();
        switch(ev)
        {
            case InputEventKey key:
                if(! key.Pressed)
                    break;
                switch(key.Keycode)
                {
					case Key.Key1:
					case Key.Key2:
					case Key.Key3:
					case Key.Key4:
					case Key.Key5:
                        int paletteColor = (int)(key.Keycode - Key.Key1);
                        SetColorFromPalette(paletteColor);
                        handled = true;
                        break;
                    case Key.C:
                        if(key.CtrlPressed)
                            break;
                        if(SelectedTool is MoveTool copyMover && ! copyMover.Began)
                        {
                            CopySelection(copyMover, region);
                            handled = true;
                        }
                        break;
                    case Key.X:
                        if(key.CtrlPressed)
                            break;
                        if(SelectedTool is MoveTool cutMover && ! cutMover.Began)
                        {
                            CutSelection(cutMover, region);
                            handled = true;
                        }
                        break;
                    case Key.V:
                        if(key.CtrlPressed)
                            break;
                        if(ClipboardImage != null)
                        {
                            PasteSelection(region);
                            handled = true;
                        }
                        break;
                    default:
                        break;
                }
                break;
            case InputEventMouseMotion:
                if(SelectedTool != null && SelectedTool.Began)
                {
                    SelectedTool.Step(pos);
                    UpdateSelectionVisuals();
                }
                break;
            case InputEventMouseButton mb:
                if(SelectedTool != null && SelectedTool.Began)
                    handled = SelectedTool.Interrupt(mb);
                if(mb.Pressed)
                {
                    handled = true;
                    switch(mb.ButtonIndex)
                    {
                        case MouseButton.Right:
                        case MouseButton.Left:
                            if(SelectedTool != null && SelectedTool.Began)
                                break;
                            SelectedTool ??= GetSelectedTool();
                            SavedInput = mb.ButtonIndex;
                            SelectedTool.Begin(pos, DrawingImage, DrawingTexture, region, Brush, mb);
                            SelectedTool.Step(pos);
                            StartSelectionVisuals();
                            UpdateSelectionVisuals();
                            break;
                        case MouseButton.WheelUp:
                            if(mb.CtrlPressed)
                                ModifyBrush(false, mb.ShiftPressed);
                            break;
                        case MouseButton.WheelDown:
                            if(mb.CtrlPressed)
                                ModifyBrush(true, mb.ShiftPressed);
                            break;
                        default:
                            handled = false;
                            break;
                    }
                }
                else
                {
                    if(SelectedTool != null && mb.ButtonIndex == SavedInput)
                    {
                        var (Output, Region, NextTool) = SelectedTool.End();
                        StopSelectionVisuals();
                        if(SelectedTool.Edited)
                        {
                            DrawingImage.BlitRect(Output, Region, Region.Position);
                            CommitDrawing(SelectedTool.Name, Region, NextTool);
                        }
                        else if(NextTool is MoveTool)
                        {
                            UndoRedo.CreateAction(SelectedTool.Name);
                            AddSetToolStep(true, NextTool);
                            AddSetToolStep(false, null);
                            UndoRedo.CommitAction();
                        }
                        else
                        {
                            SelectedTool = NextTool;
                            if(SelectedTool != null)
                            {
                                StartSelectionVisuals();
                                UpdateSelectionVisuals();
                            }
                        }
                    }
                }
                break;
        }
        return handled;
    }

    private void StartSelectionVisuals()
    {
        if(! SelectedTool.ShowSelection)
        {
            SelectionDrawing.Visible = false;
            return;
        }
        SelectionDrawing.Reparent(SelectedNode.Node);
        SelectionDrawing.Transform = Transform2D.Identity;
        SelectionDrawing.Position = SelectedNode.GetTextureOriginOffset() - SelectedNode.GetRegion().Position;
        SelectionDrawing.Visible = true;
    }

    private void UpdateSelectionVisuals()
    {
        if(! SelectedTool.ShowSelection)
            return;
        SelectionDrawing.SetPoints(SelectedTool.GetSelection());
    }

    private void StopSelectionVisuals()
    {
        SelectionDrawing.Reparent(this);
        SelectionDrawing.Visible = false;
    }

    private void ModifyBrush(bool negative, bool shift)
    {
        float mod = negative ? -1 : 1;

        if(shift)
        {
            Brush.Softness = (float)Mathf.Clamp(Brush.Softness + 0.5f*mod, SoftSlider.MinValue, SoftSlider.MaxValue);
        }
        else
        {
            Brush.Size = (float)Mathf.Clamp(Brush.Size + 0.5f*mod, SizeSlider.MinValue, SizeSlider.MaxValue);
        }
        UpdateBrushVisuals();
    }    

    private readonly EditorUndoRedoManager UndoRedo = EditorInterface.Singleton.GetEditorUndoRedo();
    private void CommitDrawing(string message, Rect2I region, DrawingTool nextTool)
    {
        var baseImage = ImagesHistory[OriginalTexture.ResourcePath];
        var changeImage = DrawingImage.GetRegion(region);
        var undoImage = baseImage.GetRegion(region);
        DrawingTool previousTool = SelectedTool is MoveTool ? SelectedTool : null;
        UndoRedo.CreateAction(message);
        UndoRedo.AddDoMethod(this, "DoTextureChange", baseImage, changeImage, region.Position);
        AddSetToolStep(true, nextTool);
        AddSetToolStep(false, previousTool);
        UndoRedo.AddUndoMethod(this, "DoTextureChange", baseImage, undoImage, region.Position);
        UndoRedo.CommitAction();
    }

    private void AddSetToolStep(bool doStep, DrawingTool tool)
    {
        if(tool is MoveTool mover)
        {
            if(doStep)
                UndoRedo.AddDoMethod(this, "SetMoveTool", mover.SelectedRegion, mover.CutoutImage, mover.SelectionImage, mover.InitialTransform);
            else
                UndoRedo.AddUndoMethod(this, "SetMoveTool", mover.SelectedRegion, mover.CutoutImage, mover.SelectionImage, mover.InitialTransform);
        }
        else
        {
            if(doStep)
                UndoRedo.AddDoMethod(this, "SetTool");
            else
                UndoRedo.AddUndoMethod(this, "SetTool");
        }
    }

    private void SetTool()
    {
        ApplySelectedTool(null);
    }

    private void SetMoveTool(Vector2[] region, Image cutout, Image selection, Transform2D initialTransform)
    {
        ApplySelectedTool(new MoveTool(region, cutout, selection, initialTransform));
    }

    private void ApplySelectedTool(DrawingTool tool)
    {
        SelectedTool = tool;
        if(SelectedTool != null)
        {
            StartSelectionVisuals();
            UpdateSelectionVisuals();
        }
        else
        {
            StopSelectionVisuals();
        }
    }

    private void DoTextureChange(Image baseImage, Image changeImage, Vector2I position)
    {
        baseImage.BlitRect(changeImage, new(Vector2I.Zero, changeImage.GetSize()), position);
        DrawingImage.CopyFrom(baseImage);
        DrawingTexture.Update(DrawingImage);
        EditorInterface.Singleton.MarkSceneAsUnsaved();
        MarkDirty();
    }

    private bool _QueueFileSave = false;
    private void QueueFileSave()
    {
        _QueueFileSave = true;
    }
    public override void _Process(double delta)
    {
        if(_QueueFileSave)
        {
            _QueueFileSave = false;
            SaveImagesToOrigin();
        }
    }
    
    private DrawingTool SelectedTool;
    private enum DrawingToolEnum
    {
        BRUSH,
        SELECT,
        LINE,
        FILL,
    }

    private BaseButton PreviousToolButton;
    private bool _ApplyingToolButton = false;
    public void CheckMouseDraw(BaseButton pressed)
    {
        MouseDraw.DrawingTool = pressed.GetMeta("DrawMouse").As<bool>();
        if(_ApplyingToolButton)
            return;
        if(PreviousToolButton != null && PreviousToolButton != pressed)
        {
            if(SelectedTool is MoveTool)
                CommitSelectedTool();
            UndoRedo.CreateAction("Switch Tool");
            UndoRedo.AddDoMethod(this, "PressToolButton", pressed);
            UndoRedo.AddUndoMethod(this, "PressToolButton", PreviousToolButton);
            UndoRedo.CommitAction();
        }
        PreviousToolButton = pressed;
    }

    private void PressToolButton(BaseButton button)
    {
        _ApplyingToolButton = true;
        button.ButtonPressed = true;
        _ApplyingToolButton = false;
        PreviousToolButton = button;
    }

    public DrawingTool GetSelectedTool()
    {
        var index = (DrawingToolEnum)ToolButtonGroup.GetPressedButton().GetMeta("ToolIndex").As<int>();
        return index switch
        {
            DrawingToolEnum.BRUSH => new BrushTool(),
            DrawingToolEnum.LINE => new LineTool(),
            DrawingToolEnum.SELECT => new SelectorTool(),
            DrawingToolEnum.FILL => new FillTool(),
            _ => null
        };
    }

    public void ClearCache()
    {
        if(DrawingActive)
            return;
        ImagesHistory.Clear();
    }

    public void OnBeforeSerialize()
    {
        ToolButtonGroup.Pressed -= CheckMouseDraw;
    }

    public void OnAfterDeserialize()
    {
        ToolButtonGroup.Pressed += CheckMouseDraw;
    }

    private Material StoredMaterial;
    private bool WasFiltering;
    private const int MaxOrder = 256;
    private int StoredOrder;
    public void ToggleFilter(bool filter)
    {
        if(! DrawingActive)
            return;

        FilterBox.SetPressedNoSignal(filter);
        SelectedNode.Filter = filter;
    }
    public void ToggleMaterial(bool material)
    {
        if(! DrawingActive)
            return;

        MaterialBox.SetPressedNoSignal(material);
        if(material)
            SelectedNode.Material = StoredMaterial;
        else
            SelectedNode.Material = null;
    }
    public void ToggleTop(bool top)
    {
        if(! DrawingActive)
            return;

        TopBox.SetPressedNoSignal(top);
        if(top)
            SelectedNode.Order = MaxOrder;
        else
            SelectedNode.Order = StoredOrder;
    }

}

public interface DrawingSelectable
{
    public bool Equals(DrawingSelectable other);
    public Node Node {get;}
    public Texture2D Texture {get; set;}
    public Rect2 GetRegion();
    public Vector2 GetTexturePosFromMouse();
    public Vector2 GetTextureOriginOffset();
    public static DrawingSelectable MakeSelectable(Node node)
    {
        if(node == null)
            return null;
        if(node is Sprite2D sprite)
            return new SpriteSelectable(sprite);
        if(node is TextureRect rect)
            return new TextureRectSelectable(rect);
        return null;
    }
    public Material Material {get; set;}
    public bool Filter {get; set;}
    public int Order {get; set;}
}

public class SpriteSelectable(Sprite2D sprite): DrawingSelectable
{
    private Sprite2D Sprite = sprite;
    public bool Equals(DrawingSelectable other) => other is SpriteSelectable sprite && sprite.Sprite == Sprite;
    public Node Node => Sprite;
    public Texture2D Texture
    {
        get => Sprite.Texture;
        set => Sprite.Texture = value;
    }
    public Rect2 GetRegion()
    {
        if(Sprite.RegionEnabled)
            return Sprite.RegionRect;
        return new Rect2(Vector2.Zero, Sprite.Texture.GetSize());
    }
    public Vector2 GetTexturePosFromMouse()
    {
        Vector2 pos = Sprite.GetLocalMousePosition();
        pos -= Sprite.Offset;
        Vector2 size = GetRegion().Size;
        if(Sprite.Centered)
            pos += size/2f;
        if(Sprite.FlipH)
            pos.X = size.X-pos.X;
        if(Sprite.FlipV)
            pos.Y = size.Y-pos.Y;
        pos += Sprite.RegionEnabled ? Sprite.RegionRect.Position : Vector2.Zero;
        return pos;
    }
    public Vector2 GetTextureOriginOffset()
    {
        Vector2 offset = Sprite.Offset;
        if(Sprite.Centered)
        {
            Vector2 size = GetRegion().Size;
            offset -= size/2f;
        }
        return offset;
    }
    public Material Material
    {
        get => Sprite.Material;
        set => Sprite.Material = value;
    }
    public bool Filter
    {
        get => Sprite.TextureFilter != CanvasItem.TextureFilterEnum.Nearest;
        set => Sprite.TextureFilter = value ? CanvasItem.TextureFilterEnum.ParentNode : CanvasItem.TextureFilterEnum.Nearest;
    }
    public int Order
    {
        get => Sprite.ZIndex;
        set => Sprite.ZIndex = value;
    }
}

public class TextureRectSelectable(TextureRect rect): DrawingSelectable
{
    private TextureRect Rect = rect;
    public bool Equals(DrawingSelectable other) => other is TextureRectSelectable texRect && texRect.Rect == Rect;
    public Node Node => Rect;
    public Texture2D Texture
    {
        get => Rect.Texture;
        set => Rect.Texture = value;
    }
    public Rect2 GetRegion()
    {
        return new Rect2(Vector2.Zero, Rect.Texture.GetSize());
    }
    public Vector2 GetTexturePosFromMouse()
    {
        Vector2 pos = Rect.GetLocalMousePosition();
        Vector2 size = Rect.Texture.GetSize();
        pos *= size/Rect.Size;
        if(Rect.FlipH)
            pos.X = size.X-pos.X;
        if(Rect.FlipV)
            pos.Y = size.Y-pos.Y;
        pos -= Vector2.One * 0.5f;
        return pos;
    }
    public Vector2 GetTextureOriginOffset()
    {
        return Vector2.Zero;
    }
    
    public Material Material
    {
        get => Rect.Material;
        set => Rect.Material = value;
    }
    public bool Filter
    {
        get => Rect.TextureFilter != CanvasItem.TextureFilterEnum.Nearest;
        set => Rect.TextureFilter = value ? CanvasItem.TextureFilterEnum.ParentNode : CanvasItem.TextureFilterEnum.Nearest;
    }
    public int Order
    {
        get => Rect.ZIndex;
        set => Rect.ZIndex = value;
    }
}

#endif