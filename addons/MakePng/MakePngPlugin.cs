#if TOOLS
using Godot;

namespace MakePng;

[Tool]
public partial class MakePngPlugin : EditorPlugin
{
	private NewImageDialog _dialog;
	private CreateImageContextMenu _contextMenu;

	public override void _EnterTree()
	{
		_dialog = new NewImageDialog();
		EditorInterface.Singleton.GetBaseControl().AddChild(_dialog);

		_contextMenu = new CreateImageContextMenu { Dialog = _dialog };
		AddContextMenuPlugin(EditorContextMenuPlugin.ContextMenuSlot.FilesystemCreate, _contextMenu);
	}

	public override void _ExitTree()
	{
		RemoveContextMenuPlugin(_contextMenu);
		_contextMenu = null;

		_dialog.QueueFree();
		_dialog = null;
	}
}
#endif
