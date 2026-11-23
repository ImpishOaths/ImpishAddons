using Godot;

namespace PuppetBones;

[Tool]
[GlobalClass]
[Icon("res://addons/ImpishPuppets/PuppetBones/PuppetBone.png")]
public partial class PointAt: Node2D
{
    [Export]
    private Node2D Target;
	[Export]
	private float RotateOffset = 0f;
	[Export]
	private Vector2 PositionOffset = Vector2.Zero;

	[Export]
	private bool DoRotation = true;
	[Export]
	private bool DoExtension = false;
	
	[Export]
	private float OffsetDist = 0f;
	
    public override void _Process(double delta)
	{
		if(Target == null)
			return;

		Position = PositionOffset;
		Rotation = RotateOffset;

		Vector2 diff = Target.GlobalPosition - GlobalPosition;

		if(DoRotation)
			GlobalRotation += diff.Angle();

		if(DoExtension)
			GlobalPosition += diff.LimitLength(OffsetDist) * GlobalScale.Abs();
	}
}