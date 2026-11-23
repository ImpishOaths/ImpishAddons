using Godot;
using System;
using BetterRemotes;

[Tool]
[GlobalClass]
public partial class PuppetSkeleton : Node, ISerializationListener
{
	[Export]
	public CharacterData Character;
	
	[Export]
	public Node2D TargetPuppet;

	[Export]
	public bool Editing;

	private bool IsReady = false;

	[OnReady("Root/EyeTarget")]
	private Remote2D EyeTarget;

	[OnReady("Root/LowerBase")]
	private Node2D LowerBase;

	[OnReady("Root/LowerBase/LowerBody")]
	private Remote2D LowerBody;

	[OnReady("Root/LowerBase/LowerBody/LegR")]
	private Remote2D LegR;

	[OnReady("Root/LowerBase/LowerBody/LegL")]
	private Remote2D LegL;

	[OnReady("Root/LowerBase/LowerBody/UpperBody")]
	private Remote2D UpperBody;

	[OnReady("Root/LowerBase/LowerBody/UpperBody/ArmR")]
	private Remote2D ArmR;

	[OnReady("Root/LowerBase/LowerBody/UpperBody/ArmL")]
	private Remote2D ArmL;

	[OnReady("Root/LowerBase/LowerBody/UpperBody/ArmR/HandR")]
	private Remote2D HandR;

	[OnReady("Root/LowerBase/LowerBody/UpperBody/ArmL/HandL")]
	private Remote2D HandL;

	[OnReady("Root/LowerBase/LowerBody/UpperBody/Head")]
	private Remote2D Head;

	[OnReady("Root/FloorR")]
	private Node2D FloorR;

	[OnReady("Root/FloorR/FootR")]
	private Remote2D FootR;

	[OnReady("Root/FloorL")]
	private Node2D FloorL;

	[OnReady("Root/FloorL/FootL")]
	private Remote2D FootL;

	[OnReady("ClothingAnimators")]
	private RemoteAnimator ClothingAnimators;

	[OnReady("ExtraAnimators")]
	private RemoteAnimator ExtraAnimators;

	[OnReady("LegAnimators")]
	private RemoteAnimator LegAnimators;

	[OnReady("ArmRAnimators")]
	private RemoteAnimator ArmRAnimators;

	[OnReady("ArmLAnimators")]
	private RemoteAnimator ArmLAnimators;

	[OnReady("HeadAnimators")]
	private RemoteAnimator HeadAnimators;

	[Export(PropertyHint.Range, "0,1")]
	private float LowerBodyHeightScale
	{
		get => _LowerBodyHeightScale;
		set
		{
			_LowerBodyHeightScale = value;
			if(! IsReady) return;
			LowerBase.Position = new Vector2(0, -Character.LowerHeight * _LowerBodyHeightScale);
		}
	}
	private float _LowerBodyHeightScale = 1.0f;

	[Export(PropertyHint.Range, "-1,1")]
	private float LegsWidthScale
	{
		get => _LegsWidthScale;
		set
		{
			_LegsWidthScale = value;
			if(! IsReady) return;
			LegR.Position = new Vector2(Character.LegWidths.Y*_LegsWidthScale, -Character.LegHeight);
			LegL.Position = new Vector2(-Character.LegWidths.X*_LegsWidthScale, -Character.LegHeight);
			FloorR.Position = new Vector2(Character.LegWidths.Y*_LegsWidthScale, 0);
			FloorL.Position = new Vector2(-Character.LegWidths.X*_LegsWidthScale, 0);

		}
	}
	private float _LegsWidthScale = 1.0f;

	[Export(PropertyHint.Range, "-1,1")]
	private float ArmsWidthScale
	{
		get => _ArmsWidthScale;
		set
		{
			_ArmsWidthScale = value;
			if(! IsReady) return;
			ArmR.Position = new Vector2(Character.ArmsWidths.Y*_ArmsWidthScale, -Character.ArmsHeight);
			ArmL.Position = new Vector2(-Character.ArmsWidths.X*_ArmsWidthScale, -Character.ArmsHeight);
		}
	}
	private float _ArmsWidthScale = 1.0f;

