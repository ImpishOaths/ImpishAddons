using Godot;
using System;

namespace BetterRemotes;

[Tool]
[GlobalClass]
public partial class Remote2D : Node2D
{
	[Export]
	public Node2D Other;

    public override void _Process(double delta)
	{
		if(Other == null)
			return;
		Other.GlobalPosition = GlobalPosition;
		Other.GlobalRotation = GlobalRotation;
	}
}
