using System.Text.Json;
using System.Text.Json.Serialization;

namespace PersistentStatePattern.Core;

/// <summary>
/// 持久化状态机 - 企业级实现
/// </summary>
public class PersistentStateMachine : IStateMachineInstance
{
    private readonly IStateStore _store;
    private readonly Dictionary<string, IPersistentState> _states = new();
    private readonly Dictionary<string, object?> _context = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    
    private string _currentState;
    private DateTime _createdAt;
    private DateTime _updatedAt;
    private long _version;
    private bool _isCompleted;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    // IStateMachineInstance 实现
    public Guid InstanceId { get; }
    public string MachineType { get; }
    public string CurrentState => _currentState;
    public IDictionary<string, object?> Context => _context;
    public DateTime CreatedAt => _createdAt;
    public DateTime UpdatedAt => _updatedAt;
    public long Version => _version;
    public bool IsCompleted => _isCompleted;
    
    /// <summary>
    /// 状态变更事件
    /// </summary>
    public event EventHandler<StateMachineEventArgs>? OnEvent;
    
    /// <summary>
    /// 创建新的持久化状态机
    /// </summary>
    public PersistentStateMachine(
        string machineType,
        IStateStore store,
        string initialState,
        Guid? instanceId = null)
    {
        InstanceId = instanceId ?? Guid.NewGuid();
        MachineType = machineType;
        _store = store;
        _currentState = initialState;
        _createdAt = DateTime.UtcNow;
        _updatedAt = _createdAt;
        _version = 1;
    }
    
    /// <summary>
    /// 注册状态
    /// </summary>
    public PersistentStateMachine RegisterState(IPersistentState state)
    {
        _states[state.Name] = state;
        return this;
    }
    
    /// <summary>
    /// 注册多个状态
    /// </summary>
    public PersistentStateMachine RegisterStates(params IPersistentState[] states)
    {
        foreach (var state in states)
        {
            RegisterState(state);
        }
        return this;
    }
    
    /// <summary>
    /// 初始化状态机
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // 保存初始状态
            await SaveAsync(cancellationToken);
            
            // 执行初始状态的进入动作
            if (_states.TryGetValue(_currentState, out var state))
            {
                var context = new StateMachineContext(InstanceId, _context);
                await state.OnEnterAsync(context, cancellationToken);
            }
            
