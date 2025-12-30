namespace TableDrivenStatePattern;

/// <summary>
/// 表驱动状态机解释器
/// </summary>
public class TableDrivenStateMachine
{
    private readonly StateMachineDefinition _definition;
    private string _currentState;
    private readonly Dictionary<string, object> _context;
    private readonly Dictionary<string, Func<Dictionary<string, object>, bool>> _guardEvaluators = new();
    private readonly Dictionary<string, Action<Dictionary<string, object>, ActionDefinition>> _actionExecutors = new();
    
    public string CurrentState => _currentState;
    public string MachineName => _definition.Name;
    public IReadOnlyDictionary<string, object> Context => _context;
    
    /// <summary>
    /// 状态变更事件
    /// </summary>
    public event Action<string, string, string>? OnStateChanged;
    
    /// <summary>
    /// 动作执行事件
    /// </summary>
    public event Action<string, ActionDefinition>? OnActionExecuted;
    
    public TableDrivenStateMachine(StateMachineDefinition definition)
    {
        _definition = definition;
        _currentState = definition.InitialState;
        _context = new Dictionary<string, object>(definition.Context ?? new());
        
        RegisterBuiltInActions();
        RegisterBuiltInGuards();
    }
    
    private void RegisterBuiltInActions()
    {
        // 日志动作
        RegisterAction("log", (ctx, action) =>
        {
            var message = action.Params?["message"]?.ToString() ?? "";
            message = InterpolateString(message, ctx);
            Console.WriteLine($"    [LOG] {message}");
        });
        
        // 赋值动作
        RegisterAction("assign", (ctx, action) =>
        {
            if (action.Params != null)
            {
                foreach (var kv in action.Params)
                {
                    ctx[kv.Key] = kv.Value;
                    Console.WriteLine($"    [ASSIGN] {kv.Key} = {kv.Value}");
                }
            }
        });
        
        // 计数器增加
        RegisterAction("increment", (ctx, action) =>
        {
            var key = action.Params?["key"]?.ToString() ?? "";
            var amount = Convert.ToInt32(action.Params?["amount"] ?? 1);
            if (ctx.TryGetValue(key, out var value))
            {
                ctx[key] = Convert.ToInt32(value) + amount;
            }
            else
            {
                ctx[key] = amount;
            }
            Console.WriteLine($"    [INCREMENT] {key} += {amount} (现在: {ctx[key]})");
        });
        
        // 调用外部服务（模拟）
        RegisterAction("invoke", (ctx, action) =>
        {
            var service = action.Params?["service"]?.ToString() ?? "";
            Console.WriteLine($"    [INVOKE] 调用服务: {service}");
        });
        
        // 延迟动作
        RegisterAction("delay", (ctx, action) =>
        {
            var ms = Convert.ToInt32(action.Params?["ms"] ?? 100);
            Console.WriteLine($"    [DELAY] 等待 {ms}ms");
            Thread.Sleep(ms);
        });
    }
    
    private void RegisterBuiltInGuards()
    {
        // 比较守卫
        RegisterGuard("equals", ctx =>
        {
            // 从上下文获取比较值
            return true;
        });
        
        // 大于守卫
        RegisterGuard("greaterThan", ctx =>
        {
            return true;
        });
    }
    
    /// <summary>
    /// 注册自定义动作处理器
    /// </summary>
    public void RegisterAction(string type, Action<Dictionary<string, object>, ActionDefinition> executor)
    {
        _actionExecutors[type] = executor;
    }
    
    /// <summary>
    /// 注册自定义守卫条件
    /// </summary>
    public void RegisterGuard(string name, Func<Dictionary<string, object>, bool> evaluator)
    {
        _guardEvaluators[name] = evaluator;
    }
    
    /// <summary>
    /// 发送事件
    /// </summary>
    public bool Send(string eventName, Dictionary<string, object>? eventData = null)
    {
        Console.WriteLine($"\n[EVENT] {eventName} (当前状态: {_currentState})");
        
        // 合并事件数据到上下文
        if (eventData != null)
        {
            foreach (var kv in eventData)
            {
                _context[kv.Key] = kv.Value;
            }
        }
        
        if (!_definition.States.TryGetValue(_currentState, out var stateDefinition))
        {
            Console.WriteLine($"  [ERROR] 未知状态: {_currentState}");
            return false;
        }
        
        if (stateDefinition.On == null || !stateDefinition.On.TryGetValue(eventName, out var transition))
        {
            Console.WriteLine($"  [IGNORED] 状态 {_currentState} 不处理事件 {eventName}");
            return false;
        }
        
        // 检查守卫条件
        if (!string.IsNullOrEmpty(transition.Guard))
        {
            if (!EvaluateGuard(transition.Guard))
            {
                Console.WriteLine($"  [GUARD] 守卫条件 '{transition.Guard}' 不满足");
                return false;
            }
            Console.WriteLine($"  [GUARD] 守卫条件 '{transition.Guard}' 通过");
        }
        
        // 执行退出动作
        ExecuteActions(stateDefinition.OnExit, "OnExit");
        
        // 执行转换动作
        ExecuteActions(transition.Actions, "Transition");
        
        var previousState = _currentState;
        _currentState = transition.Target;
        
        Console.WriteLine($"  [TRANSITION] {previousState} → {_currentState}");
        
        // 执行进入动作
        if (_definition.States.TryGetValue(_currentState, out var newStateDefinition))
        {
            ExecuteActions(newStateDefinition.OnEntry, "OnEntry");
        }
        
        OnStateChanged?.Invoke(previousState, _currentState, eventName);
        
        return true;
    }
    
