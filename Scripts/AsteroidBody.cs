using Godot;

public partial class AsteroidBody : StaticBody3D, IDamageable
{
	public ChunkedAsteroidField Field { get; set; }

	public void TakeDamage(float amount, CollisionShape3D hitShape = null)
	{
		if (Field == null || hitShape == null) return;
		if (!hitShape.HasMeta("asteroid_id")) return;

		ulong asteroidId = (ulong)(long)hitShape.GetMeta("asteroid_id");
		int meshVariant  = (int)hitShape.GetMeta("mesh_variant");
		int variantIndex = (int)hitShape.GetMeta("variant_index");
		Field.HitAsteroid(asteroidId, meshVariant, variantIndex, hitShape, (int)amount);
	}
}
