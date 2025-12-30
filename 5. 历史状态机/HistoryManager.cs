namespace HistoryStatePattern;

/// <summary>
/// 历史记录管理器
/// </summary>
public class HistoryManager
{
    private readonly Stack<StateSnapshot> _undoStack = new();
    private readonly Stack<StateSnapshot> _redoStack = new();
    private readonly int _maxHistorySize;
    
    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    
    public HistoryManager(int maxHistorySize = 50)
    {
        _maxHistorySize = maxHistorySize;
    }
    
    /// <summary>
    /// 保存状态快照
    /// </summary>
    public void SaveSnapshot(StateSnapshot snapshot)
    {
        _undoStack.Push(snapshot);
        _redoStack.Clear(); // 新操作清除重做栈
        
        // 限制历史大小
        while (_undoStack.Count > _maxHistorySize)
        {
            var items = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = 0; i < items.Length - 1; i++)
            {
                _undoStack.Push(items[i]);
            }
        }
        
        Console.WriteLine($"  [历史] 保存快照: {snapshot} (可撤销: {UndoCount})");
    }
    
    /// <summary>
    /// 获取撤销快照
    /// </summary>
    public StateSnapshot? GetUndoSnapshot(StateSnapshot currentSnapshot)
    {
        if (!CanUndo) return null;
        
        _redoStack.Push(currentSnapshot);
        return _undoStack.Pop();
    }
    
    /// <summary>
    /// 获取重做快照
    /// </summary>
    public StateSnapshot? GetRedoSnapshot(StateSnapshot currentSnapshot)
    {
        if (!CanRedo) return null;
        
        _undoStack.Push(currentSnapshot);
        return _redoStack.Pop();
    }
    
    /// <summary>
    /// 清除历史
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
    
    /// <summary>
    /// 获取历史列表
    /// </summary>
    public IEnumerable<StateSnapshot> GetHistory()
    {
        return _undoStack.Reverse();
    }
}
