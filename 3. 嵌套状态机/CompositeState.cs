namespace NestedStatePattern;

/// <summary>
/// 复合状态基类 - 包含子状态机
/// </summary>
public abstract class CompositeState : IState
{
    protected IState? _currentSubState;
    protected readonly List<IState> _subStates = [];
    
    /// <summary>
    /// 初始化子状态机
    /// </summary>
    protected abstract void InitializeSubStates();
    
    /// <summary>
    /// 获取初始子状态
    /// </summary>
    protected abstract IState GetInitialSubState();
    
    public virtual void Handle(StateContext context)
    {
        // 先处理当前子状态
        if (_currentSubState != null)
        {
            Console.WriteLine($"  [复合状态 {GetStateName()}] 委托给子状态: {_currentSubState.GetStateName()}");
            _currentSubState.Handle(context);
        }
    }
    
    public virtual void OnEnter(StateContext context)
    {
        Console.WriteLine($"  → 进入复合状态: {GetStateName()}");
        
        // 初始化并进入初始子状态
        InitializeSubStates();
        _currentSubState = GetInitialSubState();
        _currentSubState?.OnEnter(context);
    }
    
    public virtual void OnExit(StateContext context)
    {
        // 先退出当前子状态
        _currentSubState?.OnExit(context);
        _currentSubState = null;
        
        Console.WriteLine($"  ← 退出复合状态: {GetStateName()}");
    }
    
    public abstract string GetStateName();
    
    public bool IsComposite => true;
    
    public IState? GetActiveSubState() => _currentSubState;
    
    public string GetFullStatePath()
    {
        if (_currentSubState != null)
        {
            return $"{GetStateName()} > {_currentSubState.GetFullStatePath()}";
        }
        return GetStateName();
    }
    
    /// <summary>
    /// 切换子状态
    /// </summary>
    protected void SetSubState(StateContext context, IState newSubState)
    {
        if (_currentSubState != null)
        {
            _currentSubState.OnExit(context);
        }
        
        Console.WriteLine($"  [子状态转换] {_currentSubState?.GetStateName() ?? "无"} -> {newSubState.GetStateName()}");
        
        _currentSubState = newSubState;
        _currentSubState.OnEnter(context);
    }
    
    /// <summary>
    /// 检查子状态机是否完成
    /// </summary>
    public abstract bool IsSubStateMachineComplete();
}
