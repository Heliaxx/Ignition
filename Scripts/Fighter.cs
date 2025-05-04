using Godot;
using System;

public partial class Fighter : CharacterBody3D
{
    private const int MAX_HEALTH = 100;
    private int currentHealth = MAX_HEALTH;

    [Export]
    public float Speed = 100.0f;

    [Export]
    public float StopDistance = 100.0f;

    [Export]
    public float Acceleration = 10.0f;

    [Export]
    public float TurnSpeed = 2.0f; // For smooth rotation

    private Node3D player;

    public override void _Ready()
    {
        currentHealth = MAX_HEALTH;
        player = GetNode<Node3D>("../Player"); // Adjust if player path changes
        if (player == null)
        {
            GD.PrintErr("Player not found in scene.");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (player != null)
        {
            Vector3 toPlayer = player.GlobalPosition - GlobalPosition;
            float distance = toPlayer.Length();
            Vector3 desiredVelocity = Vector3.Zero;

            if (distance > StopDistance)
            {
                Vector3 direction = toPlayer.Normalized();
                desiredVelocity = direction * Speed;
            }

            Velocity = Velocity.Lerp(desiredVelocity, (float)(Acceleration * delta));
            MoveAndSlide();

            // Gradual smooth rotation
            Vector3 targetDirection = toPlayer.Normalized();
            Vector3 currentForward = -GlobalTransform.Basis.Z;

            Vector3 newForward = currentForward.Lerp(targetDirection, (float)(TurnSpeed * delta)).Normalized();
            Basis newBasis = Basis.LookingAt(newForward, Vector3.Up);
            GlobalTransform = new Transform3D(newBasis, GlobalTransform.Origin);
        }
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
