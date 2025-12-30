using System.Collections.Concurrent;

namespace PersistentStatePattern.Storage;

using Core;

/// <summary>
/// 内存状态存储实现（用于测试和开发）
/// </summary>
public class InMemoryStateStore : IStateStore
{
    private readonly ConcurrentDictionary<Guid, StateMachineData> _instances = new();
    private readonly ConcurrentDictionary<Guid, List<StateTransitionRecord>> _transitions = new();
    private readonly ConcurrentDictionary<Guid, List<StateMachineCheckpoint>> _checkpoints = new();
    
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("  [InMemory] 内存存储已初始化");
        return Task.CompletedTask;
    }
    
    public Task SaveInstanceAsync(StateMachineData data, CancellationToken cancellationToken = default)
    {
        _instances.AddOrUpdate(data.InstanceId, data, (_, _) => data);
        Console.WriteLine($"  [InMemory] 保存实例: {data.InstanceId:N} 状态: {data.CurrentState}");
        return Task.CompletedTask;
    }
    
    public Task<StateMachineData?> LoadInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        _instances.TryGetValue(instanceId, out var data);
        if (data != null)
        {
            Console.WriteLine($"  [InMemory] 加载实例: {instanceId:N}");
        }
        return Task.FromResult(data);
    }
    
    public Task DeleteInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        _instances.TryRemove(instanceId, out _);
        _transitions.TryRemove(instanceId, out _);
        _checkpoints.TryRemove(instanceId, out _);
        Console.WriteLine($"  [InMemory] 删除实例: {instanceId:N}");
        return Task.CompletedTask;
    }
    
    public Task<IReadOnlyList<StateMachineData>> QueryInstancesAsync(
        string? machineType = null,
        string? currentState = null,
        bool? isCompleted = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _instances.Values.AsEnumerable();
        
        if (machineType != null)
            query = query.Where(x => x.MachineType == machineType);
        if (currentState != null)
            query = query.Where(x => x.CurrentState == currentState);
        if (isCompleted.HasValue)
            query = query.Where(x => x.IsCompleted == isCompleted.Value);
            
        var result = query.Skip(skip).Take(take).ToList();
        return Task.FromResult<IReadOnlyList<StateMachineData>>(result);
    }
    
    public Task RecordTransitionAsync(StateTransitionRecord record, CancellationToken cancellationToken = default)
    {
        var list = _transitions.GetOrAdd(record.InstanceId, _ => new List<StateTransitionRecord>());
        lock (list)
        {
            list.Add(record);
        }
        Console.WriteLine($"  [InMemory] 记录转换: {record.FromState} → {record.ToState}");
        return Task.CompletedTask;
    }
    
    public Task<IReadOnlyList<StateTransitionRecord>> GetTransitionHistoryAsync(
        Guid instanceId, 
        CancellationToken cancellationToken = default)
    {
        if (_transitions.TryGetValue(instanceId, out var list))
        {
            lock (list)
            {
                return Task.FromResult<IReadOnlyList<StateTransitionRecord>>(list.ToList());
            }
        }
        return Task.FromResult<IReadOnlyList<StateTransitionRecord>>(Array.Empty<StateTransitionRecord>());
    }
    
    public Task CreateCheckpointAsync(StateMachineCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        var list = _checkpoints.GetOrAdd(checkpoint.InstanceId, _ => new List<StateMachineCheckpoint>());
        lock (list)
        {
            list.Add(checkpoint);
        }
        Console.WriteLine($"  [InMemory] 创建检查点: {checkpoint.CheckpointId:N}");
        return Task.CompletedTask;
    }
    
    public Task<IReadOnlyList<StateMachineCheckpoint>> GetCheckpointsAsync(
        Guid instanceId, 
        CancellationToken cancellationToken = default)
    {
        if (_checkpoints.TryGetValue(instanceId, out var list))
        {
            lock (list)
            {
                return Task.FromResult<IReadOnlyList<StateMachineCheckpoint>>(list.ToList());
            }
        }
        return Task.FromResult<IReadOnlyList<StateMachineCheckpoint>>(Array.Empty<StateMachineCheckpoint>());
    }
    
    public Task<StateMachineCheckpoint?> GetCheckpointAsync(
        Guid checkpointId, 
        CancellationToken cancellationToken = default)
    {
        foreach (var list in _checkpoints.Values)
        {
            lock (list)
            {
                var checkpoint = list.FirstOrDefault(c => c.CheckpointId == checkpointId);
                if (checkpoint != null)
                    return Task.FromResult<StateMachineCheckpoint?>(checkpoint);
            }
        }
        return Task.FromResult<StateMachineCheckpoint?>(null);
    }
}
