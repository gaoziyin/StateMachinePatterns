namespace AsyncStatePattern;

/// <summary>
/// 异步上下文类 - 维护当前状态并委托行为给状态对象
/// </summary>
public class AsyncContext
{
    private IAsyncState _currentState;
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    
    /// <summary>
    /// 状态变更事件
    /// </summary>
    public event Func<IAsyncState, IAsyncState, Task>? StateChanged;
    
    public AsyncContext(IAsyncState initialState)
    {
        _currentState = initialState;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 初始状态: {_currentState.GetStateName()}");
    }
    
    /// <summary>
    /// 异步改变当前状态
    /// </summary>
    public async Task SetStateAsync(IAsyncState newState, CancellationToken cancellationToken = default)
    {
        await _stateLock.WaitAsync(cancellationToken);
        try
        {
            var previousState = _currentState;
            
            // 退出当前状态
            await _currentState.OnExitAsync(cancellationToken);
            
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 状态转换: {_currentState.GetStateName()} -> {newState.GetStateName()}");
            
            _currentState = newState;
            
            // 进入新状态
            await _currentState.OnEnterAsync(cancellationToken);
            
            // 触发状态变更事件
            if (StateChanged != null)
            {
                await StateChanged.Invoke(previousState, newState);
            }
        }
        finally
        {
            _stateLock.Release();
        }
    }
    
    /// <summary>
    /// 获取当前状态
    /// </summary>
    public IAsyncState GetCurrentState() => _currentState;
    
    /// <summary>
    /// 异步请求处理 - 委托给当前状态
    /// </summary>
    public async Task RequestAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss.fff}] 当前状态: {_currentState.GetStateName()}");
        await _currentState.HandleAsync(this, cancellationToken);
    }
}
