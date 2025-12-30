namespace EventDrivenStatePattern;

/// <summary>
/// 状态转换配置 - 流式API构建器
/// </summary>
public class StateConfiguration<TState, TEvent> where TState : Enum where TEvent : Enum
{
    private readonly TState _state;
    private readonly StateMachine<TState, TEvent> _machine;
    private readonly List<Transition<TState, TEvent>> _transitions = [];
    private readonly List<Func<Task>> _onEntryActions = [];
    private readonly List<Func<Task>> _onExitActions = [];
    
    internal IReadOnlyList<Transition<TState, TEvent>> Transitions => _transitions;
    internal IReadOnlyList<Func<Task>> OnEntryActions => _onEntryActions;
    internal IReadOnlyList<Func<Task>> OnExitActions => _onExitActions;
    
    internal StateConfiguration(TState state, StateMachine<TState, TEvent> machine)
    {
        _state = state;
        _machine = machine;
    }
    
    /// <summary>
    /// 允许无条件转换
    /// </summary>
    public StateConfiguration<TState, TEvent> Permit(TEvent trigger, TState destination)
    {
        _transitions.Add(new Transition<TState, TEvent>(
            _state, trigger, destination, null, null));
        return this;
    }
    
    /// <summary>
    /// 允许带守卫条件的转换
    /// </summary>
    public StateConfiguration<TState, TEvent> PermitIf(
        TEvent trigger, 
        TState destination, 
        Func<bool> guard,
        string? guardDescription = null)
    {
        _transitions.Add(new Transition<TState, TEvent>(
            _state, trigger, destination, guard, guardDescription));
        return this;
    }
    
    /// <summary>
    /// 允许带异步守卫条件的转换
    /// </summary>
    public StateConfiguration<TState, TEvent> PermitIfAsync(
        TEvent trigger,
        TState destination,
        Func<Task<bool>> asyncGuard,
        string? guardDescription = null)
    {
        _transitions.Add(new Transition<TState, TEvent>(
            _state, trigger, destination, () => asyncGuard().GetAwaiter().GetResult(), guardDescription));
        return this;
    }
    
    /// <summary>
    /// 忽略某个事件（不抛异常）
    /// </summary>
    public StateConfiguration<TState, TEvent> Ignore(TEvent trigger)
    {
        _transitions.Add(new Transition<TState, TEvent>(
            _state, trigger, _state, null, null, isIgnored: true));
        return this;
    }
    
    /// <summary>
    /// 进入状态时的动作
    /// </summary>
    public StateConfiguration<TState, TEvent> OnEntry(Action action)
    {
        _onEntryActions.Add(() => { action(); return Task.CompletedTask; });
        return this;
    }
    
    /// <summary>
    /// 进入状态时的异步动作
    /// </summary>
    public StateConfiguration<TState, TEvent> OnEntryAsync(Func<Task> asyncAction)
    {
        _onEntryActions.Add(asyncAction);
        return this;
    }
    
    /// <summary>
    /// 退出状态时的动作
    /// </summary>
    public StateConfiguration<TState, TEvent> OnExit(Action action)
    {
        _onExitActions.Add(() => { action(); return Task.CompletedTask; });
        return this;
    }
    
    /// <summary>
    /// 退出状态时的异步动作
    /// </summary>
    public StateConfiguration<TState, TEvent> OnExitAsync(Func<Task> asyncAction)
    {
        _onExitActions.Add(asyncAction);
        return this;
    }
}

/// <summary>
/// 状态转换定义
/// </summary>
public record Transition<TState, TEvent>(
    TState Source,
    TEvent Trigger,
    TState Destination,
    Func<bool>? Guard,
    string? GuardDescription,
    bool IsIgnored = false) where TState : Enum where TEvent : Enum;
