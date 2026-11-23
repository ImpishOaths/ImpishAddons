using Godot;
using System;

namespace ImageBaker;

[Tool]
[GlobalClass]
public partial class SmoothingBake : ImageConverter
{
    public override Image Convert(Image image)
	{
		Vector2I size = image.GetSize();
		image.Resize(size.X*2, size.Y*2, Image.Interpolation.Bilinear);
		image.Resize(size.X, size.Y, Image.Interpolation.Bilinear);
		return image;
	}
}
