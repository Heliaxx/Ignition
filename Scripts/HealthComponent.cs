using Godot;
using System;

public partial class HealthComponent : Node
{
    [Export] public float MaxHealth { get; set; } = 100.0f;
    public float CurrentHealth { get; private set; }

    public bool IsDead => CurrentHealth <= 0;

    [Signal] public delegate void HealthChangedEventHandler(float current, float max);
    [Signal] public delegate void DiedEventHandler();

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

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