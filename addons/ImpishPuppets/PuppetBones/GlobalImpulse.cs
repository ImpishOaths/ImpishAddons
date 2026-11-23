using Godot;
using System;

[Tool]
[GlobalClass]
public partial class GlobalImpulse : Resource
{
	[Export]
	public Vector2 Impulse;
	[Export]
	public bool Active = true;
}