    private void ExecuteActions(List<ActionDefinition>? actions, string phase)
    {
        if (actions == null) return;
        
        foreach (var action in actions)
        {
            if (_actionExecutors.TryGetValue(action.Type, out var executor))
            {
                executor(_context, action);
                OnActionExecuted?.Invoke(phase, action);
            }
            else
            {
                Console.WriteLine($"    [WARNING] 未知动作类型: {action.Type}");
            }
        }
    }
    
    private bool EvaluateGuard(string guard)
    {
        // 简单的表达式解析
        // 支持: key == value, key > value, key < value, !key
        
        if (guard.Contains("=="))
        {
            var parts = guard.Split("==").Select(p => p.Trim()).ToArray();
            var left = GetContextValue(parts[0]);
            var right = ParseValue(parts[1]);
            return left?.ToString() == right?.ToString();
        }
        else if (guard.Contains(">"))
        {
            var parts = guard.Split(">").Select(p => p.Trim()).ToArray();
            var left = Convert.ToDouble(GetContextValue(parts[0]) ?? 0);
            var right = Convert.ToDouble(ParseValue(parts[1]) ?? 0);
            return left > right;
        }
        else if (guard.Contains("<"))
        {
            var parts = guard.Split("<").Select(p => p.Trim()).ToArray();
            var left = Convert.ToDouble(GetContextValue(parts[0]) ?? 0);
            var right = Convert.ToDouble(ParseValue(parts[1]) ?? 0);
            return left < right;
        }
        else if (guard.StartsWith("!"))
        {
            var key = guard.Substring(1);
            return !Convert.ToBoolean(GetContextValue(key) ?? false);
        }
        else if (_guardEvaluators.TryGetValue(guard, out var evaluator))
        {
            return evaluator(_context);
        }
        else
        {
            return Convert.ToBoolean(GetContextValue(guard) ?? false);
        }
    }
    
    private object? GetContextValue(string key)
    {
        return _context.TryGetValue(key, out var value) ? value : null;
    }
    
    private object? ParseValue(string value)
    {
        if (value.StartsWith("\"") && value.EndsWith("\""))
            return value.Trim('"');
        if (int.TryParse(value, out var intVal))
            return intVal;
        if (double.TryParse(value, out var doubleVal))
            return doubleVal;
        if (bool.TryParse(value, out var boolVal))
            return boolVal;
        return value;
    }
    
    private string InterpolateString(string template, Dictionary<string, object> context)
    {
        foreach (var kv in context)
        {
            template = template.Replace($"${{{kv.Key}}}", kv.Value?.ToString() ?? "");
        }
        return template;
    }
    
    /// <summary>
    /// 获取当前状态可处理的事件列表
    /// </summary>
    public IEnumerable<string> GetAvailableEvents()
    {
        if (_definition.States.TryGetValue(_currentState, out var state) && state.On != null)
        {
            return state.On.Keys;
        }
        return Enumerable.Empty<string>();
    }
    
    /// <summary>
    /// 检查是否为最终状态
    /// </summary>
    public bool IsInFinalState()
    {
        if (_definition.States.TryGetValue(_currentState, out var state))
        {
            return state.Type == "final";
        }
        return false;
    }
    
    /// <summary>
    /// 打印状态机状态
    /// </summary>
    public void PrintStatus()
    {
        Console.WriteLine($"\n╔═══════════════════════════════════════════════════════╗");
        Console.WriteLine($"║ 状态机: {MachineName,-20} 版本: {_definition.Version,-10} ║");
        Console.WriteLine($"║ 当前状态: {_currentState,-20}                    ║");
        Console.WriteLine($"║ 可用事件: {string.Join(", ", GetAvailableEvents()),-30} ║");
        Console.WriteLine($"╠═══════════════════════════════════════════════════════╣");
        Console.WriteLine($"║ 上下文数据:                                           ║");
        foreach (var kv in _context)
        {
            Console.WriteLine($"║   {kv.Key,-15}: {kv.Value,-25}       ║");
        }
        Console.WriteLine($"╚═══════════════════════════════════════════════════════╝");
    }
}
