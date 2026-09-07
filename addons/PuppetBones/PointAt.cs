using Godot;

namespace PuppetBones;

[Tool]
[GlobalClass]
[Icon("res://addons/PuppetBones/PuppetBone.png")]
public partial class PointAt: Node2D
{
	[Export]
	private float RotateOffset;
    [Export]
    private Node2D Target;
	
    public override void _Process(double delta)
	{
		if(Target == null)
			return;
		
		GlobalRotation = (Target.GlobalPosition - GlobalPosition).Angle() + RotateOffset;
	}
}