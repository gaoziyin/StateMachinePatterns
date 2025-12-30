namespace TurnBasedBattleStateMachine;

/// <summary>
/// 战斗单位
/// </summary>
public class BattleUnit
{
    // 基本信息
    public string Id { get; set; } = Guid.NewGuid().ToString()[..8];
    public string Name { get; set; } = "";
    public Faction Faction { get; set; }
    public Element Element { get; set; } = Element.None;
    
    // 属性
    public int Level { get; set; } = 1;
    public int MaxHp { get; set; } = 100;
    public int CurrentHp { get; set; } = 100;
    public int MaxMp { get; set; } = 50;
    public int CurrentMp { get; set; } = 50;
    public int Attack { get; set; } = 20;
    public int Defense { get; set; } = 10;
    public int Speed { get; set; } = 10;
    
    // 技能
    public List<Skill> Skills { get; } = new();
    
    // 状态效果
    private readonly List<StatusEffect> _statusEffects = new();
    public IReadOnlyList<StatusEffect> StatusEffects => _statusEffects;
    
    // 状态
    public bool IsAlive => CurrentHp > 0;
    public bool CanAct => IsAlive && !_statusEffects.Any(e => e.PreventsAction);
    
    // 护盾值
    public int ShieldValue { get; set; } = 0;
    
    /// <summary>
    /// 获取有效属性（考虑状态效果）
    /// </summary>
    public int GetEffectiveAttack()
    {
        float modifier = 1f;
        foreach (var effect in _statusEffects)
        {
            if (effect.Type == StatusEffectType.AttackUp) modifier += effect.Value;
            if (effect.Type == StatusEffectType.AttackDown) modifier -= effect.Value;
        }
        return (int)(Attack * Math.Max(0.1f, modifier));
    }
    
    public int GetEffectiveDefense()
    {
        float modifier = 1f;
        foreach (var effect in _statusEffects)
        {
            if (effect.Type == StatusEffectType.DefenseUp) modifier += effect.Value;
            if (effect.Type == StatusEffectType.DefenseDown) modifier -= effect.Value;
        }
        return (int)(Defense * Math.Max(0.1f, modifier));
    }
    
    public int GetEffectiveSpeed()
    {
        float modifier = 1f;
        foreach (var effect in _statusEffects)
        {
            if (effect.Type == StatusEffectType.SpeedUp) modifier += effect.Value;
            if (effect.Type == StatusEffectType.SpeedDown) modifier -= effect.Value;
        }
        return (int)(Speed * Math.Max(0.1f, modifier));
    }
    
    /// <summary>
    /// 添加状态效果
    /// </summary>
    public void AddStatusEffect(StatusEffect effect)
    {
        // 检查是否已存在相同效果
        var existing = _statusEffects.FirstOrDefault(e => e.Type == effect.Type);
        if (existing != null)
        {
            // 刷新持续时间
            existing.Duration = Math.Max(existing.Duration, effect.Duration);
            existing.Value = Math.Max(existing.Value, effect.Value);
        }
        else
        {
            _statusEffects.Add(effect);
            Console.WriteLine($"    💠 {Name} 获得效果: {effect.GetIcon()} {effect.Name} ({effect.Duration}回合)");
        }
    }
    
    /// <summary>
    /// 移除状态效果
    /// </summary>
    public void RemoveStatusEffect(StatusEffectType type)
    {
        var effect = _statusEffects.FirstOrDefault(e => e.Type == type);
        if (effect != null)
        {
            _statusEffects.Remove(effect);
            Console.WriteLine($"    💠 {Name} 的 {effect.GetIcon()} {effect.Name} 效果消失");
        }
    }
    
