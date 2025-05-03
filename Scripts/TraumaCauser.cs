using Godot;
using System;

public partial class TraumaCauser : Area3D
{
	[Export]
	public float TraumaAmount = 0.1f;

	public void CauseTrauma()
	{
		var traumaAreas = GetOverlappingAreas();

		foreach (var areaObj in traumaAreas)
		{
			if (areaObj is Node area && area.HasMethod("add_trauma"))
			{
				area.Call("add_trauma", TraumaAmount);
			}
		}
	}
}
