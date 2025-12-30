namespace StateMachinePatterns.Core;

/// <summary>
/// Represents a state in the State Pattern.
/// Each concrete state implements this interface and defines behavior for that state.
/// </summary>
/// <typeparam name="TContext">The context type that holds the state</typeparam>
public interface IState<TContext> where TContext : class
{
    /// <summary>
    /// Handle operations when entering this state
    /// </summary>
    void OnEnter(TContext context);
    
    /// <summary>
    /// Handle operations when exiting this state
    /// </summary>
    void OnExit(TContext context);
    
    /// <summary>
    /// Execute state-specific behavior
    /// </summary>
    void Execute(TContext context);
}
