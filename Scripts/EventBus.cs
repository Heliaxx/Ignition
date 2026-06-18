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

	public static void EmitAudioSettingsChanged() => AudioSettingsChanged?.Invoke();
	public static void EmitEnemyKilled(int count = 1) => EnemyKilled?.Invoke(count);
	public static void EmitPlayerDied() => PlayerDied?.Invoke();

	// Clears all subscriptions; call on scene change to drop stale handlers.
	public static void ClearAll()
	{
		AudioSettingsChanged = null;
		EnemyKilled = null;
		PlayerDied = null;
	}
}
