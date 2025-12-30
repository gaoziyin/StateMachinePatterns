namespace PersistentStatePattern.Core;

/// <summary>
/// 状态存储接口 - 持久化层抽象
/// </summary>
public interface IStateStore
{
    /// <summary>
    /// 初始化存储
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 保存状态机实例
    /// </summary>
    Task SaveInstanceAsync(StateMachineData data, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 加载状态机实例
    /// </summary>
    Task<StateMachineData?> LoadInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 删除状态机实例
    /// </summary>
    Task DeleteInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 查询状态机实例
    /// </summary>
    Task<IReadOnlyList<StateMachineData>> QueryInstancesAsync(
        string? machineType = null,
        string? currentState = null,
        bool? isCompleted = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 记录状态转换
    /// </summary>
    Task RecordTransitionAsync(StateTransitionRecord record, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取转换历史
    /// </summary>
    Task<IReadOnlyList<StateTransitionRecord>> GetTransitionHistoryAsync(
        Guid instanceId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 创建检查点
    /// </summary>
    Task CreateCheckpointAsync(StateMachineCheckpoint checkpoint, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取检查点列表
    /// </summary>
    Task<IReadOnlyList<StateMachineCheckpoint>> GetCheckpointsAsync(
        Guid instanceId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取指定检查点
    /// </summary>
    Task<StateMachineCheckpoint?> GetCheckpointAsync(
        Guid checkpointId, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 状态机数据传输对象
/// </summary>
public class StateMachineData
{
    public Guid InstanceId { get; set; }
    public string MachineType { get; set; } = "";
    public string CurrentState { get; set; } = "";
    public string ContextJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long Version { get; set; }
    public bool IsCompleted { get; set; }
}
