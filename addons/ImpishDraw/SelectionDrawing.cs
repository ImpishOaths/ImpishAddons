#if TOOLS
using Godot;

[Tool]
public partial class SelectionDrawing : Node2D
{
	private Vector2[] Points = [];
	public void SetPoints(Vector2[] points)
	{
		Points = points;
		QueueRedraw();
	}
	
    public override void _Draw()
	{
		DrawPolyline(Points, Colors.White);
	}
}
#endif