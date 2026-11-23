using Godot;
using System;

[Tool]
[GlobalClass]
public partial class CharacterData : Resource
{
	[Export]
	public string Name;

	[Export]
	public float LowerHeight;
	[Export]
	public float LegHeight;
	[Export]
	public Vector2 LegWidths;
	[Export]
	public Vector2 FootScale = Vector2.One;
	[Export]
	public float UpperHeight;
	[Export]
	public float ArmsHeight;
	[Export]
	public Vector2 ArmsWidths;
	[Export]
	public Vector2 HandScale = Vector2.One;
	[Export]
	public float HeadHeight;
}