	private void AssignNodes()
	{
		if(TargetPuppet == null)
			return;
		
		EyeTarget.Target = TargetPuppet.GetNode<Node2D>("Sprites/Head/EyeTargetR");
		LowerBody.Target = TargetPuppet.GetNode<Node2D>("Sprites/LowerBody");
		LegR.Target = TargetPuppet.GetNode<Node2D>("Sprites/LayerLegR/Leg");
		LegL.Target = TargetPuppet.GetNode<Node2D>("Sprites/LayerLegL/Leg");
		UpperBody.Target = TargetPuppet.GetNode<Node2D>("Sprites/UpperBody");
		Head.Target = TargetPuppet.GetNode<Node2D>("Sprites/Head");
		ArmR.Target = TargetPuppet.GetNode<Node2D>("Sprites/LayerArmR/Arm");
		ArmL.Target = TargetPuppet.GetNode<Node2D>("Sprites/LayerArmL/Arm");
		FootR.Target = TargetPuppet.GetNode<Node2D>("Sprites/LayerLegR/Foot");
		FootL.Target = TargetPuppet.GetNode<Node2D>("Sprites/LayerLegL/Foot");
		HandR.Target = TargetPuppet.GetNode<Node2D>("Sprites/LayerArmR/Hand");
		HandL.Target = TargetPuppet.GetNode<Node2D>("Sprites/LayerArmL/Hand");

		ClothingAnimators.Animators = new()
		{
			{"Head", TargetPuppet.GetNode<AnimationPlayer>("HeadClothes")},
			{"Upper", TargetPuppet.GetNode<AnimationPlayer>("UpperClothes")},
			{"Arm", TargetPuppet.GetNode<AnimationPlayer>("ArmClothes")},
			{"Lower", TargetPuppet.GetNode<AnimationPlayer>("LowerClothes")},
			{"Leg", TargetPuppet.GetNode<AnimationPlayer>("LegClothes")}
		};

		ExtraAnimators.Animators = new()
		{
			{"OrderFlip", TargetPuppet.GetNode<AnimationPlayer>("OrderFlipAnimator")},
			{"Expression", TargetPuppet.GetNode<AnimationPlayer>("ExpressionAnimator")}
		};

		LegAnimators.Animators = new()
		{
			{"LegL",TargetPuppet.GetNode<AnimationPlayer>("LegLAnimator")},
			{"LegR",TargetPuppet.GetNode<AnimationPlayer>("LegRAnimator")},
		};

		ArmRAnimators.Animators = new()
		{
			{"Arm",TargetPuppet.GetNode<AnimationPlayer>("ArmRAnimator")},
			{"Palm",TargetPuppet.GetNode<AnimationPlayer>("PalmRAnimator")},
			{"Hand",TargetPuppet.GetNode<AnimationPlayer>("HandRAnimator")},
		};

		ArmLAnimators.Animators = new()
		{
			{"Arm",TargetPuppet.GetNode<AnimationPlayer>("ArmLAnimator")},
			{"Palm",TargetPuppet.GetNode<AnimationPlayer>("PalmLAnimator")},
			{"Hand",TargetPuppet.GetNode<AnimationPlayer>("HandLAnimator")},
		};

		HeadAnimators.Animators = new()
		{
			{"EyebrowR",TargetPuppet.GetNode<AnimationPlayer>("EyebrowRAnimator")},
			{"EyebrowL",TargetPuppet.GetNode<AnimationPlayer>("EyebrowLAnimator")},
			{"EyeR",TargetPuppet.GetNode<AnimationPlayer>("EyeRAnimator")},
			{"EyeL",TargetPuppet.GetNode<AnimationPlayer>("EyeLAnimator")},
			{"Mouth",TargetPuppet.GetNode<AnimationPlayer>("MouthAnimator")},
		};
		
		IsReady = true;
	}

	private void AssignMeasurements()
	{
		if(Character == null || !IsReady)
			return;
		LowerBase.Position = new Vector2(0, -Character.LowerHeight * _LowerBodyHeightScale);
		LegR.Position = new Vector2(Character.LegWidths.Y*_LegsWidthScale, -Character.LegHeight);
		LegL.Position = new Vector2(-Character.LegWidths.X*_LegsWidthScale, -Character.LegHeight);
		UpperBody.Position = new Vector2(0, -Character.UpperHeight);
		ArmR.Position = new Vector2(Character.ArmsWidths.Y*_ArmsWidthScale, -Character.ArmsHeight);
		ArmR.Scale = Character.HandScale;
		ArmL.Position = new Vector2(-Character.ArmsWidths.X*_ArmsWidthScale, -Character.ArmsHeight);
		ArmL.Scale = Character.HandScale;
		Head.Position = new Vector2(0, -Character.HeadHeight);
		FloorR.Position = new Vector2(Character.LegWidths.Y*_LegsWidthScale, 0);
		FloorR.Scale = Character.FootScale;
		FloorL.Position = new Vector2(-Character.LegWidths.X*_LegsWidthScale, 0);
		FloorL.Scale = Character.FootScale;
	}

	public override void _Ready()
	{
		this.GetOnReadyNodes();
		AssignNodes();
		AssignMeasurements();
	}

	public override void _Process(double delta)
	{
		if(Editing)
			AssignMeasurements();
	}

    public void OnBeforeSerialize()
	{
	}

    public void OnAfterDeserialize()
    {
		this.GetOnReadyNodes();
		AssignNodes();
    }
}
