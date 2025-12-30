namespace StateMachinePatterns.Core;

/// <summary>
/// Base class for contexts that use the State Pattern.
/// Maintains a reference to the current state and delegates state-specific behavior to it.
/// </summary>
/// <typeparam name="TContext">The derived context type</typeparam>
public abstract class StateContext<TContext> where TContext : StateContext<TContext>
{
    private IState<TContext>? _currentState;
    
    /// <summary>
    /// Gets the current state
    /// </summary>
    public IState<TContext>? CurrentState => _currentState;
    
    /// <summary>
    /// Transition to a new state
    /// </summary>
    public void TransitionTo(IState<TContext> newState)
    {
        _currentState?.OnExit((TContext)this);
        _currentState = newState;
        _currentState?.OnEnter((TContext)this);
    }
    
    /// <summary>
    /// Execute the current state's behavior
    /// </summary>
    public void Execute()
    {
        _currentState?.Execute((TContext)this);
    }
}
