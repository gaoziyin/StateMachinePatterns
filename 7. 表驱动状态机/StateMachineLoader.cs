using System.Text.Json;

namespace TableDrivenStatePattern;

/// <summary>
/// 状态机定义加载器
/// </summary>
public static class StateMachineLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
    
    /// <summary>
    /// 从JSON文件加载
    /// </summary>
    public static StateMachineDefinition LoadFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return LoadFromJson(json);
    }
    
    /// <summary>
    /// 从JSON字符串加载
    /// </summary>
    public static StateMachineDefinition LoadFromJson(string json)
    {
        return JsonSerializer.Deserialize<StateMachineDefinition>(json, JsonOptions)
            ?? throw new InvalidOperationException("无法解析状态机定义");
    }
    
    /// <summary>
    /// 保存到JSON文件
    /// </summary>
    public static void SaveToFile(StateMachineDefinition definition, string filePath)
    {
        var json = ToJson(definition);
        File.WriteAllText(filePath, json);
    }
    
    /// <summary>
    /// 转换为JSON字符串
    /// </summary>
    public static string ToJson(StateMachineDefinition definition)
    {
        return JsonSerializer.Serialize(definition, JsonOptions);
    }
    
    /// <summary>
    /// 使用流式API构建定义
    /// </summary>
    public static StateMachineDefinitionBuilder CreateBuilder(string id, string name)
    {
        return new StateMachineDefinitionBuilder(id, name);
    }
}

/// <summary>
/// 状态机定义构建器
/// </summary>
public class StateMachineDefinitionBuilder
{
    private readonly StateMachineDefinition _definition;
    
    internal StateMachineDefinitionBuilder(string id, string name)
    {
        _definition = new StateMachineDefinition
        {
            Id = id,
            Name = name,
            States = new Dictionary<string, StateDefinition>(),
            Context = new Dictionary<string, object>()
        };
    }
    
    public StateMachineDefinitionBuilder WithVersion(string version)
    {
        _definition.Version = version;
        return this;
    }
    
    public StateMachineDefinitionBuilder WithInitialState(string state)
    {
        _definition.InitialState = state;
        return this;
    }
    
    public StateMachineDefinitionBuilder WithContext(string key, object value)
    {
        _definition.Context![key] = value;
        return this;
    }
    
    public StateBuilder AddState(string name)
    {
        var stateDefinition = new StateDefinition { Name = name };
        _definition.States[name] = stateDefinition;
        return new StateBuilder(this, stateDefinition);
    }
    
    public StateMachineDefinition Build()
    {
        if (string.IsNullOrEmpty(_definition.InitialState))
        {
            throw new InvalidOperationException("必须指定初始状态");
        }
        return _definition;
    }
}

/// <summary>
/// 状态构建器
/// </summary>
public class StateBuilder
{
    private readonly StateMachineDefinitionBuilder _parent;
    private readonly StateDefinition _state;
    
    internal StateBuilder(StateMachineDefinitionBuilder parent, StateDefinition state)
    {
        _parent = parent;
        _state = state;
        _state.On = new Dictionary<string, TransitionDefinition>();
        _state.OnEntry = new List<ActionDefinition>();
        _state.OnExit = new List<ActionDefinition>();
    }
    
    public StateBuilder WithDescription(string description)
    {
        _state.Description = description;
        return this;
    }
    
    public StateBuilder AsInitial()
    {
        _state.Type = "initial";
        return this;
    }
    
    public StateBuilder AsFinal()
    {
        _state.Type = "final";
        return this;
    }
    
    public StateBuilder On(string eventName, string targetState, string? guard = null)
    {
        _state.On![eventName] = new TransitionDefinition
        {
            Target = targetState,
            Guard = guard
        };
        return this;
    }
    
    public StateBuilder OnWithAction(string eventName, string targetState, 
        string actionType, Dictionary<string, object>? actionParams = null)
    {
        var transition = new TransitionDefinition
        {
            Target = targetState,
            Actions = new List<ActionDefinition>
            {
                new ActionDefinition { Type = actionType, Params = actionParams }
            }
        };
        _state.On![eventName] = transition;
        return this;
    }
    
    public StateBuilder OnEntry(string actionType, Dictionary<string, object>? actionParams = null)
    {
        _state.OnEntry!.Add(new ActionDefinition { Type = actionType, Params = actionParams });
        return this;
    }
    
    public StateBuilder OnExit(string actionType, Dictionary<string, object>? actionParams = null)
    {
        _state.OnExit!.Add(new ActionDefinition { Type = actionType, Params = actionParams });
        return this;
    }
    
    public StateBuilder AddState(string name) => _parent.AddState(name);
    
    public StateMachineDefinition Build() => _parent.Build();
}
