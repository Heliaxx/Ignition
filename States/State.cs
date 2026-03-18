using Godot;
using System;

public partial class State : Node
{

	public StateMachine stateMachine;
	protected Node3D Entity;

	public virtual void Enter() {}
	public virtual void Exit() {}

	public new virtual void Ready()
	{
		Entity = stateMachine?.GetParent<Node3D>() ?? (GetParent()?.GetParent() as Node3D);
	}
	public virtual void Update(float delta) {}
	public virtual void PhysicsUpdate(float delta) {}
	public virtual void HandleInput(InputEvent @event) {}

}