            RaiseEvent(StateMachineEventType.Created, null, _currentState, null);
        }
        finally
        {
            _lock.Release();
        }
    }
    
    /// <summary>
    /// 触发状态转换
    /// </summary>
    public async Task<bool> FireAsync(string trigger, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_isCompleted)
            {
                Console.WriteLine($"  [警告] 状态机已完成，无法触发: {trigger}");
                return false;
            }
            
            if (!_states.TryGetValue(_currentState, out var currentState))
            {
                Console.WriteLine($"  [错误] 未知状态: {_currentState}");
                return false;
            }
            
            Console.WriteLine($"\n[触发] {trigger} (当前: {_currentState})");
            
            var context = new StateMachineContext(InstanceId, _context);
            
            RaiseEvent(StateMachineEventType.TransitionStarted, _currentState, null, trigger);
            
            // 获取目标状态
            var targetStateName = await currentState.HandleTriggerAsync(trigger, context, cancellationToken);
            
            if (string.IsNullOrEmpty(targetStateName))
            {
                Console.WriteLine($"  [忽略] 触发器 {trigger} 在状态 {_currentState} 中未定义");
                RaiseEvent(StateMachineEventType.TransitionFailed, _currentState, null, trigger, "未定义的转换");
                return false;
            }
            
            if (!_states.TryGetValue(targetStateName, out var targetState))
            {
                Console.WriteLine($"  [错误] 目标状态未注册: {targetStateName}");
                RaiseEvent(StateMachineEventType.TransitionFailed, _currentState, targetStateName, trigger, "目标状态未注册");
                return false;
            }
            
            var previousState = _currentState;
            
            // 退出当前状态
            RaiseEvent(StateMachineEventType.StateExited, _currentState, null, trigger);
            await currentState.OnExitAsync(context, cancellationToken);
            
            // 记录转换
            var transitionRecord = new StateTransitionRecord(
                Guid.NewGuid(),
                InstanceId,
                previousState,
                targetStateName,
                trigger,
                DateTime.UtcNow,
                null
            );
            await _store.RecordTransitionAsync(transitionRecord, cancellationToken);
            
            // 更新状态
            _currentState = targetStateName;
            _updatedAt = DateTime.UtcNow;
            _version++;
            
            // 进入新状态
            RaiseEvent(StateMachineEventType.StateEntered, previousState, _currentState, trigger);
            await targetState.OnEnterAsync(context, cancellationToken);
            
            // 检查是否为最终状态
            if (targetState.IsFinal)
            {
                _isCompleted = true;
                RaiseEvent(StateMachineEventType.Completed, previousState, _currentState, trigger);
            }
            
            // 持久化
            await SaveAsync(cancellationToken);
            
            RaiseEvent(StateMachineEventType.TransitionCompleted, previousState, _currentState, trigger);
            Console.WriteLine($"  [转换] {previousState} → {_currentState}");
            
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }
    
    /// <summary>
    /// 创建检查点
    /// </summary>
    public async Task<Guid> CreateCheckpointAsync(string? description = null, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var checkpointId = Guid.NewGuid();
            var checkpoint = new StateMachineCheckpoint(
                checkpointId,
                InstanceId,
                _currentState,
                SerializeContext(),
                DateTime.UtcNow,
                description
            );
            
            await _store.CreateCheckpointAsync(checkpoint, cancellationToken);
            
            RaiseEvent(StateMachineEventType.CheckpointCreated, _currentState, null, null, $"检查点: {checkpointId:N}");
            Console.WriteLine($"  [检查点] 已创建: {checkpointId:N}");
            
            return checkpointId;
        }
        finally
        {
            _lock.Release();
        }
    }
    
    /// <summary>
    /// 恢复到检查点
    /// </summary>
    public async Task<bool> RestoreFromCheckpointAsync(Guid checkpointId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var checkpoint = await _store.GetCheckpointAsync(checkpointId, cancellationToken);
            if (checkpoint == null)
            {
                Console.WriteLine($"  [错误] 检查点不存在: {checkpointId:N}");
                return false;
            }
            
            var previousState = _currentState;
            _currentState = checkpoint.State;
            DeserializeContext(checkpoint.ContextJson);
            _updatedAt = DateTime.UtcNow;
            _version++;
            _isCompleted = false;
            
            await SaveAsync(cancellationToken);
            
            RaiseEvent(StateMachineEventType.Restored, previousState, _currentState, null, $"从检查点恢复: {checkpointId:N}");
            Console.WriteLine($"  [恢复] 从检查点 {checkpointId:N} 恢复到状态: {_currentState}");
            
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }
    
    /// <summary>
    /// 获取可用的触发器
    /// </summary>
    public IEnumerable<string> GetPermittedTriggers()
    {
        if (_states.TryGetValue(_currentState, out var state))
        {
            return state.GetPermittedTriggers();
        }
        return Enumerable.Empty<string>();
    }
    
    /// <summary>
    /// 获取转换历史
    /// </summary>
    public async Task<IReadOnlyList<StateTransitionRecord>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        return await _store.GetTransitionHistoryAsync(InstanceId, cancellationToken);
    }
    
    /// <summary>
    /// 获取所有检查点
    /// </summary>
    public async Task<IReadOnlyList<StateMachineCheckpoint>> GetCheckpointsAsync(CancellationToken cancellationToken = default)
    {
        return await _store.GetCheckpointsAsync(InstanceId, cancellationToken);
    }
    
    /// <summary>
    /// 保存状态
    /// </summary>
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        var data = new StateMachineData
        {
            InstanceId = InstanceId,
            MachineType = MachineType,
            CurrentState = _currentState,
            ContextJson = SerializeContext(),
            CreatedAt = _createdAt,
            UpdatedAt = _updatedAt,
            Version = _version,
            IsCompleted = _isCompleted
        };
        
        await _store.SaveInstanceAsync(data, cancellationToken);
    }
    
    /// <summary>
    /// 从存储加载状态机
    /// </summary>
    public static async Task<PersistentStateMachine?> LoadAsync(
        Guid instanceId,
        IStateStore store,
        Func<PersistentStateMachine, PersistentStateMachine> configureStates,
        CancellationToken cancellationToken = default)
    {
        var data = await store.LoadInstanceAsync(instanceId, cancellationToken);
        if (data == null)
            return null;
        
        var machine = new PersistentStateMachine(
            data.MachineType,
            store,
            data.CurrentState,
            data.InstanceId
        );
        
        machine._createdAt = data.CreatedAt;
        machine._updatedAt = data.UpdatedAt;
        machine._version = data.Version;
        machine._isCompleted = data.IsCompleted;
        machine.DeserializeContext(data.ContextJson);
        
        // 配置状态
        configureStates(machine);
        
        Console.WriteLine($"  [加载] 状态机 {instanceId:N} 已恢复到状态: {machine._currentState}");
        
        return machine;
    }
    
    private string SerializeContext()
    {
        // 将Dictionary<string, object?>转换为可序列化的格式
        var serializableContext = new Dictionary<string, JsonElement>();
        foreach (var kv in _context)
        {
            if (kv.Value != null)
            {
                var json = JsonSerializer.Serialize(kv.Value, JsonOptions);
                serializableContext[kv.Key] = JsonSerializer.Deserialize<JsonElement>(json);
            }
        }
        return JsonSerializer.Serialize(serializableContext, JsonOptions);
    }
    
    private void DeserializeContext(string json)
    {
        _context.Clear();
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        if (dict != null)
        {
            foreach (var kv in dict)
            {
                _context[kv.Key] = kv.Value;
            }
        }
    }
    
    private void RaiseEvent(StateMachineEventType eventType, string? fromState, string? toState, string? trigger, string? message = null)
    {
        OnEvent?.Invoke(this, new StateMachineEventArgs(
            InstanceId,
            eventType,
            fromState,
            toState,
            trigger,
            DateTime.UtcNow,
            message
        ));
    }
    
    /// <summary>
    /// 打印状态机状态
    /// </summary>
    public void PrintStatus()
    {
        Console.WriteLine($"\n╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║ 实例ID: {InstanceId:N}                     ║");
        Console.WriteLine($"║ 类型: {MachineType,-20} 状态: {_currentState,-15}    ║");
        Console.WriteLine($"║ 版本: {_version,-5} 已完成: {(_isCompleted ? "是" : "否"),-5}                              ║");
        Console.WriteLine($"║ 创建: {_createdAt:yyyy-MM-dd HH:mm:ss}                                    ║");
        Console.WriteLine($"║ 更新: {_updatedAt:yyyy-MM-dd HH:mm:ss}                                    ║");
        Console.WriteLine($"╠════════════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║ 可用触发器: {string.Join(", ", GetPermittedTriggers()),-40}   ║");
        Console.WriteLine($"╠════════════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║ 上下文:                                                        ║");
        foreach (var kv in _context)
        {
            Console.WriteLine($"║   {kv.Key,-20}: {kv.Value?.ToString() ?? "null",-30} ║");
        }
        Console.WriteLine($"╚════════════════════════════════════════════════════════════════╝");
    }
}

/// <summary>
/// 状态机上下文实现
/// </summary>
internal class StateMachineContext : IStateMachineContext
{
    public Guid InstanceId { get; }
    public IDictionary<string, object?> Data { get; }
    
    public StateMachineContext(Guid instanceId, IDictionary<string, object?> data)
    {
        InstanceId = instanceId;
        Data = data;
    }
    
    public T? Get<T>(string key)
    {
        if (Data.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
                return typedValue;
            if (value is System.Text.Json.JsonElement element)
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(element.GetRawText());
            }
        }
        return default;
    }
    
    public void Set<T>(string key, T value)
    {
        Data[key] = value;
    }
    
    public bool Remove(string key)
    {
        return Data.Remove(key);
    }
}
