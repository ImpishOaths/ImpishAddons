using Godot;
using System;

namespace PuppetBones;

[Tool]
[GlobalClass]
public partial class Pendulum: ImpulseBone
{
	[Export]
	public float RotationScale = 1;
    [Export]
    public float RotationOffset = 0;
	

    public override void ApplyImpulseModification(float distance)
	{
		Rotation = distance*RotationScale + RotationOffset;
	}
}
