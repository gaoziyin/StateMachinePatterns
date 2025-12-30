namespace EventDrivenStatePattern;

/// <summary>
/// 状态转换参数
/// </summary>
public class TransitionArgs<TState, TEvent> : EventArgs where TState : Enum where TEvent : Enum
{
    public TState Source { get; }
    public TState Destination { get; }
    public TEvent Trigger { get; }
    public bool IsReentry { get; }
    
    public TransitionArgs(TState source, TState destination, TEvent trigger, bool isReentry = false)
    {
        Source = source;
        Destination = destination;
        Trigger = trigger;
        IsReentry = isReentry;
    }
}

/// <summary>
/// 事件驱动状态机
/// </summary>
public class StateMachine<TState, TEvent> where TState : Enum where TEvent : Enum
{
    private TState _currentState;
    private readonly Dictionary<TState, StateConfiguration<TState, TEvent>> _configurations = new();
    private readonly object _lock = new();
    
    public TState CurrentState => _currentState;
    public bool IsInState(TState state) => _currentState.Equals(state);
    
    /// <summary>
    /// 状态转换事件
    /// </summary>
    public event EventHandler<TransitionArgs<TState, TEvent>>? OnTransitioned;
    
    /// <summary>
    /// 未处理事件事件
    /// </summary>
    public event EventHandler<(TState State, TEvent Trigger)>? OnUnhandledTrigger;
    
    public StateMachine(TState initialState)
    {
        _currentState = initialState;
    }
    
    /// <summary>
    /// 配置状态
    /// </summary>
    public StateConfiguration<TState, TEvent> Configure(TState state)
    {
        if (!_configurations.TryGetValue(state, out var config))
        {
            config = new StateConfiguration<TState, TEvent>(state, this);
            _configurations[state] = config;
        }
        return config;
    }
    
    /// <summary>
    /// 触发事件
    /// </summary>
    public void Fire(TEvent trigger)
    {
        lock (_lock)
        {
            FireInternal(trigger);
        }
    }
    
    /// <summary>
    /// 异步触发事件
    /// </summary>
    public async Task FireAsync(TEvent trigger)
    {
        await Task.Run(() => Fire(trigger));
    }
    
    private void FireInternal(TEvent trigger)
    {
        Console.WriteLine($"\n[事件] {trigger} (当前状态: {_currentState})");
        
        if (!_configurations.TryGetValue(_currentState, out var config))
        {
            HandleUnhandledTrigger(trigger);
            return;
        }
        
        // 查找匹配的转换
        var transition = FindValidTransition(config, trigger);
        
        if (transition == null)
        {
            HandleUnhandledTrigger(trigger);
            return;
        }
        
        if (transition.IsIgnored)
        {
            Console.WriteLine($"  [忽略] 事件 {trigger} 被忽略");
            return;
        }
        
        ExecuteTransition(transition, trigger);
    }
    
    private Transition<TState, TEvent>? FindValidTransition(
        StateConfiguration<TState, TEvent> config, 
        TEvent trigger)
    {
        foreach (var transition in config.Transitions)
        {
            if (!transition.Trigger.Equals(trigger))
                continue;
                
            // 检查守卫条件
            if (transition.Guard != null)
            {
                var guardResult = transition.Guard();
                Console.WriteLine($"  [守卫] {transition.GuardDescription ?? "条件检查"}: {(guardResult ? "通过" : "不通过")}");
                
                if (!guardResult)
                    continue;
            }
            
            return transition;
        }
        
        return null;
    }
    
    private void ExecuteTransition(Transition<TState, TEvent> transition, TEvent trigger)
    {
        var source = _currentState;
        var destination = transition.Destination;
        var isReentry = source.Equals(destination);
        
        // 执行退出动作
        if (_configurations.TryGetValue(source, out var sourceConfig))
        {
            foreach (var action in sourceConfig.OnExitActions)
            {
                action().GetAwaiter().GetResult();
            }
        }
        
        Console.WriteLine($"  [转换] {source} → {destination}");
        
        // 更新状态
        _currentState = destination;
        
        // 执行进入动作
        if (_configurations.TryGetValue(destination, out var destConfig))
        {
            foreach (var action in destConfig.OnEntryActions)
            {
                action().GetAwaiter().GetResult();
            }
        }
        
        // 触发事件
        OnTransitioned?.Invoke(this, new TransitionArgs<TState, TEvent>(
            source, destination, trigger, isReentry));
    }
    
    private void HandleUnhandledTrigger(TEvent trigger)
    {
        Console.WriteLine($"  [警告] 状态 {_currentState} 未处理事件 {trigger}");
        OnUnhandledTrigger?.Invoke(this, (_currentState, trigger));
    }
    
    /// <summary>
    /// 检查是否可以触发某个事件
    /// </summary>
    public bool CanFire(TEvent trigger)
    {
        if (!_configurations.TryGetValue(_currentState, out var config))
            return false;
            
        return FindValidTransition(config, trigger) != null;
    }
    
    /// <summary>
    /// 获取当前状态可触发的事件列表
    /// </summary>
    public IEnumerable<TEvent> GetPermittedTriggers()
    {
        if (!_configurations.TryGetValue(_currentState, out var config))
            yield break;
            
        foreach (var transition in config.Transitions)
        {
            if (transition.Guard == null || transition.Guard())
            {
                yield return transition.Trigger;
            }
        }
    }
    
    /// <summary>
    /// 生成状态图（DOT格式）
    /// </summary>
    public string GenerateDotGraph()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("digraph StateMachine {");
        sb.AppendLine("  rankdir=LR;");
        sb.AppendLine($"  node [shape=circle];");
        sb.AppendLine($"  \"{_currentState}\" [style=filled, fillcolor=lightblue];");
        
        foreach (var (state, config) in _configurations)
        {
            foreach (var transition in config.Transitions)
            {
                if (transition.IsIgnored) continue;
                
                var label = transition.GuardDescription != null 
                    ? $"{transition.Trigger}\\n[{transition.GuardDescription}]"
                    : transition.Trigger.ToString();
                    
                sb.AppendLine($"  \"{state}\" -> \"{transition.Destination}\" [label=\"{label}\"];");
            }
        }
        
        sb.AppendLine("}");
        return sb.ToString();
    }
}
