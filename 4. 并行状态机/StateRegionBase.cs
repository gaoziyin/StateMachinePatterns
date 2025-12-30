namespace ParallelStatePattern;

/// <summary>
/// 状态区域基类
/// </summary>
public abstract class StateRegionBase<TState> : IStateRegion where TState : Enum
{
    protected TState _currentState;
    protected TState _initialState;
    
    public string RegionName { get; }
    public TState CurrentState => _currentState;
    public string CurrentStateName => _currentState.ToString();
    
    protected StateRegionBase(string regionName, TState initialState)
    {
        RegionName = regionName;
        _initialState = initialState;
        _currentState = initialState;
    }
    
    public abstract void Update(GameCharacter character, float deltaTime);
    public abstract bool HandleEvent(GameCharacter character, GameEvent gameEvent);
    
    protected void TransitionTo(TState newState, GameCharacter character)
    {
        if (!_currentState.Equals(newState))
        {
            Console.WriteLine($"  [{RegionName}] {_currentState} → {newState}");
            OnExit(_currentState, character);
            _currentState = newState;
            OnEnter(_currentState, character);
        }
    }
    
    protected virtual void OnEnter(TState state, GameCharacter character) { }
    protected virtual void OnExit(TState state, GameCharacter character) { }
    
    public void Reset()
    {
        _currentState = _initialState;
    }
}
