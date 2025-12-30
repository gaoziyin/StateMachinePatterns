namespace BossFightStateMachine;

/// <summary>
/// Boss控制器 - 管理阶段和行为状态机
/// </summary>
public class BossController
{
    private readonly Dictionary<BossPhase, IBossPhaseState> _phaseStates = new();
    private IBossPhaseState? _currentPhaseState;
    
    public BossStats Stats { get; }
    public BossPhase CurrentPhase { get; private set; }
    public BossPhase PreviousPhase { get; private set; }
    public BossBehavior CurrentBehavior { get; private set; }
    public bool IsInvulnerable { get; private set; }
    public bool IsBattleActive { get; private set; }
    
    // 事件
    public event Action<BossPhase, BossPhase>? OnPhaseChanged;
    public event Action<BossBehavior>? OnBehaviorChanged;
    public event Action<float, float>? OnDamageDealt; // damage, actualDamage
    public event Action<SkillConfig>? OnSkillExecuted;
    public event Action? OnDefeated;
    
    public BossController(string name = "Dragon Lord", float maxHealth = 10000f)
    {
        Stats = new BossStats
        {
            Name = name,
            MaxHealth = maxHealth,
            CurrentHealth = maxHealth
        };
        
        InitializePhaseStates();
    }
    
    private void InitializePhaseStates()
    {
        _phaseStates[BossPhase.Phase1] = new Phase1State();
        _phaseStates[BossPhase.Phase2] = new Phase2State();
        _phaseStates[BossPhase.Phase3] = new Phase3State();
        _phaseStates[BossPhase.Enraged] = new EnragedState();
        _phaseStates[BossPhase.Stunned] = new StunnedState();
        _phaseStates[BossPhase.Defeated] = new DefeatedState();
    }
    
    /// <summary>
    /// 开始战斗
    /// </summary>
    public void StartBattle()
    {
        if (IsBattleActive) return;
        
        IsBattleActive = true;
        Console.WriteLine($"\n╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║  ⚔️  BOSS战斗开始: {Stats.Name,-30}     ⚔️  ║");
        Console.WriteLine($"╚════════════════════════════════════════════════════════════╝");
        
        TransitionToPhase(BossPhase.Phase1);
    }
    
