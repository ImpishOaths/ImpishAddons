using Godot;
using System;

namespace BetterRemotes;

[Tool]
[GlobalClass]
public partial class Remote3DTo2D : Node3D
{
	[Export]
	public Node2D Target;
	[Export]
	public Node3D PlaneNode;
	[Export]
	public Vector2 RemoteScale = Vector2.One;
	[Export]
	public Vector2 RemoteOffset = Vector2.Zero;

	public override void _Process(double delta)
	{
		if(Target == null)
			return;
		if(PlaneNode == null)
			return;
		Vector3 local = PlaneNode.GlobalTransform.AffineInverse() * GlobalPosition;
		Vector2 projection2D = new Vector2(local.X, -local.Y) * RemoteScale + RemoteOffset;
		Target.GlobalPosition = projection2D;
		Target.GlobalRotation = -GlobalRotation.Z;
	}
}
