using ParallelStatePattern.Regions;

namespace ParallelStatePattern;

/// <summary>
/// 游戏角色 - 包含多个并行状态区域
/// </summary>
public class GameCharacter
{
    public string Name { get; }
    public int MaxHealth { get; } = 100;
    public int CurrentHealth { get; private set; } = 100;
    public int AttackPower { get; } = 15;
    public float SpeedModifier { get; set; } = 1.0f;
    public bool IsAlive => CurrentHealth > 0;
    
    // 并行状态区域
    public MovementRegion Movement { get; }
    public ActionRegion Action { get; }
    public BuffRegion Buffs { get; }
    
    private readonly List<IStateRegion> _regions;
    
    /// <summary>
    /// 状态变更事件
    /// </summary>
    public event Action<string>? OnStateChanged;
    
    public GameCharacter(string name)
    {
        Name = name;
        Movement = new MovementRegion();
        Action = new ActionRegion();
        Buffs = new BuffRegion();
        
        _regions = [Movement, Action, Buffs];
    }
    
    /// <summary>
    /// 更新所有状态区域
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!IsAlive) return;
        
        foreach (var region in _regions)
        {
            region.Update(this, deltaTime);
        }
    }
    
    /// <summary>
    /// 发送事件到所有状态区域
    /// </summary>
    public void SendEvent(GameEvent gameEvent)
    {
        Console.WriteLine($"\n[{Name}] 事件: {gameEvent.Type}");
        
        foreach (var region in _regions)
        {
            region.HandleEvent(this, gameEvent);
        }
        
        OnStateChanged?.Invoke(GetStateSnapshot());
    }
    
    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(int damage, bool silent = false)
    {
        if (Buffs.HasBuff(BuffState.Invincible))
        {
            if (!silent) Console.WriteLine($"    ✨ 无敌状态，免疫伤害!");
            return;
        }
        
        CurrentHealth = Math.Max(0, CurrentHealth - damage);
        if (!silent) Console.WriteLine($"    💔 受到 {damage} 点伤害! HP: {CurrentHealth}/{MaxHealth}");
        
        if (CurrentHealth <= 0)
        {
            SendEvent(new GameEvent(GameEventType.Die));
        }
    }
    
    /// <summary>
    /// 恢复生命
    /// </summary>
    public void Heal(int amount)
    {
        if (!IsAlive) return;
        
        var oldHealth = CurrentHealth;
        CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        
        if (CurrentHealth > oldHealth)
        {
            // 只在实际回血时输出（避免刷屏）
        }
    }
    
    /// <summary>
    /// 复活
    /// </summary>
    public void Revive()
    {
        CurrentHealth = MaxHealth / 2;
        SpeedModifier = 1.0f;
        SendEvent(new GameEvent(GameEventType.Revive));
    }
    
    /// <summary>
    /// 获取状态快照
    /// </summary>
    public string GetStateSnapshot()
    {
        return $"移动:{Movement.CurrentStateName} | 动作:{Action.CurrentStateName} | Buff:{Buffs.CurrentStateName}";
    }
    
    /// <summary>
    /// 打印角色状态
    /// </summary>
    public void PrintStatus()
    {
        Console.WriteLine($"\n╔═══════════════════════════════════════════════════════╗");
        Console.WriteLine($"║  角色: {Name,-15} HP: {CurrentHealth,3}/{MaxHealth,-3}  速度: {SpeedModifier:F1}x  ║");
        Console.WriteLine($"╠═══════════════════════════════════════════════════════╣");
        Console.WriteLine($"║  移动状态: {Movement.CurrentStateName,-12} 当前速度: {Movement.CurrentSpeed:F1}       ║");
        Console.WriteLine($"║  动作状态: {Action.CurrentStateName,-12} 可移动: {(Action.CanMove ? "是" : "否"),-6}    ║");
        Console.WriteLine($"║  Buff状态: {Buffs.CurrentStateName,-20}              ║");
        Console.WriteLine($"╚═══════════════════════════════════════════════════════╝");
    }
}
