using Godot;

// Every hit in the game passes through here. Weapons say who hit what; this decides
// whether that is local business or the server's, and applies it. Gameplay scripts keep
// their plain TakeDamage and never learn there is a network.
public partial class DamageManager : Node
{
	public static DamageManager Instance { get; private set; }

	public override void _Ready() => Instance = this;

	public void Report(Node3D target, float amount, CollisionShape3D hitShape, Node3D source)
	{
		int targetId = Participants.IdOf(target);

		// Offline, or a target that is not a replicated ship — asteroids and hazards are
		// simulated per machine anyway, so routing their damage would buy nothing.
		if (targetId == Participants.None || !NetworkManager.Instance.IsActive)
		{
			ApplyLocal(target, amount, hitShape, source);
			return;
		}

		if (NetworkManager.Instance.IsServer)
			Rpc(MethodName.ApplyDamage, targetId, amount, Participants.IdOf(source));
		else
			RpcId(1, MethodName.RequestDamage, targetId, amount);
	}

	// Client -> server. The server stamps the attacker from the sender id, so a client can
	// only ever claim its own hits, never frame somebody else.
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RequestDamage(int targetId, float amount)
	{
		Rpc(MethodName.ApplyDamage, targetId, amount, Multiplayer.GetRemoteSenderId());
	}

	// Server -> everyone, so health and death agree on every machine.
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void ApplyDamage(int targetId, float amount, int sourceId)
	{
		ApplyLocal(Participants.NodeOf(targetId), amount, null, Participants.NodeOf(sourceId));
	}

	private static void ApplyLocal(Node3D target, float amount, CollisionShape3D hitShape, Node3D source)
	{
		if (target is IDamageable damageable)
			damageable.TakeDamage(amount, hitShape, source);
	}
}
