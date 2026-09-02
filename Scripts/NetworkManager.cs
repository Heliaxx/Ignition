using System.Collections.Generic;
using Godot;

public partial class NetworkManager : Node
{
	private const string ProtocolVersion = "1";

	private const double AuthTimeoutSeconds = 5.0;

	public const int DefaultPort = 30500;
	public const int MaxPlayers = 12;

	public static NetworkManager Instance { get; private set; }

	public IPeerFactory PeerFactory { get; set; } = new EnetPeerFactory();

	[Signal] public delegate void PeerJoinedEventHandler(int peerId);
	[Signal] public delegate void PeerLeftEventHandler(int peerId);
	[Signal] public delegate void JoinedServerEventHandler();
	[Signal] public delegate void LeftServerEventHandler(string reason);

	private readonly HashSet<int> _peers = new();

	private SceneMultiplayer _scene;

	private bool _sessionOpen;

	public bool IsActive => _sessionOpen;

	public bool IsServer => !IsActive || Multiplayer.IsServer();

	public int LocalPeerId => IsActive ? Multiplayer.GetUniqueId() : 1;

	public IReadOnlyCollection<int> Peers => _peers;

	public override void _Ready()
	{
		Instance = this;
		_scene = (SceneMultiplayer)Multiplayer;

		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
		Multiplayer.ConnectedToServer += OnConnectedToServer;
		Multiplayer.ConnectionFailed += OnConnectionFailed;
		Multiplayer.ServerDisconnected += OnServerDisconnected;

		// Runs before a peer counts as connected, so a version mismatch never reaches a
		// match. A check after connecting would leave an incompatible peer briefly live.
		_scene.PeerAuthenticating += OnPeerAuthenticating;
		_scene.PeerAuthenticationFailed += OnPeerAuthenticationFailed;
		_scene.AuthCallback = Callable.From<int, byte[]>(OnAuthReceived);
		_scene.AuthTimeout = AuthTimeoutSeconds;
	}

	public bool Host(int port = DefaultPort)
	{
		if (IsActive)
		{
			GD.PrintErr("NetworkManager: already connected; call Leave() first");
			return false;
		}

		MultiplayerPeer peer = PeerFactory.CreateHost(port, MaxPlayers);
		if (peer == null)
		{
			GD.PrintErr($"NetworkManager: could not host on port {port}");
			return false;
		}

		Multiplayer.MultiplayerPeer = peer;
		_sessionOpen = true;
		_peers.Clear();
		_peers.Add(LocalPeerId);
		return true;
	}

	public bool Join(string address, int port = DefaultPort)
	{
		if (IsActive)
		{
			GD.PrintErr("NetworkManager: already connected; call Leave() first");
			return false;
		}

		MultiplayerPeer peer = PeerFactory.CreateClient(address, port);
		if (peer == null)
		{
			GD.PrintErr($"NetworkManager: could not reach {address}:{port}");
			return false;
		}

		Multiplayer.MultiplayerPeer = peer;
		_sessionOpen = true;
		_peers.Clear();
		return true;
	}

	public void Leave()
	{
		if (_sessionOpen)
			Multiplayer.MultiplayerPeer?.Close();

		Multiplayer.MultiplayerPeer = null;
		_sessionOpen = false;
		_peers.Clear();
	}

	private void OnPeerAuthenticating(long id)
	{
		_scene.SendAuth((int)id, ProtocolVersion.ToUtf8Buffer());
	}

	private void OnAuthReceived(int id, byte[] data)
	{
		string theirs = data.GetStringFromUtf8();
		if (theirs == ProtocolVersion)
		{
			_scene.CompleteAuth(id);
			return;
		}

		GD.PrintErr($"NetworkManager: peer {id} speaks protocol '{theirs}', we speak '{ProtocolVersion}'");
		Multiplayer.MultiplayerPeer.DisconnectPeer(id);
	}

	private void OnPeerAuthenticationFailed(long id)
	{
		GD.PrintErr($"NetworkManager: peer {id} failed authentication");
	}

	private void OnPeerConnected(long id)
	{
		_peers.Add((int)id);
		EmitSignal(SignalName.PeerJoined, (int)id);
	}

	private void OnPeerDisconnected(long id)
	{
		_peers.Remove((int)id);
		EmitSignal(SignalName.PeerLeft, (int)id);
	}

	private void OnConnectedToServer()
	{
		_peers.Add(LocalPeerId);
		EmitSignal(SignalName.JoinedServer);
	}

	private void OnConnectionFailed()
	{
		Leave();
		EmitSignal(SignalName.LeftServer, "connection failed");
	}

	private void OnServerDisconnected()
	{
		Leave();
		EmitSignal(SignalName.LeftServer, "host closed the session");
	}
}
