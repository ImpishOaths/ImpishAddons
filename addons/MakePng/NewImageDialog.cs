#if TOOLS
using Godot;

namespace MakePng;

[Tool]
public partial class NewImageDialog : ConfirmationDialog
{
	private LineEdit _nameEdit;
	private SpinBox _widthSpin;
	private SpinBox _heightSpin;
	private Label _errorLabel;

	private string _targetFolder = "res://";

	public override void _Ready()
	{
		Title = "Create New Image";
		OkButtonText = "Create";
		DialogHideOnOk = false;
		MinSize = new Vector2I(320, 0);

		var root = new VBoxContainer();
		root.AddThemeConstantOverride("separation", 8);
		AddChild(root);

		_nameEdit = new LineEdit { Text = "NewImage" };
		AddRow(root, "Name:", _nameEdit);

		_widthSpin = new SpinBox { MinValue = 1, MaxValue = 8192, Value = 64 };
		AddRow(root, "Width:", _widthSpin);

		_heightSpin = new SpinBox { MinValue = 1, MaxValue = 8192, Value = 64 };
		AddRow(root, "Height:", _heightSpin);

		_errorLabel = new Label { Visible = false, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		_errorLabel.AddThemeColorOverride("font_color", new Color(1f, 0.45f, 0.45f));
		root.AddChild(_errorLabel);

		RegisterTextEnter(_nameEdit);
		Confirmed += OnConfirmed;
	}

	private static void AddRow(VBoxContainer root, string labelText, Control control)
	{
		var row = new HBoxContainer();
		row.AddChild(new Label { Text = labelText, CustomMinimumSize = new Vector2(60, 0) });
		control.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		row.AddChild(control);
		root.AddChild(row);
	}

	public void OpenFor(string folder)
	{
		_targetFolder = string.IsNullOrEmpty(folder) ? "res://" : folder;
		_nameEdit.Text = "NewImage";
		_widthSpin.Value = 64;
		_heightSpin.Value = 64;
		_errorLabel.Visible = false;

		PopupCentered();
		_nameEdit.GrabFocus();
		_nameEdit.SelectAll();
	}

	private void OnConfirmed()
	{
		string name = _nameEdit.Text.Trim();
		if (string.IsNullOrEmpty(name))
		{
			ShowError("Name cannot be empty.");
			return;
		}
		if (name.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
		{
			ShowError("Name cannot contain / \\ or :");
			return;
		}

		int width = (int)_widthSpin.Value;
		int height = (int)_heightSpin.Value;

		string folder = _targetFolder.TrimSuffix("/");
		string path = $"{folder}/{name}.png";

		if (FileAccess.FileExists(path))
		{
			ShowError($"\"{name}.png\" already exists in this folder.");
			return;
		}

		using var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
		Error err = image.SavePng(path);

		if (err != Error.Ok)
		{
			ShowError($"Failed to create image: {err}");
			return;
		}

		var fileSystem = EditorInterface.Singleton.GetResourceFilesystem();
		fileSystem.UpdateFile(path);
		fileSystem.ReimportFiles(new[] { path });
		EditorInterface.Singleton.GetFileSystemDock().NavigateToPath(path);

		Hide();
	}

	private void ShowError(string message)
	{
		_errorLabel.Text = message;
		_errorLabel.Visible = true;
	}
}
#endif
