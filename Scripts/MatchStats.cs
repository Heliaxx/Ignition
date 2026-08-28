using System.Collections.Generic;
using Godot;

// Per-match kill/death tally, fed by EventBus.Killed. Static to match EventBus: a match
// is global to the running scene, and BaseLevel._Ready resets it after EventBus.ClearAll().
public static class MatchStats
{
	public class Entry
	{
		public string Name;
		public int Kills;
		public int Deaths;
		// Deaths nobody gets credit for: rammed an asteroid, own missile, level hazard.
		public int Suicides;
	}

	private static readonly Dictionary<Node3D, Entry> _entries = new();

	public static IReadOnlyDictionary<Node3D, Entry> Entries => _entries;

	public static void Reset()
	{
		_entries.Clear();

		// EventBus.ClearAll() dropped our handler, so resubscribe; the -= guards a double Reset().
		EventBus.Killed -= OnKilled;
		EventBus.Killed += OnKilled;
	}

	// Puts a participant on the board before it has scored. Safe to call twice.
	public static void Register(Node3D participant)
	{
		if (participant != null) Get(participant);
	}

	private static void OnKilled(Node3D victim, Node3D killer)
	{
		if (victim == null) return;

		Get(victim).Deaths++;

		// Attribution also carries non-ships — ramming an asteroid credits the asteroid —
		// so anything that is not a ship counts as a suicide.
		if (killer == null || killer == victim || !IsParticipant(killer))
		{
			Get(victim).Suicides++;
			return;
		}

		Get(killer).Kills++;
	}

	private static bool IsParticipant(Node3D node) => node is Kaito || node is Fighter;

	private static Entry Get(Node3D participant)
	{
		if (!_entries.TryGetValue(participant, out Entry entry))
		{
			entry = new Entry { Name = LabelFor(participant) };
			_entries[participant] = entry;
		}
		return entry;
	}

	private static string LabelFor(Node3D node)
	{
		if (node is Fighter fighter && !string.IsNullOrEmpty(fighter.DisplayName))
			return fighter.DisplayName;
		return node.Name.ToString();
	}
}
