using Godot;
using System;

namespace PuppetBones;

[Tool]
[GlobalClass]
public partial class Squish: ImpulseBone
{
	[Export]
	public Vector2 SquishDirection = new(1, -1);

    public override void ApplyImpulseModification(float distance)
	{
		Vector2 scale = Vector2.One + SquishDirection*Distance;
		if(Mathf.Abs(scale.X) < Mathf.Epsilon)
			scale.X = Mathf.Epsilon;
		if(Mathf.Abs(scale.Y) < Mathf.Epsilon)
			scale.Y = Mathf.Epsilon;
		Scale = scale;
	}
}
