using System;

// Global event bus for publishers and subscribers without direct node references.
public static class EventBus
{
	// Audio settings changed (volume sliders, etc.).
	public static event Action AudioSettingsChanged;

	// An enemy was killed; the argument is the kill count delta.
	public static event Action<int> EnemyKilled;

	// The player died.
	public static event Action PlayerDied;

	// A ship was destroyed: the victim and whoever last damaged it (null if nobody).
	public static event Action<Godot.Node3D, Godot.Node3D> Killed;

	public static void EmitAudioSettingsChanged() => AudioSettingsChanged?.Invoke();
	public static void EmitEnemyKilled(int count = 1) => EnemyKilled?.Invoke(count);
	public static void EmitPlayerDied() => PlayerDied?.Invoke();
	public static void EmitKilled(Godot.Node3D victim, Godot.Node3D killer) => Killed?.Invoke(victim, killer);

	// Clears scene-scoped subscriptions; call on scene change to drop stale
	// handlers.
	public static void ClearAll()
	{
		EnemyKilled = null;
		PlayerDied = null;
		Killed = null;
	}
}
