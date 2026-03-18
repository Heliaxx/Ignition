using Godot;

public partial class CombatState : State
{
	protected Fighter Fighter => Entity as Fighter;
}
