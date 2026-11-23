using Godot;
using System;

namespace ImageBaker;

[Tool]
[GlobalClass]
public partial class PuppetShaderBake : ImageConverter
{
	public const float BlackWhiteCutoff = 0.33f;
	public const float AlphaCutoff = 0.66f;

	[Export]
	public Texture2D GradientTexture;
	[Export]
	public int Resize = 2;

    public override Image Convert(Image image)
	{
		var size = image.GetSize();
		size *= Resize;
		image.Resize(size.X, size.Y, Image.Interpolation.Bilinear);
		Image output = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
		Image gradientImage = GradientTexture.GetImage();
		for(int x = 0; x < size.X; ++x)
		{
			for(int y = 0; y < size.Y; ++y)
			{
				Color inputColor = image.GetPixel(x,y);
				Color outputColor;
				if(inputColor.A < AlphaCutoff)
					outputColor = new Color(0, 0, 0, 0);
				else
				{
					Vector2 uv = new((float)x/size.X, (float)y/size.Y);
					uv.X /= 5;
					float region = ChooseGradientRegion(inputColor);
					uv.X += region;
					uv *= gradientImage.GetSize();
					outputColor = gradientImage.GetPixelv((Vector2I)uv);
				}

				output.SetPixel(x, y, outputColor);
			}
		}
		//size /= Resize;
		//output.Resize(size.X, size.Y, Image.Interpolation.Cubic);
		return output;
	}

	private static float ChooseGradientRegion(Color color)
	{
		int cuttoffCheck = (color.R > BlackWhiteCutoff ? 1 : 0) + (color.G > BlackWhiteCutoff ? 1 : 0) + (color.B > BlackWhiteCutoff ? 1 : 0);
		if(cuttoffCheck == 0)
			return 0f;
		if(cuttoffCheck == 3)
			return 0.8f;
		
		float maxColor = color.R;
		float maxIndex = 0.2f;
		if(color.G > maxColor)
		{
			maxColor = color.G;
			maxIndex = 0.4f;
		}
		if(color.B > maxColor)
		{
			maxIndex = 0.6f;
		}
		return maxIndex;
	}

}
