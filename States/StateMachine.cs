using Godot;
using System;
using System.Collections.Generic;

public partial class StateMachine : Node
{
    [Export] public NodePath initialState;
    [Export] public float MinStateDuration = 0.15f;
    [Export] public bool EnableTransitionLogs = false;
    
    private Dictionary<string, State> _states;
    private State _currentState;
    private float _timeInState;
    
    public override void _Ready()
    {
        _states = new Dictionary<string, State>();
        foreach (Node node in GetChildren()) {
            if (node is State s) {
                _states[node.Name] = s;
                s.stateMachine = this;
                s.Ready();
                s.Exit(); // reset
            }
        }
        _currentState = GetNode<State>(initialState);
        _timeInState = 0f;
        _currentState.Enter();
    }

    public override void _Process(double delta)
    {
        _timeInState += (float)delta;
        _currentState.Update((float)delta);
    }
    
    public override void _PhysicsProcess(double delta)
    {
        _currentState.PhysicsUpdate((float)delta);
    }
    
    public override void _UnhandledInput(InputEvent @event)
    {
        _currentState.HandleInput(@event);
    }
    
    public void TransitionTo(string key) {
        if (!_states.ContainsKey(key) || _states[key] == _currentState)
            return;

        if (_timeInState < MinStateDuration)
            return;

        _currentState.Exit();
        string previousState = _currentState.Name;
        _currentState = _states[key];
        _timeInState = 0f;

        if (EnableTransitionLogs)
        {
            GD.Print($"State transition: {previousState} -> {key}");
        }

        _currentState.Enter();
    }
    
    public void ResetToInitialState()
    {
        if (_currentState != null)
        {
            _currentState.Exit();
        }
        _currentState = GetNode<State>(initialState);
        _timeInState = 0f;
        _currentState.Enter();
    }
}