using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PuppetBones;

[Tool]
[GlobalClass]
[Icon("res://addons/ImpishPuppets/PuppetBones/PuppetBone.png")]
public partial class IKParent : Node2D
{

#if TOOLS
	[Export]
	public bool Editing
	{
		get => _Editing;
		set
		{
			if(_Editing && !value)
			{
				BuildBoneList();
			}
			if(!_Editing && value)
			{
				if(BoneChildren != null)
				{
					foreach(var bone in BoneChildren)
						bone.Transform = bone.InitialTransform;
				}
			}
			_Editing = value;
		}
	}
	private bool _Editing = true;
#else
	private bool _Editing = false;
#endif

	[Export]
	public Node2D Target;
	[Export]
	public float RootSlack = 0f;
	[Export]
	public bool Rooted = true;

	public List<IKBone> BoneChildren = null;
	private Vector2 PreviousPosition;

	private void BuildBoneList()
	{
		BoneChildren = [];
		foreach(var child in GetChildren())
		{
			if(child is IKBone bone)
				BoneChildren.Add(bone);
		}
		BoneChildren.Sort((b1,b2)=>b1.Order.CompareTo(b2.Order));
		for(int i = 0; i < BoneChildren.Count; ++i)
		{
			if(i == BoneChildren.Count - 1)
				BoneChildren[i].SetNext(null);
			else
				BoneChildren[i].SetNext(BoneChildren[i+1]);
		}
	}

    public override void _Ready()
	{
		ProcessPriority = 100;
		BuildBoneList();
	}

    public override void _Process(double delta)
	{
		if(BoneChildren == null)
			BuildBoneList();
		
		if(_Editing)
		{
			foreach(var bone in BoneChildren)
				bone.SetInitialTransform();
			return;
		}
		
		if(Target == null)
			return;
		if(BoneChildren.Count == 0)
			return;
		
		Vector2 localTarget = ToLocal(Target.GlobalPosition);
		for(int i = 0; i < 2; ++i)
		{
			Vector2 end = BoneChildren[^1].Position;
			if((end-localTarget).LengthSquared() <= Mathf.Epsilon)
				break;
			ApplyIK(BoneChildren, localTarget);
			if(Rooted)
			{
				Vector2 rootPos = BoneChildren[0].Position.LimitLength(RootSlack);
				BoneChildren[0].Position = rootPos;
			}
			LimitAngle(BoneChildren);
			if((end-BoneChildren[^1].Position).LengthSquared() <= Mathf.Epsilon)
				break;
		}
	}

	private static void ApplyIK(List<IKBone> bones, Vector2 targetPos)
	{
		Vector2 currentTarget = targetPos;
		foreach(var bone in bones.Reverse<IKBone>())
		{
			Vector2 diff = currentTarget - bone.Position;
			bone.Rotation = diff.Angle();
			currentTarget -= diff.Normalized()*bone.Length;
			bone.Position = currentTarget;
		}
		if(bones.Count > 1)
			bones[^1].Rotation = bones[^2].Rotation;
	}

	private static bool LimitAngle(List<IKBone> bones)
	{
		bool angleLimited = false;
		for(int i = 1; i < bones.Count; ++i)
		{
			IKBone currentBone = bones[i];
			IKBone previousBone = bones[i-1];
			if(currentBone.RotationLimit != 0f)
			{
				float limitSign = Mathf.Sign(currentBone.RotationLimit);
				float bendAngle = Mathf.Wrap(currentBone.Rotation - previousBone.Rotation + currentBone.RotationLimit, -Mathf.Pi, Mathf.Pi);
				float bendSign = Mathf.Sign(bendAngle);
				if(bendSign == limitSign)
				{
					angleLimited = true;
					previousBone.Rotation += bendAngle;
				}
			}
			currentBone.Position = previousBone.Position + previousBone.Length*Vector2.FromAngle(previousBone.Rotation);
		}
		return angleLimited;
	}

#if TOOLS
    public override void _Notification(int what)
	{
		if(what == NotificationChildOrderChanged)
			BuildBoneList();
	}
#endif
}