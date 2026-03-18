using Godot;

public partial class SplashScreen : Control
{
	[Export] private float fadeInSeconds = 0.4f;
	[Export] private float holdSeconds = 1.0f;
	[Export] private float fadeOutSeconds = 0.4f;
	[Export(PropertyHint.File, "*.tscn")] private string nextScenePath = "res://Scenes/Menu.tscn";
	[Export] private NodePath[] fadeTargets =
	{
		new NodePath("CenterContainer/VBoxContainer/Logo"),
		new NodePath("CenterContainer/VBoxContainer/Title"),
		new NodePath("CenterContainer/VBoxContainer/Hint")
	};

	public override async void _Ready()
	{
		fadeInSeconds = Mathf.Max(0.0f, fadeInSeconds);
		holdSeconds = Mathf.Max(0.0f, holdSeconds);
		fadeOutSeconds = Mathf.Max(0.0f, fadeOutSeconds);

		SetTargetsAlpha(0.0f);
		await FadeTo(1.0f, fadeInSeconds);
		if (holdSeconds > 0.0f)
			await ToSignal(GetTree().CreateTimer(holdSeconds), SceneTreeTimer.SignalName.Timeout);
		await FadeTo(0.0f, fadeOutSeconds);

		if (string.IsNullOrWhiteSpace(nextScenePath))
		{
			GD.PushError("SplashScreen: nextScenePath is empty.");
			return;
		}

		var error = GetTree().ChangeSceneToFile(nextScenePath);
		if (error != Error.Ok)
			GD.PushError($"SplashScreen: failed to change scene to '{nextScenePath}' with error {error}.");
	}

	private async System.Threading.Tasks.Task FadeTo(float alpha, float duration)
	{
		if (duration <= 0.0f)
		{
			SetTargetsAlpha(alpha);
			return;
		}

		var tween = CreateTween();
		foreach (var target in fadeTargets)
		{
			var canvasItem = GetNodeOrNull<CanvasItem>(target);
			if (canvasItem == null)
				continue;

			var targetColor = canvasItem.Modulate;
			targetColor.A = alpha;
			tween.Parallel().TweenProperty(canvasItem, "modulate", targetColor, duration);
		}

		await ToSignal(tween, Tween.SignalName.Finished);
	}

	private void SetTargetsAlpha(float alpha)
	{
		foreach (var target in fadeTargets)
		{
			var canvasItem = GetNodeOrNull<CanvasItem>(target);
			if (canvasItem == null)
				continue;

			var color = canvasItem.Modulate;
			color.A = alpha;
			canvasItem.Modulate = color;
		}
	}
}
