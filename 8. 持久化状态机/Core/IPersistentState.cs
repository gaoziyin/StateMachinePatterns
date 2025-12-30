namespace PersistentStatePattern.Core;

/// <summary>
/// 持久化状态接口
/// </summary>
public interface IPersistentState
{
    /// <summary>
    /// 状态名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 是否为最终状态
    /// </summary>
    bool IsFinal { get; }
    
    /// <summary>
    /// 进入状态时的处理
    /// </summary>
    Task OnEnterAsync(IStateMachineContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 退出状态时的处理
    /// </summary>
    Task OnExitAsync(IStateMachineContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 处理触发器，返回目标状态名称
    /// </summary>
    Task<string?> HandleTriggerAsync(string trigger, IStateMachineContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取允许的触发器列表
    /// </summary>
    IEnumerable<string> GetPermittedTriggers();
}

/// <summary>
/// 状态机上下文接口
/// </summary>
public interface IStateMachineContext
{
    /// <summary>
    /// 实例ID
    /// </summary>
    Guid InstanceId { get; }
    
    /// <summary>
    /// 上下文数据
    /// </summary>
    IDictionary<string, object?> Data { get; }
    
    /// <summary>
    /// 获取强类型数据
    /// </summary>
    T? Get<T>(string key);
    
    /// <summary>
    /// 设置数据
    /// </summary>
    void Set<T>(string key, T value);
    
    /// <summary>
    /// 删除数据
    /// </summary>
    bool Remove(string key);
}

/// <summary>
/// 状态机事件类型
/// </summary>
public enum StateMachineEventType
{
    Created,
    StateEntered,
    StateExited,
    TransitionStarted,
    TransitionCompleted,
    TransitionFailed,
    CheckpointCreated,
    Restored,
    Completed
}

/// <summary>
/// 状态机事件参数
/// </summary>
public record StateMachineEventArgs(
    Guid InstanceId,
    StateMachineEventType EventType,
    string? FromState,
    string? ToState,
    string? Trigger,
    DateTime Timestamp,
    string? Message = null
);
