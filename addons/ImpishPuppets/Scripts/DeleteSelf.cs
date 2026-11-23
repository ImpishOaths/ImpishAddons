using Godot;
using System;

[Tool]
public partial class DeleteSelf : Node
{
    public override void _EnterTree()
	{
		#if TOOLS
			if(Owner != GetTree().EditedSceneRoot)
			{
				QueueFree();
			}
		#else
			QueueFree();
		#endif
	}
}
