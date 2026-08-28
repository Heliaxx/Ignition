public interface IDamageable
{
	// source: who caused the damage (the shooter, or the body rammed); null if nobody.
	// Last on purpose — CollisionShape3D is a Node3D, so an earlier source would let
	// old two-argument calls silently bind hitShape to it.
	void TakeDamage(float amount, Godot.CollisionShape3D hitShape = null, Godot.Node3D source = null);
}
