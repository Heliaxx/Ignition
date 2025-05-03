using Godot;
using System;

public partial class Fighter : CharacterBody3D
{
	private const int MAX_HEALTH = 100;

	private int currentHealth = MAX_HEALTH;

	public override void _Ready()
	{
		currentHealth = MAX_HEALTH;
	}

	public void Hit(int damage = 10)
	{
		currentHealth -= damage;
		GD.Print($"{Name} hit! Health: {currentHealth}");

		if (currentHealth <= 0)
		{
			Die();
		}
	}

	private void Die()
	{
		GD.Print($"{Name} destroyed!");
		QueueFree();
	}
}
