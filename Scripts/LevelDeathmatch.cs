using Godot;

// PVP arena. Keeps a fixed world origin — see BaseLevel.ShiftOrigin — so every machine
// agrees on coordinates.
public partial class LevelDeathmatch : BaseLevel
{
	private const string ShipScene = "res://Scenes/Kaito.tscn";

	protected override int LocalParticipantId => NetworkManager.Instance.LocalPeerId;

	public override void _Ready()
	{
		base._Ready();

		Node3D points = GetNodeOrNull<Node3D>("SpawnPoints");
		PlaceAtSpawn(Player, points, MatchManager.Instance.SpawnIndexOf(NetworkManager.Instance.LocalPeerId));
		SpawnRemoteShips(points);
	}

	private void SpawnRemoteShips(Node3D points)
	{
		ShipSync.Instance.Clear();
		ShipSync.Instance.SetLocalShip(PlayerKaito);
		MissileSync.Instance.Clear();

		var scene = GD.Load<PackedScene>(ShipScene);
		int localId = NetworkManager.Instance.LocalPeerId;
		System.Collections.Generic.IReadOnlyList<int> roster = MatchManager.Instance.Roster;

		for (int i = 0; i < roster.Count; i++)
		{
			int peerId = roster[i];
			if (peerId == localId) continue;

			var ship = scene.Instantiate<Kaito>();
			// Before AddChild: _Ready would otherwise hand this ship the viewport camera.
			ship.MakeRemote();
			AddChild(ship);

			PlaceAtSpawn(ship, points, i);
			Participants.Register(ship, peerId);
			MatchStats.Register(ship);
			ShipSync.Instance.AddRemoteShip(peerId, ship);
		}
	}

	private static void PlaceAtSpawn(Node3D ship, Node3D points, int index)
	{
		if (ship == null) return;
		ship.GlobalTransform = TransformAt(points, index) ?? ship.GlobalTransform;
	}

	// Where a participant belongs at match start and on every respawn.
	public Transform3D SpawnTransform(int index) =>
		TransformAt(GetNodeOrNull<Node3D>("SpawnPoints"), index) ?? GlobalTransform;

	private static Transform3D? TransformAt(Node3D points, int index)
	{
		if (points == null || points.GetChildCount() == 0) return null;
		if (index < 0) index = 0;

		return points.GetChild(index % points.GetChildCount()) is Node3D marker
			? marker.GlobalTransform
			: null;
	}
}
