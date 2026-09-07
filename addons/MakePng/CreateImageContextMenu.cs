#if TOOLS
using Godot;

namespace MakePng;

[Tool]
public partial class CreateImageContextMenu : EditorContextMenuPlugin
{
	public NewImageDialog Dialog;

	public override void _PopupMenu(string[] paths)
	{
		string folder = paths.Length > 0 ? paths[0] : EditorInterface.Singleton.GetCurrentDirectory();
		AddContextMenuItem("Image...", Callable.From<Godot.Collections.Array>(_ => Dialog.OpenFor(folder)), null);
	}
}
#endif
