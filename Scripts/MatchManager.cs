using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// Owns match lifecycle: which level everyone loads and who spawns where. Sits above
// NetworkManager, which knows only about connections and nothing about a match.
public partial class MatchManager : Node
{
	public const string DeathmatchLevel = "res://Scenes/LevelDeathmatch.tscn";
	public const string MenuScene = "res://Scenes/Menu.tscn";

	public static MatchManager Instance { get; private set; }

	// Peer order fixed by the server when the match starts; a peer's position in it is its
	// spawn point. Every peer gets the same array, so spawn assignment needs no further
	// agreement.
	private int[] _spawnOrder = Array.Empty<int>();

	public override void _Ready() => Instance = this;

	public void StartMatch(string levelPath = DeathmatchLevel)
	{
		if (!NetworkManager.Instance.IsServer) return;

		// Offline the same path runs without a peer to send to.
		if (!NetworkManager.Instance.IsActive)
		{
			BeginMatch(levelPath, new[] { NetworkManager.Instance.LocalPeerId });
			return;
		}

		int[] order = NetworkManager.Instance.Peers.OrderBy(id => id).ToArray();
		Rpc(MethodName.BeginMatch, levelPath, order);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void BeginMatch(string levelPath, int[] spawnOrder)
	{
		_spawnOrder = spawnOrder;
		GetTree().ChangeSceneToFile(levelPath);
	}

	// Long enough to see the explosion, short enough not to sit out the match.
	private const double RespawnDelay = 3.0;

	// A ship was destroyed. Offline that ends the run; a deathmatch puts the player back in,
	// and only the server decides when.
	public void OnShipDestroyed(Kaito ship)
	{
		if (!NetworkManager.Instance.IsActive)
		{
			ship.ShowDeathScreen();
			return;
		}

		if (!NetworkManager.Instance.IsServer) return;

		int participantId = Participants.IdOf(ship);
		GetTree().CreateTimer(RespawnDelay).Timeout += () =>
		{
			// The match can end, or everyone can leave, while the timer runs.
			if (NetworkManager.Instance.IsActive)
				Rpc(MethodName.RespawnShip, participantId);
		};
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RespawnShip(int participantId)
	{
		if (Participants.NodeOf(participantId) is not Kaito ship) return;
		if (GetTree().CurrentScene is not LevelDeathmatch level) return;

		ship.Respawn(level.SpawnTransform(SpawnIndexOf(participantId)));
	}

	// Ends the session and returns to the menu. Without this a player could walk out of a
	// match through the pause menu and leave the connection dangling behind them.
	public void LeaveMatch(string menuPath = MenuScene)
	{
		NetworkManager.Instance.Leave();
		ShipSync.Instance.Clear();
		MissileSync.Instance.Clear();
		GetTree().ChangeSceneToFile(menuPath);
	}

	// -1 when the peer is not in this match.
	public int SpawnIndexOf(int peerId) => Array.IndexOf(_spawnOrder, peerId);

	// Everyone in the match, in the order the server fixed at start.
	public IReadOnlyList<int> Roster => _spawnOrder;
}
