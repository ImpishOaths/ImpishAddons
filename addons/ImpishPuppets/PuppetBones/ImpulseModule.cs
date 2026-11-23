using Godot;
using System;

namespace PuppetBones;

[Tool]
[GlobalClass]
public partial class ImpulseModule: Resource
{
	private const float VEL_EPSILON = 0.01f;
	private const float DIST_EPSILON = 0.001f;

	private enum DistanceEdgeBehaviour
	{
		NONE,
		CLAMP,
		STOP,
		BOUNCE
	}
	[Export]
	private Vector2 ImpulseDir = new(0, 1);
	[Export]
	private float ImpulseStrength = 20;
	[Export]
	private Vector2 MinMax = new(-1, 1);
	[Export]
	private DistanceEdgeBehaviour EdgeBehaviour = DistanceEdgeBehaviour.STOP;
	[Export]
	private float RestoreStrength = 100;
	[Export]
	private float Damping = 1f;

	public (float distance, float velocity) UpdateImpulse(float distance, float velocity, Vector2 offset, float delta)
	{
		float alignedImpulse = ImpulseDir.Dot(offset);
		float acceleration = Mathf.Sign(alignedImpulse) * Mathf.Sqrt(Mathf.Abs(alignedImpulse)) * ImpulseStrength;
		float restoreAcceleration = RestoreStrength * -distance;
		velocity += (restoreAcceleration + acceleration) * delta;
		velocity *= 1f/(1+delta*Damping);
		distance += velocity * delta;

		float checkDistance = Mathf.Clamp(distance, MinMax.X, MinMax.Y);
		if(checkDistance != distance)
		{
			if(EdgeBehaviour == DistanceEdgeBehaviour.STOP)
				velocity = 0;
			if(EdgeBehaviour == DistanceEdgeBehaviour.BOUNCE)
				velocity *= -1;
		}
		if(EdgeBehaviour != DistanceEdgeBehaviour.NONE)
			distance = checkDistance;
		
		if(Mathf.Abs(distance) < DIST_EPSILON)
			distance = 0;
		if(Mathf.Abs(velocity) < VEL_EPSILON)
			velocity = 0;

		return (distance, velocity);
	}
}