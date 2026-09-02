using Godot;

// The only seam that knows which transport carries the game. ENet in the open source
// build; the Steam build supplies its own implementation and nothing else changes.
// Returns null when the peer cannot be created, so callers report rather than crash.
public interface IPeerFactory
{
	MultiplayerPeer CreateHost(int port, int maxPlayers);
	MultiplayerPeer CreateClient(string address, int port);
}
