using Godot;
using System;

namespace PuppetBones;

[Tool]
[GlobalClass]
public partial class Spring: ImpulseBone
{
	[Export]
	public Vector2 SpringDirection = new(0, 20);

    protected override Vector2 CalculatePosition => GlobalPosition - Position;

    public override void ApplyImpulseModification(float distance)
	{
		Position = SpringDirection*distance;
	}
}
