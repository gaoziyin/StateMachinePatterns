namespace HistoryStatePattern.States;

/// <summary>
/// 文档编辑器状态 - 演示深度历史
/// </summary>
public class DocumentEditorState : IState
{
    private string _content = "";
    private int _cursorPosition = 0;
    private readonly List<string> _history = new();
    
    public string StateName => $"编辑器(字数:{_content.Length})";
    public string Content => _content;
    public int CursorPosition => _cursorPosition;
    
    public DocumentEditorState() { }
    
    public DocumentEditorState(string content, int cursorPosition)
    {
        _content = content;
        _cursorPosition = cursorPosition;
    }
    
    public void OnEnter(StateMachineContext context)
    {
        Console.WriteLine($"  → 进入: {StateName}");
    }
    
    public void OnExit(StateMachineContext context)
    {
        Console.WriteLine($"  ← 退出: {StateName}");
    }
    
    public void Handle(StateMachineContext context)
    {
        Console.WriteLine($"  [编辑器] 当前内容: \"{_content}\"");
        Console.WriteLine($"  [编辑器] 光标位置: {_cursorPosition}");
    }
    
    /// <summary>
    /// 插入文本
    /// </summary>
    public void InsertText(string text, StateMachineContext context)
    {
        // 保存当前状态到历史
        context.TransitionTo(new DocumentEditorState(
            _content.Insert(_cursorPosition, text),
            _cursorPosition + text.Length
        ));
    }
    
    /// <summary>
    /// 删除文本
    /// </summary>
    public void DeleteText(int length, StateMachineContext context)
    {
        if (_cursorPosition >= length)
        {
            context.TransitionTo(new DocumentEditorState(
                _content.Remove(_cursorPosition - length, length),
                _cursorPosition - length
            ));
        }
    }
    
    /// <summary>
    /// 移动光标
    /// </summary>
    public void MoveCursor(int position, StateMachineContext context)
    {
        _cursorPosition = Math.Clamp(position, 0, _content.Length);
        Console.WriteLine($"  [编辑器] 光标移动到: {_cursorPosition}");
    }
    
    public StateSnapshot CreateSnapshot()
    {
        return new StateSnapshot("编辑器", new Dictionary<string, object>
        {
            ["content"] = _content,
            ["cursorPosition"] = _cursorPosition
        });
    }
    
    public void RestoreFromSnapshot(StateSnapshot snapshot, StateMachineContext context)
    {
        if (snapshot.StateData.TryGetValue("content", out var content))
        {
            _content = content?.ToString() ?? "";
        }
        if (snapshot.StateData.TryGetValue("cursorPosition", out var pos))
        {
            _cursorPosition = Convert.ToInt32(pos);
        }
        
        Console.WriteLine($"  [恢复] 内容: \"{_content}\", 光标: {_cursorPosition}");
        OnEnter(context);
    }
}
