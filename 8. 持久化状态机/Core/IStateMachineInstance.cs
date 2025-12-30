namespace PersistentStatePattern.Core;

/// <summary>
/// 状态机实例接口
/// </summary>
public interface IStateMachineInstance
{
    /// <summary>
    /// 实例唯一标识
    /// </summary>
    Guid InstanceId { get; }
    
    /// <summary>
    /// 状态机类型名称
    /// </summary>
    string MachineType { get; }
    
    /// <summary>
    /// 当前状态
    /// </summary>
    string CurrentState { get; }
    
    /// <summary>
    /// 上下文数据
    /// </summary>
    IDictionary<string, object?> Context { get; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    DateTime CreatedAt { get; }
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    DateTime UpdatedAt { get; }
    
    /// <summary>
    /// 版本号（乐观锁）
    /// </summary>
    long Version { get; }
    
    /// <summary>
    /// 是否已完成
    /// </summary>
    bool IsCompleted { get; }
}

/// <summary>
/// 状态转换历史记录
/// </summary>
public record StateTransitionRecord(
    Guid Id,
    Guid InstanceId,
    string FromState,
    string ToState,
    string Trigger,
    DateTime Timestamp,
    string? Metadata
);

/// <summary>
/// 状态机检查点
/// </summary>
public record StateMachineCheckpoint(
    Guid CheckpointId,
    Guid InstanceId,
    string State,
    string ContextJson,
    DateTime CreatedAt,
    string? Description
);
