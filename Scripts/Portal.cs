using Godot;
using System;

public partial class Portal : Node3D
{
	[Export] public float OpenDuration = 2f;
	[Export] public float BeforeSpawnDuration = 0.5f;
	[Export] public float AfterSpawnDuration = 1f;
	[Export] public float CloseDuration = 1f;

	public void OpenThenClose(Action onOpened)
	{
		Scale = Vector3.Zero;

		var tween = CreateTween();
		tween.TweenProperty(this, "scale", Vector3.One, OpenDuration)
			 .SetTrans(Tween.TransitionType.Expo)
			 .SetEase(Tween.EaseType.In);
		tween.TweenInterval(BeforeSpawnDuration);
		tween.TweenCallback(Callable.From(() => onOpened?.Invoke()));
		tween.TweenInterval(AfterSpawnDuration);
		tween.TweenProperty(this, "scale", Vector3.Zero, CloseDuration)
			 .SetTrans(Tween.TransitionType.Cubic)
			 .SetEase(Tween.EaseType.In);
		tween.TweenCallback(Callable.From(QueueFree));
	}
}
