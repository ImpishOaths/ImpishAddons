using Godot;
using System;

namespace ImageBaker;

[Tool]
[GlobalClass]
public partial class ImageBaker : Node
{
	[Export]
	public Texture2D Input;
	[Export]
	public ImageConverter Converter;
	[ExportToolButton("Apply Conversion")]
	public Callable Bake => Callable.From(ApplyConversion);
	public void ApplyConversion()
	{
		if(Input == null || Converter == null)
			return;
		Image image = Input.GetImage();
		Image output = Converter.Convert((Image)image.Duplicate());
		Output = ImageTexture.CreateFromImage(output);
	}
	[Export]
	public Texture2D Output;
    public override void _Notification(int what)
	{
		if(what == NotificationEditorPreSave)
		{
			Input = null;
			Output = null;
		}
	}
}
