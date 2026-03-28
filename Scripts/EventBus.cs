using System;

/// <summary>
/// Static event bus for events where the publisher and subscriber don't have direct node references.
/// </summary>
public static class EventBus
{
	/// <summary>Fired when audio settings change (volume sliders, etc.).</summary>
	public static event Action AudioSettingsChanged;

	/// <summary>Fired when an enemy is killed. Subscribers receive the kill count delta.</summary>
	public static event Action<int> EnemyKilled;

	/// <summary>Fired when the player dies.</summary>
	public static event Action PlayerDied;

	public static void EmitAudioSettingsChanged() => AudioSettingsChanged?.Invoke();
	public static void EmitEnemyKilled(int count = 1) => EnemyKilled?.Invoke(count);
	public static void EmitPlayerDied() => PlayerDied?.Invoke();

	/// <summary>
	/// Fired when changing scenes to prevent stale subscriptions from freed nodes.
	/// </summary>
	public static void ClearAll()
	{
		AudioSettingsChanged = null;
		EnemyKilled = null;
		PlayerDied = null;
	}
}
