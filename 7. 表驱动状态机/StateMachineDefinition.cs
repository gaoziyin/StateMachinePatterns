using System.Text.Json.Serialization;

namespace TableDrivenStatePattern;

/// <summary>
/// 状态机定义（JSON可序列化）
/// </summary>
public class StateMachineDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";
    
    [JsonPropertyName("initialState")]
    public string InitialState { get; set; } = "";
    
    [JsonPropertyName("states")]
    public Dictionary<string, StateDefinition> States { get; set; } = new();
    
    [JsonPropertyName("context")]
    public Dictionary<string, object>? Context { get; set; }
}

/// <summary>
/// 状态定义
/// </summary>
public class StateDefinition
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "normal"; // normal, initial, final
    
    [JsonPropertyName("on")]
    public Dictionary<string, TransitionDefinition>? On { get; set; }
    
    [JsonPropertyName("onEntry")]
    public List<ActionDefinition>? OnEntry { get; set; }
    
    [JsonPropertyName("onExit")]
    public List<ActionDefinition>? OnExit { get; set; }
}

/// <summary>
/// 转换定义
/// </summary>
public class TransitionDefinition
{
    [JsonPropertyName("target")]
    public string Target { get; set; } = "";
    
    [JsonPropertyName("guard")]
    public string? Guard { get; set; }
    
    [JsonPropertyName("actions")]
    public List<ActionDefinition>? Actions { get; set; }
}

/// <summary>
/// 动作定义
/// </summary>
public class ActionDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = ""; // log, assign, invoke, etc.
    
    [JsonPropertyName("params")]
    public Dictionary<string, object>? Params { get; set; }
}