    /// <summary>
    /// 游戏更新
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!IsBattleActive || CurrentPhase == BossPhase.Defeated) return;
        
        // 更新当前阶段
        _currentPhaseState?.Update(this, deltaTime);
        
        // 检查阶段转换条件
        CheckPhaseTransition();
        
        // 选择并执行行为
        if (CurrentPhase != BossPhase.Transitioning && CurrentPhase != BossPhase.Stunned)
        {
            var nextBehavior = _currentPhaseState?.SelectNextBehavior(this);
            if (nextBehavior.HasValue && nextBehavior.Value != CurrentBehavior)
            {
                SetBehavior(nextBehavior.Value);
            }
        }
    }
    
    /// <summary>
    /// 检查阶段转换
    /// </summary>
    private void CheckPhaseTransition()
    {
        if (CurrentPhase == BossPhase.Transitioning || CurrentPhase == BossPhase.Defeated)
            return;
        
        var healthPercent = Stats.HealthPercentage;
        BossPhase? targetPhase = null;
        
        if (healthPercent <= 0)
        {
            targetPhase = BossPhase.Defeated;
        }
        else if (healthPercent <= 0.1f && CurrentPhase != BossPhase.Enraged)
        {
            targetPhase = BossPhase.Enraged;
        }
        else if (healthPercent <= 0.4f && CurrentPhase == BossPhase.Phase2)
        {
            targetPhase = BossPhase.Phase3;
        }
        else if (healthPercent <= 0.7f && CurrentPhase == BossPhase.Phase1)
        {
            targetPhase = BossPhase.Phase2;
        }
        
        if (targetPhase.HasValue && targetPhase != CurrentPhase)
        {
            if (targetPhase == BossPhase.Defeated)
            {
                TransitionToPhase(BossPhase.Defeated);
            }
            else
            {
                BeginPhaseTransition(targetPhase.Value);
            }
        }
    }
    
    /// <summary>
    /// 开始阶段转换
    /// </summary>
    private void BeginPhaseTransition(BossPhase targetPhase)
    {
        PreviousPhase = CurrentPhase;
        _currentPhaseState?.Exit(this);
        
        _currentPhaseState = new TransitioningState(targetPhase);
        CurrentPhase = BossPhase.Transitioning;
        IsInvulnerable = true;
        _currentPhaseState.Enter(this);
    }
    
    /// <summary>
    /// 完成阶段转换
    /// </summary>
    public void CompleteTransition(BossPhase targetPhase)
    {
        IsInvulnerable = false;
        TransitionToPhase(targetPhase);
    }
    
    /// <summary>
    /// 直接转换到指定阶段
    /// </summary>
    private void TransitionToPhase(BossPhase newPhase)
    {
        var oldPhase = CurrentPhase;
        _currentPhaseState?.Exit(this);
        
        CurrentPhase = newPhase;
        if (_phaseStates.TryGetValue(newPhase, out var state))
        {
            _currentPhaseState = state;
            _currentPhaseState.Enter(this);
        }
        
        OnPhaseChanged?.Invoke(oldPhase, newPhase);
        
        if (newPhase == BossPhase.Defeated)
        {
            IsBattleActive = false;
            OnDefeated?.Invoke();
        }
    }
    
    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(float damage, bool ignoreDefense = false)
    {
        if (!IsBattleActive || CurrentPhase == BossPhase.Defeated) return;
        
        if (IsInvulnerable)
        {
            Console.WriteLine($"  🛡️ {Stats.Name} 处于无敌状态，伤害无效！");
            return;
        }
        
        var actualDamage = ignoreDefense ? damage : Math.Max(1, damage - Stats.Defense);
        Stats.CurrentHealth = Math.Max(0, Stats.CurrentHealth - actualDamage);
        
        // 增加怒气
        Stats.CurrentRage = Math.Min(Stats.MaxRage, Stats.CurrentRage + actualDamage * 0.1f);
        
        Console.WriteLine($"  💥 {Stats.Name} 受到 {actualDamage:F0} 点伤害！" +
                         $"(HP: {Stats.CurrentHealth:F0}/{Stats.MaxHealth:F0} = {Stats.HealthPercentage:P0})");
        
        OnDamageDealt?.Invoke(damage, actualDamage);
        SetBehavior(BossBehavior.TakingDamage);
    }
    
    /// <summary>
    /// 使Boss眩晕
    /// </summary>
    public void Stun(float duration = 3f)
    {
        if (IsInvulnerable || CurrentPhase == BossPhase.Defeated) return;
        
        PreviousPhase = CurrentPhase;
        _phaseStates[BossPhase.Stunned] = new StunnedState(duration);
        TransitionToPhase(BossPhase.Stunned);
    }
    
    /// <summary>
    /// 从眩晕中恢复
    /// </summary>
    public void RecoverFromStun(BossPhase returnPhase)
    {
        TransitionToPhase(returnPhase);
    }
    
    /// <summary>
    /// 设置行为状态
    /// </summary>
    private void SetBehavior(BossBehavior behavior)
    {
        if (CurrentBehavior != behavior)
        {
            CurrentBehavior = behavior;
            OnBehaviorChanged?.Invoke(behavior);
        }
    }
    
    /// <summary>
    /// 执行技能
    /// </summary>
    public void ExecuteSkill(SkillConfig skill)
    {
        Console.WriteLine($"  🎯 {Stats.Name} 使用 [{skill.Name}]！造成 {skill.Damage:F0} 点伤害");
        
        if (skill.Damage < 0)
        {
            // 治疗
            Stats.CurrentHealth = Math.Min(Stats.MaxHealth, Stats.CurrentHealth - skill.Damage);
            Console.WriteLine($"  💚 {Stats.Name} 恢复了 {-skill.Damage:F0} 点生命！");
        }
        
        OnSkillExecuted?.Invoke(skill);
    }
    
    /// <summary>
    /// 打印状态
    /// </summary>
    public void PrintStatus()
    {
        var healthBar = GenerateBar(Stats.HealthPercentage, 30, '█', '░');
        var rageBar = GenerateBar(Stats.RagePercentage, 15, '▓', '░');
        
        Console.WriteLine($"\n┌──────────────────────────────────────────────────────────────┐");
        Console.WriteLine($"│ 🐉 {Stats.Name,-20} 阶段: {CurrentPhase,-15}         │");
        Console.WriteLine($"│ HP: [{healthBar}] {Stats.CurrentHealth,6:F0}/{Stats.MaxHealth,-6:F0}  │");
        Console.WriteLine($"│ 怒气: [{rageBar}] {Stats.RagePercentage,6:P0}                   │");
        Console.WriteLine($"│ 攻击力: {Stats.AttackPower,-8:F0} 防御: {Stats.Defense,-8:F0}          │");
        Console.WriteLine($"│ 行为: {CurrentBehavior,-15} 无敌: {(IsInvulnerable ? "是" : "否"),-5}              │");
        Console.WriteLine($"└──────────────────────────────────────────────────────────────┘");
    }
    
    private static string GenerateBar(float percentage, int length, char filled, char empty)
    {
        var filledCount = (int)(percentage * length);
        return new string(filled, filledCount) + new string(empty, length - filledCount);
    }
}
