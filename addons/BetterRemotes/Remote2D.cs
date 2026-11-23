using Godot;
using System;

namespace BetterRemotes;

[Tool]
[GlobalClass]
public partial class Remote2D : Node2D
{
	[Export]
	public Node2D Target; 

    public override void _Process(double delta)
	{
		if(Target == null)
			return;
		Target.GlobalPosition = GlobalPosition;
		Target.GlobalRotation = GlobalRotation;
	}
}
