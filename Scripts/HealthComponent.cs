using Godot;
using System;

public partial class HealthComponent : Node
{
    [Export] public float MaxHealth { get; set; } = 100.0f;
    public float CurrentHealth { get; private set; }

    public bool IsDead => CurrentHealth <= 0;

    // Whoever dealt the most recent damage; still readable from a Died handler.
    public Node3D LastAttacker { get; private set; }

    [Signal] public delegate void HealthChangedEventHandler(float current, float max);
    [Signal] public delegate void DiedEventHandler();

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(float amount, Node3D source = null)
    {
        if (IsDead) return;

        // Self-damage keeps the previous attacker, so a shove into an asteroid still counts.
        if (source != null && source != GetParent())
            LastAttacker = source;

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0)
        {
            EmitSignal(SignalName.Died);
        }
    }

    public void Heal(float amount)
    {
        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }
}