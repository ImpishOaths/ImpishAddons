using Godot;
using System;

namespace BetterRemotes;

[Tool]
[GlobalClass]
public partial class Remote2DTo3D : Node3D
{
	[Export]
	public Node2D Target;
	[Export]
	public Node3D PlaneNode;
	[Export]
	public Vector2 RemoteScale = Vector2.One;
	[Export]
	public Vector2 RemoteOffset = Vector2.Zero;
	[Export]
	public float Offset = 0;

	public override void _Process(double delta)
	{
		if(Target == null)
			return;
		if(PlaneNode == null)
			return;
		Vector2 local2D = (Target.GlobalPosition - RemoteOffset) / RemoteScale;
		Vector3 local = new(local2D.X, -local2D.Y, Offset);
		GlobalPosition = PlaneNode.GlobalTransform * local;
		Vector3 rotation = GlobalRotation;
		rotation.Z = -Target.GlobalRotation;
		GlobalRotation = rotation;
	}
}
