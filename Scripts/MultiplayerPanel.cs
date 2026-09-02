using Godot;

// Host/join bench for the ENet transport and the roster in NetworkManager. The real
// lobby grows out of this; right now it only proves a connection is established and
// torn down cleanly.
public partial class MultiplayerPanel : Control
{
	private LineEdit _address;
	private Label _status;

	public override void _Ready()
	{
		MenuUtils.AttachButtonSounds(this);

		_address = (LineEdit)FindChild("AddressEdit");
		_status = (Label)FindChild("StatusLabel");

		((Button)FindChild("HostButton")).Pressed += OnHost;
		((Button)FindChild("JoinButton")).Pressed += OnJoin;
		((Button)FindChild("BackButton")).Pressed += OnBack;

		NetworkManager net = NetworkManager.Instance;
		net.PeerJoined += OnPeerJoined;
		net.PeerLeft += OnPeerLeft;
		net.JoinedServer += OnJoinedServer;
		net.LeftServer += OnLeftServer;

		Refresh();
	}

	// NetworkManager outlives this panel, so its signals must not keep pointing here.
	public override void _ExitTree()
	{
		NetworkManager net = NetworkManager.Instance;
		if (net == null) return;

		net.PeerJoined -= OnPeerJoined;
		net.PeerLeft -= OnPeerLeft;
		net.JoinedServer -= OnJoinedServer;
		net.LeftServer -= OnLeftServer;
	}

	private void OnHost()
	{
		if (NetworkManager.Instance.Host())
			Refresh();
		else
			_status.Text = "could not host";
	}

	private void OnJoin()
	{
		string address = _address.Text.Length > 0 ? _address.Text : "127.0.0.1";
		if (NetworkManager.Instance.Join(address))
			_status.Text = $"connecting to {address}…";
		else
			_status.Text = $"could not reach {address}";
	}

	private void OnBack()
	{
		// Leaving the panel drops the session: a connection with no UI behind it is worse
		// than none while this is still a bench.
		NetworkManager.Instance.Leave();
		GetParent<MenuStack>().Pop();
	}

	private void OnPeerJoined(int peerId) => Refresh();
	private void OnPeerLeft(int peerId) => Refresh();
	private void OnJoinedServer() => Refresh();
	private void OnLeftServer(string reason) { Refresh(); _status.Text = reason; }

	private void Refresh()
	{
		NetworkManager net = NetworkManager.Instance;
		if (!net.IsActive)
		{
			_status.Text = "offline";
			return;
		}

		string role = net.IsServer ? "hosting" : "client";
		_status.Text = $"{role} · id {net.LocalPeerId} · {net.Peers.Count} peer(s)";
	}
}
