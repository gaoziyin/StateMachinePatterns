namespace StateMachinePatterns.Core;

/// <summary>
/// Represents a hierarchical state that can contain sub-states.
/// This allows for nested state machines where states can have their own internal states.
/// </summary>
public abstract class HierarchicalState
{
    private HierarchicalState? _currentSubState;
    private HierarchicalState? _parentState;
    
    /// <summary>
    /// Gets the current sub-state, if any
    /// </summary>
    public HierarchicalState? CurrentSubState => _currentSubState;
    
    /// <summary>
    /// Gets the parent state, if any
    /// </summary>
    public HierarchicalState? ParentState => _parentState;
    
    /// <summary>
    /// Gets the name of this state
    /// </summary>
    public abstract string Name { get; }
    
    /// <summary>
    /// Called when entering this state
    /// </summary>
    public virtual void OnEnter()
    {
    }
    
    /// <summary>
    /// Called when exiting this state
    /// </summary>
    public virtual void OnExit()
    {
    }
    
    /// <summary>
    /// Handle an event. Returns true if handled, false otherwise.
    /// If not handled, the event bubbles up to the parent state.
    /// </summary>
    public virtual bool HandleEvent(string eventName)
    {
        // First, try to handle in the current sub-state
        if (_currentSubState != null && _currentSubState.HandleEvent(eventName))
        {
            return true;
        }
        
        // If not handled by sub-state, try to handle it here
        return false;
    }
    
    /// <summary>
    /// Transition to a sub-state
    /// </summary>
    protected void TransitionToSubState(HierarchicalState subState)
    {
        _currentSubState?.OnExit();
        _currentSubState = subState;
        subState._parentState = this;
        _currentSubState?.OnEnter();
    }
    
    /// <summary>
    /// Transition to a sibling state (same parent)
    /// </summary>
    protected void TransitionToSibling(HierarchicalState siblingState)
    {
        if (_parentState == null)
        {
            throw new InvalidOperationException("Cannot transition to sibling - no parent state");
        }
        
        _parentState.TransitionToSubState(siblingState);
    }
    
    /// <summary>
    /// Get the full state path (including parent states)
    /// </summary>
    public string GetFullPath()
    {
        if (_parentState == null)
        {
            return Name;
        }
        
        return $"{_parentState.GetFullPath()}/{Name}";
    }
}

/// <summary>
/// Context for managing hierarchical state machines
/// </summary>
public class HierarchicalStateMachine
{
    private HierarchicalState? _rootState;
    
    /// <summary>
    /// Gets the current root state
    /// </summary>
    public HierarchicalState? CurrentState => _rootState;
    
    /// <summary>
    /// Initialize the state machine with a root state
    /// </summary>
    public void Initialize(HierarchicalState initialState)
    {
        _rootState = initialState;
        _rootState.OnEnter();
    }
    
    /// <summary>
    /// Transition to a new root state
    /// </summary>
    public void TransitionTo(HierarchicalState newState)
    {
        _rootState?.OnExit();
        _rootState = newState;
        _rootState?.OnEnter();
    }
    
    /// <summary>
    /// Handle an event by passing it to the current state
    /// </summary>
    public bool HandleEvent(string eventName)
    {
        return _rootState?.HandleEvent(eventName) ?? false;
    }
    
    /// <summary>
    /// Get the current active state path
    /// </summary>
    public string GetCurrentStatePath()
    {
        return _rootState?.GetFullPath() ?? "None";
    }
}
