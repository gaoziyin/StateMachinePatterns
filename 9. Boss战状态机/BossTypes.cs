namespace BossFightStateMachine;

/// <summary>
/// Boss阶段枚举
/// </summary>
public enum BossPhase
{
    Idle,           // 空闲（战斗未开始）
    Phase1,         // 第一阶段（100%-70%血量）
    Phase2,         // 第二阶段（70%-40%血量）
    Phase3,         // 第三阶段（40%-10%血量）
    Enraged,        // 狂暴阶段（10%以下）
    Stunned,        // 眩晕
    Transitioning,  // 阶段转换中
    Defeated,       // 被击败
    Fled            // 玩家逃跑
}

/// <summary>
/// Boss行为状态
/// </summary>
public enum BossBehavior
{
    Idle,           // 空闲
    Walking,        // 行走
    Charging,       // 蓄力
    BasicAttack,    // 普通攻击
    HeavyAttack,    // 重击
    AreaAttack,     // 范围攻击
    Summon,         // 召唤小怪
    Heal,           // 治疗
    Teleport,       // 传送
    UltimateSkill,  // 终极技能
    TakingDamage,   // 受击
    Recovering      // 恢复
}

/// <summary>
/// Boss属性数据
/// </summary>
public class BossStats
{
    public string Name { get; set; } = "Dragon Lord";
    public float MaxHealth { get; set; } = 10000f;
    public float CurrentHealth { get; set; } = 10000f;
    public float MaxRage { get; set; } = 100f;
    public float CurrentRage { get; set; } = 0f;
    public float Defense { get; set; } = 50f;
    public float AttackPower { get; set; } = 100f;
    public float CritChance { get; set; } = 0.1f;
    public float CritMultiplier { get; set; } = 2.0f;
    
    public float HealthPercentage => CurrentHealth / MaxHealth;
    public float RagePercentage => CurrentRage / MaxRage;
    
    public bool IsAlive => CurrentHealth > 0;
}

/// <summary>
/// 阶段技能配置
/// </summary>
public class PhaseSkillSet
{
    public BossPhase Phase { get; set; }
    public List<SkillConfig> Skills { get; set; } = new();
    public float AttackMultiplier { get; set; } = 1.0f;
    public float SpeedMultiplier { get; set; } = 1.0f;
    public string EntryAnimation { get; set; } = "";
    public string EntryDialogue { get; set; } = "";
}

/// <summary>
/// 技能配置
/// </summary>
public class SkillConfig
{
    public string Name { get; set; } = "";
    public BossBehavior Behavior { get; set; }
    public float Damage { get; set; }
    public float Cooldown { get; set; }
    public float CastTime { get; set; }
    public float Range { get; set; }
    public int Weight { get; set; } = 1; // 权重，用于随机选择
    public bool RequiresRage { get; set; } = false;
    public float RageCost { get; set; } = 0f;
}
