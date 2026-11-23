using Godot;
using Godot.Collections;
using System;

[Tool]
[GlobalClass]
[Icon("AnimationPlayer")]
public partial class RemoteAnimator: Node
{
	[Export]
	public Dictionary<StringName,AnimationPlayer> Animators = null;

    public override Variant _Get(StringName property)
	{
		if(Animators != null && Animators.TryGetValue(property, out AnimationPlayer animator))
		{
			return animator.AssignedAnimation;
		}
		return default;
	}

    public override bool _Set(StringName property, Variant value)
	{
		if(Animators != null && Animators.TryGetValue(property, out AnimationPlayer animator))
		{
			animator.Play(value.AsString());
			return true;
		}
		return false;
	}

	public override Array<Dictionary> _GetPropertyList()
	{
        Array<Dictionary> properties = [];

		if(Animators == null)
			return properties;

		foreach(var anim in Animators)
		{
			string animations = "";
			animations +=  string.Join(",",anim.Value.GetAnimationList());
			
			properties.Add(new(){
				{ "name", anim.Key },
				{ "type", (int)Variant.Type.StringName },
				{ "hint", (int)PropertyHint.Enum },
				{ "hint_string", animations }
			});
		}

		return properties;
	}


}
