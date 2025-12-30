namespace TurnBasedBattleStateMachine;

/// <summary>
/// 行动队列 - 管理回合顺序
/// </summary>
public class ActionQueue
{
    private readonly List<BattleUnit> _allUnits = new();
    private readonly Queue<BattleUnit> _turnQueue = new();
    private int _currentRound = 0;
    
    public int CurrentRound => _currentRound;
    public BattleUnit? CurrentUnit => _turnQueue.Count > 0 ? _turnQueue.Peek() : null;
    public int RemainingActions => _turnQueue.Count;
    
    /// <summary>
    /// 初始化队列
    /// </summary>
    public void Initialize(IEnumerable<BattleUnit> units)
    {
        _allUnits.Clear();
        _allUnits.AddRange(units);
        _currentRound = 0;
    }
    
    /// <summary>
    /// 开始新回合
    /// </summary>
    public void StartNewRound()
    {
        _currentRound++;
        _turnQueue.Clear();
        
        // 按速度排序（速度高的先行动）
        var orderedUnits = _allUnits
            .Where(u => u.IsAlive)
            .OrderByDescending(u => u.GetEffectiveSpeed())
            .ThenBy(u => u.Faction) // 同速度时玩家优先
            .ToList();
        
        foreach (var unit in orderedUnits)
        {
            _turnQueue.Enqueue(unit);
        }
        
        Console.WriteLine($"\n  ══════════ 第 {_currentRound} 回合 ══════════");
        Console.WriteLine($"  行动顺序: {string.Join(" → ", orderedUnits.Select(u => u.Name))}");
    }
    
    /// <summary>
    /// 获取下一个行动单位
    /// </summary>
    public BattleUnit? GetNextUnit()
    {
        while (_turnQueue.Count > 0)
        {
            var unit = _turnQueue.Dequeue();
            if (unit.IsAlive) return unit;
        }
        return null;
    }
    
    /// <summary>
    /// 检查回合是否结束
    /// </summary>
    public bool IsRoundComplete => _turnQueue.Count == 0;
    
    /// <summary>
    /// 移除单位
    /// </summary>
    public void RemoveUnit(BattleUnit unit)
    {
        _allUnits.Remove(unit);
    }
    
    /// <summary>
    /// 显示队列
    /// </summary>
    public void PrintQueue()
    {
        if (_turnQueue.Count == 0)
        {
            Console.WriteLine("  [行动队列为空]");
            return;
        }
        
        Console.WriteLine($"  待行动: {string.Join(" → ", _turnQueue.Select(u => u.Name))}");
    }
}

/// <summary>
/// 伤害计算器
/// </summary>
public static class DamageCalculator
{
    private static readonly Random _random = new();
    
    /// <summary>
    /// 计算技能伤害
    /// </summary>
    public static (int damage, bool isCrit, float elementBonus) CalculateSkillDamage(
        BattleUnit attacker,
        BattleUnit defender,
        Skill skill)
    {
        // 基础伤害 = 攻击力 * 技能威力
        float baseDamage = attacker.GetEffectiveAttack() * skill.BasePower;
        
        // 暴击判定 (10% 基础暴击率)
        bool isCrit = _random.NextDouble() < 0.1;
        if (isCrit)
        {
            baseDamage *= 1.5f;
        }
        
        // 元素克制
        float elementBonus = GetElementBonus(skill.Element, defender.Element);
        baseDamage *= elementBonus;
        
        // 随机浮动 (±10%)
        float randomFactor = 0.9f + (float)_random.NextDouble() * 0.2f;
        baseDamage *= randomFactor;
        
        return ((int)baseDamage, isCrit, elementBonus);
    }
    
    /// <summary>
    /// 计算治疗量
    /// </summary>
    public static int CalculateHealAmount(BattleUnit healer, Skill skill)
    {
        // 治疗量 = 基础威力 * 目标最大HP
        float healAmount = skill.BasePower;
        
        // 随机浮动 (±5%)
        float randomFactor = 0.95f + (float)_random.NextDouble() * 0.1f;
        healAmount *= randomFactor;
        
        return (int)(healAmount * 100); // 返回固定值或百分比
    }
    
    /// <summary>
    /// 元素克制关系
    /// </summary>
    private static float GetElementBonus(Element attackElement, Element defenderElement)
    {
        if (attackElement == Element.None || defenderElement == Element.None)
            return 1f;
        
        // 克制关系: 火>风>土>雷>水>火, 光<>暗
        var advantages = new Dictionary<Element, Element>
        {
            { Element.Fire, Element.Wind },
            { Element.Wind, Element.Earth },
            { Element.Earth, Element.Thunder },
            { Element.Thunder, Element.Water },
            { Element.Water, Element.Fire },
            { Element.Light, Element.Dark },
            { Element.Dark, Element.Light }
        };
        
        if (advantages.TryGetValue(attackElement, out var weak) && weak == defenderElement)
            return 1.5f; // 克制
        
        if (advantages.TryGetValue(defenderElement, out var strong) && strong == attackElement)
            return 0.75f; // 被克制
        
        return 1f;
    }
    
    /// <summary>
    /// 状态效果命中判定
    /// </summary>
    public static bool CheckEffectHit(float chance)
    {
        return _random.NextDouble() < chance;
    }
}
