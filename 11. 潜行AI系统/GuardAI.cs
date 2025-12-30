namespace StealthAIStateMachine;

/// <summary>
/// 守卫AI控制器
/// </summary>
public class GuardAI
{
    private readonly Dictionary<AIState, IAIState> _states = new();
    private IAIState? _currentState;
    
    // 基本属性
    public string Name { get; }
    public Vector2 Position { get; set; }
    public float FacingAngle { get; set; } // 朝向角度
    public float MoveSpeed { get; set; } = 3f;
    public float RunSpeed { get; set; } = 6f;
    
    // 巡逻
    public List<PatrolPoint> PatrolPoints { get; } = new();
    
    // 系统
    public PerceptionSystem Perception { get; }
    public MemorySystem Memory { get; } = new();
    
    // 状态
    public AIState CurrentState => _currentState?.State ?? AIState.Idle;
    public AlertLevel AlertLevel { get; set; } = AlertLevel.Unaware;
    public bool IsAlive { get; set; } = true;
    
    // 事件
    public event Action<AIState, AIState>? OnStateChanged;
    public event Action<AlertLevel>? OnAlertLevelChanged;
    public event Action<PerceptionEvent>? OnTargetPerceived;
    
    public GuardAI(string name, Vector2 startPosition)
    {
        Name = name;
        Position = startPosition;
        Perception = new PerceptionSystem(this);
        
        InitializeStates();
        ChangeState(AIState.Idle);
    }
    
    private void InitializeStates()
    {
        _states[AIState.Idle] = new IdleState();
        _states[AIState.Patrolling] = new PatrollingState();
        _states[AIState.Investigating] = new InvestigatingState();
        _states[AIState.Chasing] = new ChasingState();
        _states[AIState.Attacking] = new AttackingState();
        _states[AIState.Searching] = new SearchingState();
        _states[AIState.Returning] = new ReturningState();
        _states[AIState.Alerting] = new AlertingState();
    }
    
    /// <summary>
    /// 添加巡逻点
    /// </summary>
    public void AddPatrolPoint(Vector2 position, float waitTime = 2f, float lookAngle = 0f)
    {
        PatrolPoints.Add(new PatrolPoint
        {
            Position = position,
            WaitTime = waitTime,
            LookAngle = lookAngle
        });
    }
    
    /// <summary>
    /// 更新AI
    /// </summary>
    public void Update(float deltaTime, Vector2 playerPos, bool playerIsMoving, bool playerInLight, bool playerCrouching)
    {
        if (!IsAlive) return;
        
        // 衰减检测值
        Perception.DecayDetection(deltaTime);
        
        // 清理旧记忆
        Memory.CleanupOldMemories();
        
        // 感知目标
        var perception = Perception.DetectTarget(playerPos, playerIsMoving, playerInLight, playerCrouching);
        
        if (perception != null)
        {
            OnTargetPerceived?.Invoke(perception);
            ProcessPerception(perception, playerPos);
        }
        
        // 更新当前状态
        _currentState?.Update(this, deltaTime);
    }
    
    private void ProcessPerception(PerceptionEvent perception, Vector2 playerPos)
    {
        var totalDetection = Perception.TotalDetection;
        var oldLevel = AlertLevel;
        
        // 根据检测值更新警戒等级
        if (totalDetection >= 1f)
        {
            AlertLevel = AlertLevel.Combat;
            Memory.RememberPlayerPosition(playerPos);
            
            if (CurrentState != AIState.Chasing && CurrentState != AIState.Attacking)
            {
                ChangeState(AIState.Alerting);
            }
        }
        else if (totalDetection >= 0.6f)
        {
            if (AlertLevel < AlertLevel.Alerted)
            {
                AlertLevel = AlertLevel.Alerted;
                Console.WriteLine($"  [{Name}] ⚠️ 警觉！检测到可疑目标！");
            }
            Memory.RememberPlayerPosition(playerPos);
            
            if (CurrentState == AIState.Patrolling || CurrentState == AIState.Idle)
            {
                ChangeState(AIState.Investigating);
            }
        }
        else if (totalDetection >= 0.3f)
        {
            if (AlertLevel < AlertLevel.Suspicious)
            {
                AlertLevel = AlertLevel.Suspicious;
                Console.WriteLine($"  [{Name}] ❓ 嗯？好像有什么...");
                Memory.RememberSuspiciousEvent(perception.Position, "可疑声音/动静");
            }
        }
        
        if (oldLevel != AlertLevel)
        {
            OnAlertLevelChanged?.Invoke(AlertLevel);
        }
    }
    
    /// <summary>
    /// 改变状态
    /// </summary>
    public void ChangeState(AIState newState)
    {
        if (_states.TryGetValue(newState, out var state))
        {
            var oldState = CurrentState;
            _currentState?.Exit(this);
            _currentState = state;
            _currentState.Enter(this);
            OnStateChanged?.Invoke(oldState, newState);
        }
    }
    
    /// <summary>
    /// 通知发现尸体
    /// </summary>
    public void NotifyBodyFound(Vector2 bodyPosition)
    {
        Console.WriteLine($"  [{Name}] 😱 发现尸体！");
        Memory.RememberBodyFound(bodyPosition);
        AlertLevel = AlertLevel.Combat;
        ChangeState(AIState.Alerting);
    }
    
    /// <summary>
    /// 通知听到警报
    /// </summary>
    public void NotifyAlarmTriggered(Vector2 alarmPosition)
    {
        Console.WriteLine($"  [{Name}] 🚨 听到警报！");
        Memory.RememberPlayerPosition(alarmPosition);
        AlertLevel = AlertLevel.Combat;
        ChangeState(AIState.Chasing);
    }
    
    /// <summary>
    /// 打印状态
    /// </summary>
    public void PrintStatus()
    {
        var alertSymbol = AlertLevel switch
        {
            AlertLevel.Unaware => "🟢",
            AlertLevel.Suspicious => "🟡",
            AlertLevel.Alerted => "🟠",
            AlertLevel.Combat => "🔴",
            _ => "⚪"
        };
        
        var detectionBar = GenerateBar(Perception.TotalDetection, 20);
        
        Console.WriteLine($"\n┌────────────────────────────────────────────────────────┐");
        Console.WriteLine($"│ 👮 {Name,-15} {alertSymbol} {AlertLevel,-12}              │");
        Console.WriteLine($"│ 位置: {Position,-15} 朝向: {FacingAngle:F0}°               │");
        Console.WriteLine($"│ 状态: {CurrentState,-20}                      │");
        Console.WriteLine($"│ 检测: [{detectionBar}] {Perception.TotalDetection:P0}        │");
        Console.WriteLine($"│   视觉: {Perception.VisualDetection:P0}  听觉: {Perception.AudioDetection:P0}              │");
        if (Memory.LastKnownPlayerPosition.HasValue)
        {
            Console.WriteLine($"│ 记忆: 最后见于 {Memory.LastKnownPlayerPosition}                  │");
        }
        Console.WriteLine($"└────────────────────────────────────────────────────────┘");
    }
    
    private static string GenerateBar(float value, int length)
    {
        var filled = (int)(value * length);
        return new string('█', filled) + new string('░', length - filled);
    }
}
