using Godot;

public enum GimbalTargetType { Enemy, Ally, Powerup }

/// <summary>
/// Place this node anywhere in a scene to make that point targetable by the player's gimbal system.
/// Registers itself in the "gimbal_targets" group on _Ready.
/// Automatically links to its parent node as the owner — no Fighter reference needed.
/// If the parent has a "Died" signal, it is forwarded as GimbalTarget.Died.
/// </summary>
public partial class GimbalTarget : Node3D
{
	[Export] public GimbalTargetType TargetType = GimbalTargetType.Enemy;

	/// <summary>Overrides the HUD display name. Falls back to parent name, then node name.</summary>
	[Export] public string DisplayNameOverride = "";

	[Signal]
	public delegate void DiedEventHandler();

	private Node3D _owner;
	public Node3D TargetOwner => _owner;

	public override void _Ready()
	{
		AddToGroup("gimbal_targets");
		_owner = GetParentOrNull<Node3D>();

		// Forward the parent's Died signal as our own, if it has one
		if (_owner != null && _owner.HasSignal("Died"))
			_owner.Connect("Died", Callable.From(() => EmitSignal(SignalName.Died)));
	}

	/// <summary>Returns the velocity of this target point from the parent physics body, if any.</summary>
	public Vector3 GetVelocity()
	{
		if (_owner is CharacterBody3D cb && IsInstanceValid(cb)) return cb.Velocity;
		if (_owner is RigidBody3D rb    && IsInstanceValid(rb)) return rb.LinearVelocity;
		return Vector3.Zero;
	}

	/// <summary>Returns true if this target is still worth locking onto.</summary>
	public bool IsValid()
	{
		if (!IsInstanceValid(this) || !Visible)
			return false;

		if (_owner == null || !IsInstanceValid(_owner))
			return true; // standalone — valid while this node is visible

		if (!_owner.Visible || _owner.ProcessMode == ProcessModeEnum.Disabled)
			return false;

		// Health check — works with any node that exposes CurrentHealthValue
		if (_owner is Fighter fighter)
			return fighter.CurrentHealthValue > 0;

		return true;
	}

	public string GetDisplayName()
	{
		if (!string.IsNullOrEmpty(DisplayNameOverride)) return DisplayNameOverride;
		if (_owner is Fighter fighter) return fighter.DisplayName;
		return _owner?.Name ?? Name;
	}

	/// <summary>Returns true if health bar data is available.</summary>
	public bool HasHealthData() => _owner is Fighter && IsInstanceValid(_owner);

	public float GetCurrentHealth() => _owner is Fighter f ? f.CurrentHealthValue : 0;
	public float GetMaxHealth()     => _owner is Fighter f ? f.MaxHealthValue     : 1;
}
