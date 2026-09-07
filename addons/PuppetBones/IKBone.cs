using Godot;
using System;

namespace PuppetBones;

[Tool]
[GlobalClass]
[Icon("res://addons/PuppetBones/PuppetBone.png")]
public partial class IKBone : Node2D
{
	[Export]
	public float RotationLimit = 0;
	[Export]
	public int Order;
	[Export]
	public Transform2D InitialTransform {get; private set;} = Transform2D.Identity;
	public void SetInitialTransform()
	{
		InitialTransform = Transform;
	}
	public float Length {get; private set;}
	public void SetNext(IKBone next)
	{
		Length = next == null ? 0 : (InitialTransform.Origin - next.InitialTransform.Origin).Length();
	}
}
