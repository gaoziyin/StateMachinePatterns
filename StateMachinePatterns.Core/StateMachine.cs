namespace StateMachinePatterns.Core;

/// <summary>
/// Represents a state machine with explicit state transitions.
/// This pattern is useful when you need to validate and control transitions between states.
/// </summary>
/// <typeparam name="TState">The state enum type</typeparam>
/// <typeparam name="TTrigger">The trigger/event enum type</typeparam>
public class StateMachine<TState, TTrigger> 
    where TState : struct, Enum 
    where TTrigger : struct, Enum
{
    private readonly Dictionary<TState, StateConfiguration<TState, TTrigger>> _stateConfigurations = new();
    private TState _currentState;
    
    /// <summary>
    /// Gets the current state
    /// </summary>
    public TState CurrentState => _currentState;
    
    /// <summary>
    /// Event raised when a transition occurs
    /// </summary>
    public event EventHandler<StateTransitionEventArgs<TState, TTrigger>>? OnTransition;
    
    public StateMachine(TState initialState)
    {
        _currentState = initialState;
    }
    
    /// <summary>
    /// Configure a state with allowed transitions
    /// </summary>
    public StateConfiguration<TState, TTrigger> Configure(TState state)
    {
        if (!_stateConfigurations.TryGetValue(state, out var config))
        {
            config = new StateConfiguration<TState, TTrigger>(state);
            _stateConfigurations[state] = config;
        }
        return config;
    }
    
    /// <summary>
    /// Fire a trigger to potentially transition to a new state
    /// </summary>
    public bool Fire(TTrigger trigger)
    {
        if (!_stateConfigurations.TryGetValue(_currentState, out var config))
        {
            return false;
        }
        
        if (config.TryGetTransition(trigger, out var targetState))
        {
            var previousState = _currentState;
            
            // Execute exit action
            config.ExecuteExitAction();
            
            _currentState = targetState;
            
            // Execute entry action
            if (_stateConfigurations.TryGetValue(_currentState, out var newConfig))
            {
                newConfig.ExecuteEntryAction();
            }
            
            // Raise transition event
            OnTransition?.Invoke(this, new StateTransitionEventArgs<TState, TTrigger>(
                previousState, targetState, trigger));
            
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if a trigger can be fired from the current state
    /// </summary>
    public bool CanFire(TTrigger trigger)
    {
        if (!_stateConfigurations.TryGetValue(_currentState, out var config))
        {
            return false;
        }
        
        return config.HasTransition(trigger);
    }
}

/// <summary>
/// Configuration for a specific state
/// </summary>
public class StateConfiguration<TState, TTrigger> 
    where TState : struct, Enum 
    where TTrigger : struct, Enum
{
    private readonly TState _state;
    private readonly Dictionary<TTrigger, TState> _transitions = new();
    private Action? _onEntry;
    private Action? _onExit;
    
    public StateConfiguration(TState state)
    {
        _state = state;
    }
    
    /// <summary>
    /// Define a transition from this state triggered by the specified trigger
    /// </summary>
    public StateConfiguration<TState, TTrigger> Permit(TTrigger trigger, TState targetState)
    {
        _transitions[trigger] = targetState;
        return this;
    }
    
    /// <summary>
    /// Define an action to execute when entering this state
    /// </summary>
    public StateConfiguration<TState, TTrigger> OnEntry(Action action)
    {
        _onEntry = action;
        return this;
    }
    
    /// <summary>
    /// Define an action to execute when exiting this state
    /// </summary>
    public StateConfiguration<TState, TTrigger> OnExit(Action action)
    {
        _onExit = action;
        return this;
    }
    
    internal bool TryGetTransition(TTrigger trigger, out TState targetState)
    {
        return _transitions.TryGetValue(trigger, out targetState);
    }
    
    internal bool HasTransition(TTrigger trigger)
    {
        return _transitions.ContainsKey(trigger);
    }
    
    internal void ExecuteEntryAction()
    {
        _onEntry?.Invoke();
    }
    
    internal void ExecuteExitAction()
    {
        _onExit?.Invoke();
    }
}

/// <summary>
/// Event arguments for state transitions
/// </summary>
public class StateTransitionEventArgs<TState, TTrigger> : EventArgs 
    where TState : struct, Enum 
    where TTrigger : struct, Enum
{
    public TState FromState { get; }
    public TState ToState { get; }
    public TTrigger Trigger { get; }
    
    public StateTransitionEventArgs(TState fromState, TState toState, TTrigger trigger)
    {
        FromState = fromState;
        ToState = toState;
        Trigger = trigger;
    }
}
