namespace NestedStatePattern;

/// <summary>
/// 状态上下文 - 管理状态层级
/// </summary>
public class StateContext
{
    private IState _currentState;
    private readonly Stack<IState> _stateHistory = new();
    
    /// <summary>
    /// 状态变更事件
    /// </summary>
    public event Action<IState, IState>? StateChanged;
    
    public StateContext(IState initialState)
    {
        _currentState = initialState;
        Console.WriteLine($"[初始化] 状态: {_currentState.GetFullStatePath()}");
        _currentState.OnEnter(this);
    }
    
    /// <summary>
    /// 设置新状态
    /// </summary>
    public void SetState(IState newState)
    {
        var previousState = _currentState;
        
        // 保存历史
        _stateHistory.Push(_currentState);
        
        // 退出当前状态
        _currentState.OnExit(this);
        
        Console.WriteLine($"\n[状态转换] {_currentState.GetStateName()} -> {newState.GetStateName()}");
        
        _currentState = newState;
        
        // 进入新状态
        _currentState.OnEnter(this);
        
        // 触发事件
        StateChanged?.Invoke(previousState, newState);
    }
    
    /// <summary>
    /// 获取当前状态
    /// </summary>
    public IState GetCurrentState() => _currentState;
    
    /// <summary>
    /// 获取完整状态路径
    /// </summary>
    public string GetFullStatePath() => _currentState.GetFullStatePath();
    
    /// <summary>
    /// 处理请求
    /// </summary>
    public void Request()
    {
        Console.WriteLine($"\n{'='u8.ToArray().Length}=== 处理请求 ===");
        Console.WriteLine($"[当前状态路径] {GetFullStatePath()}");
        _currentState.Handle(this);
    }
    
    /// <summary>
    /// 获取状态历史
    /// </summary>
    public IEnumerable<IState> GetStateHistory() => _stateHistory;
    
    /// <summary>
    /// 打印状态树
    /// </summary>
    public void PrintStateTree()
    {
        Console.WriteLine("\n[状态树]");
        PrintStateTreeRecursive(_currentState, 0);
    }
    
    private void PrintStateTreeRecursive(IState state, int level)
    {
        var indent = new string(' ', level * 2);
        var marker = state == _currentState || IsActiveState(state) ? "●" : "○";
        Console.WriteLine($"{indent}{marker} {state.GetStateName()}");
        
        if (state.IsComposite && state.GetActiveSubState() != null)
        {
            PrintStateTreeRecursive(state.GetActiveSubState()!, level + 1);
        }
    }
    
    private bool IsActiveState(IState state)
    {
        var current = _currentState;
        while (current != null)
        {
            if (current == state) return true;
            current = current.GetActiveSubState();
        }
        return false;
    }
}
