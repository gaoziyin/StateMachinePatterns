namespace TurnBasedBattleStateMachine;

/// <summary>
/// 战斗阶段
/// </summary>
public enum BattlePhase
{
    NotStarted,     // 未开始
    BattleStart,    // 战斗开始
    TurnStart,      // 回合开始
    ActionSelect,   // 选择行动
    ActionExecute,  // 执行行动
    TurnEnd,        // 回合结束
    BattleEnd,      // 战斗结束
    Victory,        // 胜利
    Defeat          // 失败
}

/// <summary>
/// 角色阵营
/// </summary>
public enum Faction
{
    Player,
    Enemy
}

/// <summary>
/// 元素类型
/// </summary>
public enum Element
{
    None,
    Fire,       // 火
    Water,      // 水
    Thunder,    // 雷
    Earth,      // 土
    Wind,       // 风
    Light,      // 光
    Dark        // 暗
}

/// <summary>
/// 技能目标类型
/// </summary>
public enum TargetType
{
    Self,           // 自身
    SingleAlly,     // 单个友方
    AllAllies,      // 全体友方
    SingleEnemy,    // 单个敌人
    AllEnemies,     // 全体敌人
    All             // 全体
}

/// <summary>
/// 状态效果类型
/// </summary>
public enum StatusEffectType
{
    // Buff
    AttackUp,       // 攻击提升
    DefenseUp,      // 防御提升
    SpeedUp,        // 速度提升
    Regeneration,   // 持续回复
    Shield,         // 护盾
    Counter,        // 反击
    
    // Debuff
    AttackDown,     // 攻击下降
    DefenseDown,    // 防御下降
    SpeedDown,      // 速度下降
    Poison,         // 中毒
    Burn,           // 燃烧
    Freeze,         // 冰冻
    Paralysis,      // 麻痹
    Silence,        // 沉默
    Blind,          // 致盲
    Sleep,          // 睡眠
    Stun            // 眩晕
}

/// <summary>
/// 状态效果
/// </summary>
public class StatusEffect
{
    public StatusEffectType Type { get; set; }
    public string Name { get; set; } = "";
    public int Duration { get; set; }       // 剩余回合
    public float Value { get; set; }        // 效果值
    public Element Element { get; set; } = Element.None;
    
    public bool IsBuff => Type switch
    {
        StatusEffectType.AttackUp or
        StatusEffectType.DefenseUp or
        StatusEffectType.SpeedUp or
        StatusEffectType.Regeneration or
        StatusEffectType.Shield or
        StatusEffectType.Counter => true,
        _ => false
    };
    
    public bool IsDebuff => !IsBuff;
    
    public bool PreventsAction => Type switch
    {
        StatusEffectType.Freeze or
        StatusEffectType.Paralysis or
        StatusEffectType.Sleep or
        StatusEffectType.Stun => true,
        _ => false
    };
    
    public string GetIcon() => Type switch
    {
        StatusEffectType.AttackUp => "⚔️↑",
        StatusEffectType.DefenseUp => "🛡️↑",
        StatusEffectType.SpeedUp => "💨↑",
        StatusEffectType.Regeneration => "💚",
        StatusEffectType.Shield => "🔰",
        StatusEffectType.Counter => "↩️",
        StatusEffectType.AttackDown => "⚔️↓",
        StatusEffectType.DefenseDown => "🛡️↓",
        StatusEffectType.SpeedDown => "💨↓",
        StatusEffectType.Poison => "☠️",
        StatusEffectType.Burn => "🔥",
        StatusEffectType.Freeze => "❄️",
        StatusEffectType.Paralysis => "⚡",
        StatusEffectType.Silence => "🔇",
        StatusEffectType.Blind => "👁️",
        StatusEffectType.Sleep => "💤",
        StatusEffectType.Stun => "💫",
        _ => "?"
    };
}

/// <summary>
/// 技能定义
/// </summary>
public class Skill
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int ManaCost { get; set; }
    public int Cooldown { get; set; }       // 冷却回合
    public int CurrentCooldown { get; set; } = 0;
    public TargetType TargetType { get; set; }
    public Element Element { get; set; } = Element.None;
    
    // 伤害/治疗
    public float BasePower { get; set; }    // 基础威力
    public bool IsHealing { get; set; }     // 是否是治疗技能
    
    // 状态效果
    public List<(StatusEffectType Type, float Value, int Duration, float Chance)> Effects { get; set; } = new();
    
    public bool IsReady => CurrentCooldown == 0;
    
    public void Use()
    {
        CurrentCooldown = Cooldown;
    }
    
    public void TickCooldown()
    {
        if (CurrentCooldown > 0) CurrentCooldown--;
    }
}

/// <summary>
/// 行动指令
/// </summary>
public record ActionCommand(
    BattleUnit Actor,
    Skill Skill,
    List<BattleUnit> Targets
);