    /// <summary>
    /// 回合开始处理状态效果
    /// </summary>
    public void ProcessStatusEffectsOnTurnStart()
    {
        foreach (var effect in _statusEffects.ToList())
        {
            switch (effect.Type)
            {
                case StatusEffectType.Regeneration:
                    int healAmount = (int)(MaxHp * effect.Value);
                    Heal(healAmount);
                    Console.WriteLine($"    💚 {Name} 回复 {healAmount} HP (再生)");
                    break;
                    
                case StatusEffectType.Poison:
                    int poisonDamage = (int)(MaxHp * effect.Value);
                    TakeDamage(poisonDamage, true);
                    Console.WriteLine($"    ☠️ {Name} 受到 {poisonDamage} 毒素伤害");
                    break;
                    
                case StatusEffectType.Burn:
                    int burnDamage = (int)(MaxHp * effect.Value);
                    TakeDamage(burnDamage, true);
                    Console.WriteLine($"    🔥 {Name} 受到 {burnDamage} 燃烧伤害");
                    break;
            }
        }
    }
    
    /// <summary>
    /// 回合结束减少状态持续时间
    /// </summary>
    public void TickStatusEffects()
    {
        foreach (var effect in _statusEffects.ToList())
        {
            effect.Duration--;
            if (effect.Duration <= 0)
            {
                _statusEffects.Remove(effect);
                Console.WriteLine($"    💠 {Name} 的 {effect.GetIcon()} {effect.Name} 效果结束");
            }
        }
    }
    
    /// <summary>
    /// 受到伤害
    /// </summary>
    public int TakeDamage(int damage, bool ignoreDefense = false)
    {
        int actualDamage = damage;
        
        if (!ignoreDefense)
        {
            // 防御减伤
            float reduction = GetEffectiveDefense() / (float)(GetEffectiveDefense() + 100);
            actualDamage = (int)(damage * (1 - reduction));
        }
        
        // 护盾吸收
        if (ShieldValue > 0)
        {
            if (ShieldValue >= actualDamage)
            {
                ShieldValue -= actualDamage;
                Console.WriteLine($"    🔰 护盾吸收了 {actualDamage} 伤害 (剩余护盾: {ShieldValue})");
                return 0;
            }
            else
            {
                actualDamage -= ShieldValue;
                Console.WriteLine($"    🔰 护盾吸收了 {ShieldValue} 伤害后破碎");
                ShieldValue = 0;
            }
        }
        
        CurrentHp = Math.Max(0, CurrentHp - actualDamage);
        return actualDamage;
    }
    
    /// <summary>
    /// 治疗
    /// </summary>
    public int Heal(int amount)
    {
        int actualHeal = Math.Min(amount, MaxHp - CurrentHp);
        CurrentHp += actualHeal;
        return actualHeal;
    }
    
    /// <summary>
    /// 消耗MP
    /// </summary>
    public bool UseMp(int amount)
    {
        if (CurrentMp < amount) return false;
        CurrentMp -= amount;
        return true;
    }
    
    /// <summary>
    /// 回复MP
    /// </summary>
    public void RestoreMp(int amount)
    {
        CurrentMp = Math.Min(MaxMp, CurrentMp + amount);
    }
    
    /// <summary>
    /// 重置技能冷却
    /// </summary>
    public void TickSkillCooldowns()
    {
        foreach (var skill in Skills)
        {
            skill.TickCooldown();
        }
    }
    
    /// <summary>
    /// 检查是否有反击状态
    /// </summary>
    public bool HasCounter => _statusEffects.Any(e => e.Type == StatusEffectType.Counter);
    
    /// <summary>
    /// 检查是否被沉默
    /// </summary>
    public bool IsSilenced => _statusEffects.Any(e => e.Type == StatusEffectType.Silence);
    
    /// <summary>
    /// 显示状态
    /// </summary>
    public void PrintStatus()
    {
        var faction = Faction == Faction.Player ? "👤" : "👹";
        var hpBar = GenerateBar(CurrentHp, MaxHp, 15);
        var mpBar = GenerateBar(CurrentMp, MaxMp, 10);
        
        var statusIcons = string.Join(" ", _statusEffects.Select(e => e.GetIcon()));
        
        Console.WriteLine($"  {faction} {Name,-12} HP [{hpBar}] {CurrentHp,4}/{MaxHp,-4} MP [{mpBar}] {CurrentMp,3}/{MaxMp,-3} {statusIcons}");
    }
    
    private static string GenerateBar(int current, int max, int length)
    {
        int filled = max > 0 ? (int)((float)current / max * length) : 0;
        return new string('█', filled) + new string('░', length - filled);
    }
}
