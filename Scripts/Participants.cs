using System.Collections.Generic;
using Godot;

// Stable per-match ids for the ships a match scores. A Node3D reference cannot cross the
// network and does not survive a respawn, so anything naming a participant — kill
// attribution now, the damage and match managers later — uses the id instead.
// Ids are assigned locally; a networked match will take them from the server.
public static class Participants
{
	public const int None = 0;

	private static readonly Dictionary<Node3D, int> _ids = new();
	private static readonly Dictionary<int, string> _names = new();
	private static int _nextId = 1;

	// Call before MatchStats.Reset() on scene change; both are scoped to one match.
	public static void Reset()
	{
		_ids.Clear();
		_names.Clear();
		_nextId = 1;
	}

	// Puts a ship on the roster. Safe to call twice; returns the existing id.
	// Pass an explicit id for a networked match, where the server decides who is who;
	// omit it offline and the local counter assigns one.
	public static int Register(Node3D ship, int id = None)
	{
		if (ship == null) return None;
		if (_ids.TryGetValue(ship, out int existing)) return existing;

		if (id == None) id = _nextId++;
		_ids[ship] = id;
		// Captured now so the name outlives the node: a freed ship must still be nameable
		// on the scoreboard.
		_names[id] = NameFor(ship);
		return id;
	}

	// None for anything off the roster: asteroids, level hazards, a ship that never
	// registered.
	public static int IdOf(Node3D ship)
	{
		if (ship == null) return None;
		return _ids.TryGetValue(ship, out int id) ? id : None;
	}

	public static string NameOf(int id) => _names.TryGetValue(id, out string name) ? name : "?";

	// The ship carrying this id on this machine, or null once it has been freed.
	public static Node3D NodeOf(int id)
	{
		foreach (KeyValuePair<Node3D, int> pair in _ids)
			if (pair.Value == id && GodotObject.IsInstanceValid(pair.Key))
				return pair.Key;
		return null;
	}

	private static string NameFor(Node3D ship)
	{
		if (ship is Fighter fighter && !string.IsNullOrEmpty(fighter.DisplayName))
			return fighter.DisplayName;
		return ship.Name.ToString();
	}
}
