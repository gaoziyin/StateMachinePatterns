namespace HistoryStatePattern;

/// <summary>
/// 历史类型
/// </summary>
public enum HistoryType
{
    /// <summary>
    /// 浅历史 - 只记住当前层级的状态
    /// </summary>
    Shallow,
    
    /// <summary>
    /// 深历史 - 记住完整的嵌套状态路径
    /// </summary>
    Deep
}

/// <summary>
/// 状态快照 - 记录状态的完整信息
/// </summary>
public record StateSnapshot(
    string StateName,
    Dictionary<string, object> StateData,
    StateSnapshot? SubStateSnapshot = null)
{
    public override string ToString()
    {
        if (SubStateSnapshot != null)
        {
            return $"{StateName} > {SubStateSnapshot}";
        }
        return StateName;
    }
}

/// <summary>
/// 状态接口 - 支持历史记录
/// </summary>
public interface IState
{
    string StateName { get; }
    
    void OnEnter(StateMachineContext context);
    void OnExit(StateMachineContext context);
    void Handle(StateMachineContext context);
    
    /// <summary>
    /// 创建状态快照
    /// </summary>
    StateSnapshot CreateSnapshot();
    
    /// <summary>
    /// 从快照恢复
    /// </summary>
    void RestoreFromSnapshot(StateSnapshot snapshot, StateMachineContext context);
}
