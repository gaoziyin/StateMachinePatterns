namespace PersistentStatePattern.Core;

/// <summary>
/// 持久化状态基类
/// </summary>
public abstract class PersistentStateBase : IPersistentState
{
    private readonly Dictionary<string, Func<IStateMachineContext, CancellationToken, Task<bool>>> _transitions = new();
    private readonly Dictionary<string, string> _transitionTargets = new();
    
    public abstract string Name { get; }
    public virtual bool IsFinal => false;
    
    /// <summary>
    /// 配置转换
    /// </summary>
    protected void Permit(string trigger, string targetState, Func<IStateMachineContext, CancellationToken, Task<bool>>? guard = null)
    {
        _transitionTargets[trigger] = targetState;
        _transitions[trigger] = guard ?? ((_, _) => Task.FromResult(true));
    }
    
    /// <summary>
    /// 配置无条件转换
    /// </summary>
    protected void Permit(string trigger, string targetState)
    {
        Permit(trigger, targetState, null);
    }
    
    /// <summary>
    /// 配置带同步条件的转换
    /// </summary>
    protected void PermitIf(string trigger, string targetState, Func<IStateMachineContext, bool> guard)
    {
        Permit(trigger, targetState, (ctx, _) => Task.FromResult(guard(ctx)));
    }
    
    public virtual Task OnEnterAsync(IStateMachineContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"    [进入] {Name}");
        return Task.CompletedTask;
    }
    
    public virtual Task OnExitAsync(IStateMachineContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"    [退出] {Name}");
        return Task.CompletedTask;
    }
    
    public async Task<string?> HandleTriggerAsync(string trigger, IStateMachineContext context, CancellationToken cancellationToken = default)
    {
        if (!_transitions.TryGetValue(trigger, out var guard))
            return null;
        
        if (!_transitionTargets.TryGetValue(trigger, out var target))
            return null;
        
        var allowed = await guard(context, cancellationToken);
        if (!allowed)
        {
            Console.WriteLine($"    [拒绝] 守卫条件不满足: {trigger}");
            return null;
        }
        
        return target;
    }
    
    public IEnumerable<string> GetPermittedTriggers()
    {
        return _transitionTargets.Keys;
    }
}

/// <summary>
/// 最终状态基类
/// </summary>
public abstract class FinalStateBase : PersistentStateBase
{
    public override bool IsFinal => true;
    
    public override Task OnEnterAsync(IStateMachineContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"    [完成] 进入最终状态: {Name}");
        return Task.CompletedTask;
    }
}
