namespace HistoryStatePattern;

/// <summary>
/// 状态机上下文 - 支持历史记录
/// </summary>
public class StateMachineContext
{
    private IState _currentState;
    private readonly HistoryManager _historyManager;
    private readonly HistoryType _historyType;
    private bool _isPaused = false;
    private StateSnapshot? _pausedSnapshot;
    
    public IState CurrentState => _currentState;
    public bool IsPaused => _isPaused;
    public HistoryManager History => _historyManager;
    
    /// <summary>
    /// 状态变更事件
    /// </summary>
    public event Action<IState, IState, string>? StateChanged;
    
    public StateMachineContext(IState initialState, HistoryType historyType = HistoryType.Deep)
    {
        _currentState = initialState;
        _historyType = historyType;
        _historyManager = new HistoryManager();
        
        Console.WriteLine($"[初始化] 状态: {_currentState.StateName}, 历史类型: {_historyType}");
        _currentState.OnEnter(this);
    }
    
    /// <summary>
    /// 切换状态
    /// </summary>
    public void TransitionTo(IState newState, bool saveHistory = true)
    {
        if (_isPaused)
        {
            Console.WriteLine("  [警告] 状态机已暂停，无法转换状态");
            return;
        }
        
        var previousState = _currentState;
        
        // 保存历史
        if (saveHistory)
        {
            var snapshot = _currentState.CreateSnapshot();
            _historyManager.SaveSnapshot(snapshot);
        }
        
        // 退出当前状态
        _currentState.OnExit(this);
        
        Console.WriteLine($"\n[状态转换] {_currentState.StateName} → {newState.StateName}");
        
        // 进入新状态
        _currentState = newState;
        _currentState.OnEnter(this);
        
        // 触发事件
        StateChanged?.Invoke(previousState, newState, "transition");
    }
    
    /// <summary>
    /// 处理请求
    /// </summary>
    public void Handle()
    {
        if (_isPaused)
        {
            Console.WriteLine("  [警告] 状态机已暂停");
            return;
        }
        
        Console.WriteLine($"\n[处理] 当前状态: {_currentState.StateName}");
        _currentState.Handle(this);
    }
    
    /// <summary>
    /// 暂停状态机 - 保存当前状态
    /// </summary>
    public void Pause()
    {
        if (_isPaused) return;
        
        _pausedSnapshot = _currentState.CreateSnapshot();
        _isPaused = true;
        
        Console.WriteLine($"\n[暂停] 保存状态: {_pausedSnapshot}");
    }
    
    /// <summary>
    /// 恢复状态机 - 回到暂停时的状态
    /// </summary>
    public void Resume()
    {
        if (!_isPaused || _pausedSnapshot == null) return;
        
        Console.WriteLine($"\n[恢复] 恢复到状态: {_pausedSnapshot}");
        
        _isPaused = false;
        _currentState.RestoreFromSnapshot(_pausedSnapshot, this);
        _pausedSnapshot = null;
    }
    
    /// <summary>
    /// 撤销 - 回到上一个状态
    /// </summary>
    public bool Undo()
    {
        if (_isPaused)
        {
            Console.WriteLine("  [警告] 状态机已暂停，无法撤销");
            return false;
        }
        
        var currentSnapshot = _currentState.CreateSnapshot();
        var previousSnapshot = _historyManager.GetUndoSnapshot(currentSnapshot);
        
        if (previousSnapshot == null)
        {
            Console.WriteLine("\n[撤销] 没有可撤销的历史");
            return false;
        }
        
        Console.WriteLine($"\n[撤销] {currentSnapshot} → {previousSnapshot}");
        
        _currentState.OnExit(this);
        _currentState.RestoreFromSnapshot(previousSnapshot, this);
        
        StateChanged?.Invoke(_currentState, _currentState, "undo");
        return true;
    }
    
    /// <summary>
    /// 重做 - 回到撤销前的状态
    /// </summary>
    public bool Redo()
    {
        if (_isPaused)
        {
            Console.WriteLine("  [警告] 状态机已暂停，无法重做");
            return false;
        }
        
        var currentSnapshot = _currentState.CreateSnapshot();
        var nextSnapshot = _historyManager.GetRedoSnapshot(currentSnapshot);
        
        if (nextSnapshot == null)
        {
            Console.WriteLine("\n[重做] 没有可重做的历史");
            return false;
        }
        
        Console.WriteLine($"\n[重做] {currentSnapshot} → {nextSnapshot}");
        
        _currentState.OnExit(this);
        _currentState.RestoreFromSnapshot(nextSnapshot, this);
        
        StateChanged?.Invoke(_currentState, _currentState, "redo");
        return true;
    }
    
    /// <summary>
    /// 打印状态信息
    /// </summary>
    public void PrintStatus()
    {
        Console.WriteLine($"\n╔═══════════════════════════════════════════════════════╗");
        Console.WriteLine($"║  当前状态: {_currentState.StateName,-20}             ║");
        Console.WriteLine($"║  暂停状态: {(_isPaused ? "是" : "否"),-5}  可撤销: {_historyManager.UndoCount,-3}  可重做: {_historyManager.RedoCount,-3}  ║");
        Console.WriteLine($"╚═══════════════════════════════════════════════════════╝");
    }
    
    /// <summary>
    /// 打印历史记录
    /// </summary>
    public void PrintHistory()
    {
        Console.WriteLine("\n[历史记录]");
        var history = _historyManager.GetHistory().ToList();
        if (history.Count == 0)
        {
            Console.WriteLine("  (空)");
            return;
        }
        
        for (int i = 0; i < history.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {history[i]}");
        }
    }
}
