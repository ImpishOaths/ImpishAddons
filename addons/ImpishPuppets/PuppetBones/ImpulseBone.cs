using Godot;
using System;

namespace PuppetBones;

[Tool]
[GlobalClass]
[Icon("res://addons/ImpishPuppets/PuppetBones/PuppetBone.png")]
public abstract partial class ImpulseBone: Node2D
{
	[Export]
    private ImpulseModule Impulse = new();
    [Export]
    private GlobalImpulse GlobalImpulse = null;
    
    public float Distance = 0;
    public float Velocity = 0;

    private Vector2? PreviousPostion = null;
    private const float DeltaMax = 1/120f;

    protected virtual Vector2 CalculatePosition => GlobalPosition;

    public override void _Process(double delta)
    {
        if(Impulse == null)
            return;
        if(GlobalImpulse != null && !GlobalImpulse.Active)
            return;

        var currentPosition = CalculatePosition;
        PreviousPostion ??= currentPosition;
        var offset = currentPosition - PreviousPostion.Value;
        if(GlobalImpulse != null)
            offset += GlobalImpulse.Impulse;

        if(Distance == 0 && Velocity == 0 && offset.Length() <= Mathf.Epsilon)
        {
            ApplyImpulseModification(Distance);
            return;
        }

        float totalDelta = (float)delta;
        float subDelta = totalDelta;
        while(subDelta > DeltaMax)
        {
            Vector2 stepOffset = offset * (DeltaMax / totalDelta);
            (Distance, Velocity) = Impulse.UpdateImpulse(Distance, Velocity, stepOffset, DeltaMax);
            subDelta -= DeltaMax;
        }
        Vector2 lastOffset = offset * (subDelta / totalDelta);
        (Distance, Velocity) = Impulse.UpdateImpulse(Distance, Velocity, lastOffset, subDelta);


        ApplyImpulseModification(Distance);

        PreviousPostion = currentPosition;
    }

    public abstract void ApplyImpulseModification(float distance);
}
