using Godot;
using System;

namespace ImageBaker;

[Tool]
[GlobalClass]
public partial class NormalBake : ImageConverter
{
	[Export]
	public float AlphaCutoff = 0.5f;
	[Export]
	public float BulgeRadius = 8.0f;
	[Export]
	public float Strength = 1.0f;

	public override Image Convert(Image image)
	{
		Vector2I size = image.GetSize();
		int width = size.X;
		int height = size.Y;

		bool[,] opaque = new bool[width, height];
		for (int x = 0; x < width; ++x)
			for (int y = 0; y < height; ++y)
				opaque[x, y] = image.GetPixel(x, y).A >= AlphaCutoff;

		float radius = Mathf.Max(BulgeRadius, 0.001f);
		float[,] distance = ComputeDistanceToBackground(opaque, width, height);

		// Treat each sprite as a slice of a sphere: flat at the silhouette edge,
		// curving up to full height once far enough from the edge, then flat on top.
		float[,] heightField = new float[width, height];
		for (int x = 0; x < width; ++x)
		{
			for (int y = 0; y < height; ++y)
			{
				float d = Mathf.Min(distance[x, y], radius);
				heightField[x, y] = Mathf.Sqrt(Mathf.Max(0f, radius * radius - (radius - d) * (radius - d)));
			}
		}

		Image output = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
		for (int x = 0; x < width; ++x)
		{
			for (int y = 0; y < height; ++y)
			{
				if (!opaque[x, y])
				{
					output.SetPixel(x, y, new Color(0.5f, 0.5f, 1f, 0f));
					continue;
				}

				float hL = SampleHeight(heightField, opaque, x - 1, y, width, height);
				float hR = SampleHeight(heightField, opaque, x + 1, y, width, height);
				float hD = SampleHeight(heightField, opaque, x, y - 1, width, height);
				float hU = SampleHeight(heightField, opaque, x, y + 1, width, height);

				float dx = (hR - hL) * 0.5f * Strength;
				float dy = (hU - hD) * 0.5f * Strength;

				Vector3 normal = new Vector3(-dx, -dy, 1f).Normalized();
				Color normalColor = new Color(
					normal.X * 0.5f + 0.5f,
					normal.Y * 0.5f + 0.5f,
					normal.Z * 0.5f + 0.5f,
					1f
				);
				output.SetPixel(x, y, normalColor);
			}
		}

		return output;
	}

	private static float SampleHeight(float[,] heightField, bool[,] opaque, int x, int y, int width, int height)
	{
		// Outside the sprite (or the canvas) counts as height 0, matching the
		// silhouette edge so the normal tilts outward at the boundary.
		if (x < 0 || y < 0 || x >= width || y >= height || !opaque[x, y])
			return 0f;
		return heightField[x, y];
	}

	// Two-pass chamfer distance transform (1 / sqrt(2) weights): for every opaque
	// pixel, the approximate Euclidean distance to the nearest transparent pixel
	// (or the canvas edge).
	private static float[,] ComputeDistanceToBackground(bool[,] opaque, int width, int height)
	{
		const float Infinity = 1e20f;
		const float Diagonal = 1.41421356f;
		float[,] dist = new float[width, height];

		for (int x = 0; x < width; ++x)
			for (int y = 0; y < height; ++y)
				dist[x, y] = opaque[x, y] ? Infinity : 0f;

		for (int y = 0; y < height; ++y)
		{
			for (int x = 0; x < width; ++x)
			{
				float best = dist[x, y];
				best = Mathf.Min(best, Sample(dist, x - 1, y, width, height, 1f));
				best = Mathf.Min(best, Sample(dist, x, y - 1, width, height, 1f));
				best = Mathf.Min(best, Sample(dist, x - 1, y - 1, width, height, Diagonal));
				best = Mathf.Min(best, Sample(dist, x + 1, y - 1, width, height, Diagonal));
				dist[x, y] = best;
			}
		}

		for (int y = height - 1; y >= 0; --y)
		{
			for (int x = width - 1; x >= 0; --x)
			{
				float best = dist[x, y];
				best = Mathf.Min(best, Sample(dist, x + 1, y, width, height, 1f));
				best = Mathf.Min(best, Sample(dist, x, y + 1, width, height, 1f));
				best = Mathf.Min(best, Sample(dist, x + 1, y + 1, width, height, Diagonal));
				best = Mathf.Min(best, Sample(dist, x - 1, y + 1, width, height, Diagonal));
				dist[x, y] = best;
			}
		}

		return dist;
	}

	private static float Sample(float[,] dist, int x, int y, int width, int height, float weight)
	{
		// Off-canvas is treated as background too, so sprites touching the edge
		// still taper off instead of bulging forever.
		if (x < 0 || y < 0 || x >= width || y >= height)
			return weight;
		return dist[x, y] + weight;
	}
}
