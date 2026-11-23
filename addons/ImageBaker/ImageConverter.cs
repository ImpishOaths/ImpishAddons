using Godot;
using System;

namespace ImageBaker;

[Tool]
public abstract partial class ImageConverter : Resource
{
	public abstract Image Convert(Image image);
}
