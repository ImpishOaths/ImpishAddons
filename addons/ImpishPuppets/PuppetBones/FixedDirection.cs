using Godot;

namespace PuppetBones;

[Tool]
[GlobalClass]
[Icon("res://addons/ImpishPuppets/PuppetBones/PuppetBone.png")]
public partial class FixedDirection: Node2D
{
	[Export]
	private float Direction;
    [Export]
    private Node2D Relative;

    public override void _Process(double delta)
	{
		if(Relative == null)
			GlobalRotation = Direction;
		else
		{
			float RelativeFlipSign = Mathf.Sign(Relative.Scale.X * Relative.Scale.Y);
			float GlobalFlipSign = Mathf.Sign(GlobalScale.X * GlobalScale.Y) * RelativeFlipSign;
			GlobalRotation = Relative.GlobalRotation + (Direction * RelativeFlipSign);// + (GlobalFlipSign < 0 ? Mathf.Pi : 0);
		}
	}
}