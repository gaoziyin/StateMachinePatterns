namespace StatePattern;

/// <summary>
/// 上下文类 - 维护当前状态并委托行为给状态对象
/// </summary>
public class Context
{
    private IState _currentState;
    
    public Context(IState initialState)
    {
        _currentState = initialState;
        Console.WriteLine($"初始状态: {_currentState.GetStateName()}");
    }
    
    /// <summary>
    /// 改变当前状态
    /// </summary>
    public void SetState(IState state)
    {
        Console.WriteLine($"状态转换: {_currentState.GetStateName()} -> {state.GetStateName()}");
        _currentState = state;
    }
    
    /// <summary>
    /// 获取当前状态
    /// </summary>
    public IState GetCurrentState() => _currentState;
    
    /// <summary>
    /// 请求处理 - 委托给当前状态
    /// </summary>
    public void Request()
    {
        Console.WriteLine($"\n当前状态: {_currentState.GetStateName()}");
        _currentState.Handle(this);
    }
}
