namespace TurnBasedBattleStateMachine;

/// <summary>
/// 战斗状态接口
/// </summary>
public interface IBattleState
{
    BattlePhase Phase { get; }
    void Enter(BattleSystem battle);
    void Update(BattleSystem battle);
    void Exit(BattleSystem battle);
}

/// <summary>
/// 战斗开始状态
/// </summary>
public class BattleStartState : IBattleState
{
    public BattlePhase Phase => BattlePhase.BattleStart;
    
    public void Enter(BattleSystem battle)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                      ⚔️  战斗开始  ⚔️                        ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        
        // 显示双方阵容
        Console.WriteLine("\n【我方队伍】");
        foreach (var unit in battle.PlayerTeam)
        {
            unit.PrintStatus();
        }
        
        Console.WriteLine("\n【敌方队伍】");
        foreach (var unit in battle.EnemyTeam)
        {
            unit.PrintStatus();
        }
    }
    
    public void Update(BattleSystem battle)
    {
        battle.ChangePhase(BattlePhase.TurnStart);
    }
    
    public void Exit(BattleSystem battle) { }
}

/// <summary>
/// 回合开始状态
/// </summary>
public class TurnStartState : IBattleState
{
    public BattlePhase Phase => BattlePhase.TurnStart;
    
    public void Enter(BattleSystem battle)
    {
        battle.ActionQueue.StartNewRound();
    }
    
    public void Update(BattleSystem battle)
    {
        var currentUnit = battle.ActionQueue.GetNextUnit();
        
        if (currentUnit == null)
        {
            battle.ChangePhase(BattlePhase.TurnEnd);
            return;
        }
        
        battle.CurrentActor = currentUnit;
        
        // 处理回合开始效果
        Console.WriteLine($"\n  ─── {currentUnit.Name} 的回合 ───");
        currentUnit.ProcessStatusEffectsOnTurnStart();
        
        // 检查是否能行动
        if (!currentUnit.CanAct)
        {
            var preventingEffect = currentUnit.StatusEffects.FirstOrDefault(e => e.PreventsAction);
            Console.WriteLine($"    {currentUnit.Name} 因 {preventingEffect?.GetIcon()} {preventingEffect?.Name} 无法行动！");
            
            // 继续下一个单位
            battle.ChangePhase(BattlePhase.TurnStart);
            return;
        }
        
        if (!currentUnit.IsAlive)
        {
            battle.ChangePhase(BattlePhase.TurnStart);
            return;
        }
        
        battle.ChangePhase(BattlePhase.ActionSelect);
    }
    
    public void Exit(BattleSystem battle) { }
}

/// <summary>
/// 行动选择状态
/// </summary>
public class ActionSelectState : IBattleState
{
    public BattlePhase Phase => BattlePhase.ActionSelect;
    
    public void Enter(BattleSystem battle)
    {
        var actor = battle.CurrentActor;
        if (actor == null) return;
        
        ActionCommand? command;
        
        if (actor.Faction == Faction.Player)
        {
            command = SelectPlayerAction(battle, actor);
        }
        else
        {
            command = SelectEnemyAction(battle, actor);
        }
        
        if (command != null)
        {
            battle.CurrentCommand = command;
        }
    }
    
    private ActionCommand? SelectPlayerAction(BattleSystem battle, BattleUnit actor)
    {
        // 简化: 自动选择技能 (实际游戏中会有UI)
        var availableSkills = actor.Skills
            .Where(s => s.IsReady && actor.CurrentMp >= s.ManaCost && (!actor.IsSilenced || s.ManaCost == 0))
            .ToList();
        
        if (availableSkills.Count == 0)
        {
            Console.WriteLine($"    {actor.Name} 没有可用技能，进行普通攻击");
            var basicAttack = new Skill { Name = "普通攻击", BasePower = 1f, TargetType = TargetType.SingleEnemy };
            var target = battle.EnemyTeam.Where(e => e.IsAlive).OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
            return target != null ? new ActionCommand(actor, basicAttack, new List<BattleUnit> { target }) : null;
        }
        
        // 智能选择
        var skill = ChooseBestSkill(battle, actor, availableSkills);
        var targets = SelectTargets(battle, actor, skill);
        
        return new ActionCommand(actor, skill, targets);
    }
    
    private ActionCommand? SelectEnemyAction(BattleSystem battle, BattleUnit actor)
    {
        var availableSkills = actor.Skills
            .Where(s => s.IsReady && actor.CurrentMp >= s.ManaCost)
            .ToList();
        
        if (availableSkills.Count == 0)
        {
            var basicAttack = new Skill { Name = "普通攻击", BasePower = 1f, TargetType = TargetType.SingleEnemy };
            var target = battle.PlayerTeam.Where(e => e.IsAlive).OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
            return target != null ? new ActionCommand(actor, basicAttack, new List<BattleUnit> { target }) : null;
        }
        
        // 敌人随机选择技能
        var skill = availableSkills[Random.Shared.Next(availableSkills.Count)];
        var targets = SelectTargets(battle, actor, skill);
        
        return new ActionCommand(actor, skill, targets);
    }
    
    private Skill ChooseBestSkill(BattleSystem battle, BattleUnit actor, List<Skill> skills)
    {
        // 如果HP低于30%，优先使用治疗技能
        if (actor.CurrentHp < actor.MaxHp * 0.3f)
        {
            var healSkill = skills.FirstOrDefault(s => s.IsHealing);
            if (healSkill != null) return healSkill;
        }
        
        // 否则使用攻击技能
        var attackSkill = skills.FirstOrDefault(s => !s.IsHealing && s.BasePower > 0);
        return attackSkill ?? skills[0];
    }
    
    private List<BattleUnit> SelectTargets(BattleSystem battle, BattleUnit actor, Skill skill)
    {
        var allies = actor.Faction == Faction.Player ? battle.PlayerTeam : battle.EnemyTeam;
        var enemies = actor.Faction == Faction.Player ? battle.EnemyTeam : battle.PlayerTeam;
        
        return skill.TargetType switch
        {
            TargetType.Self => new List<BattleUnit> { actor },
            TargetType.SingleAlly => new List<BattleUnit> { allies.Where(a => a.IsAlive).OrderBy(a => a.CurrentHp).First() },
            TargetType.AllAllies => allies.Where(a => a.IsAlive).ToList(),
            TargetType.SingleEnemy => new List<BattleUnit> { enemies.Where(e => e.IsAlive).OrderBy(_ => Guid.NewGuid()).First() },
            TargetType.AllEnemies => enemies.Where(e => e.IsAlive).ToList(),
            TargetType.All => allies.Concat(enemies).Where(u => u.IsAlive).ToList(),
            _ => new List<BattleUnit>()
        };
    }
    
    public void Update(BattleSystem battle)
    {
        battle.ChangePhase(BattlePhase.ActionExecute);
    }
    
    public void Exit(BattleSystem battle) { }
}

/// <summary>
/// 行动执行状态
/// </summary>
public class ActionExecuteState : IBattleState
{
    public BattlePhase Phase => BattlePhase.ActionExecute;
    
    public void Enter(BattleSystem battle)
    {
        var command = battle.CurrentCommand;
        if (command == null) return;
        
        var actor = command.Actor;
        var skill = command.Skill;
        var targets = command.Targets;
        
        Console.WriteLine($"\n    ▶ {actor.Name} 使用 【{skill.Name}】");
        
        // 消耗MP
        if (skill.ManaCost > 0)
        {
            actor.UseMp(skill.ManaCost);
            Console.WriteLine($"      消耗 {skill.ManaCost} MP");
        }
        
        // 设置冷却
        skill.Use();
        
        // 执行技能效果
        if (skill.IsHealing)
        {
            ExecuteHealingSkill(actor, skill, targets);
        }
        else if (skill.BasePower > 0)
        {
            ExecuteDamageSkill(battle, actor, skill, targets);
        }
        
        // 应用状态效果
        ApplyStatusEffects(skill, targets);
    }
    
    private void ExecuteHealingSkill(BattleUnit actor, Skill skill, List<BattleUnit> targets)
    {
        foreach (var target in targets)
        {
            int healAmount = DamageCalculator.CalculateHealAmount(actor, skill);
            int actualHeal = target.Heal(healAmount);
            Console.WriteLine($"      💚 {target.Name} 回复 {actualHeal} HP");
        }
    }
    
    private void ExecuteDamageSkill(BattleSystem battle, BattleUnit actor, Skill skill, List<BattleUnit> targets)
    {
        foreach (var target in targets)
        {
            var (baseDamage, isCrit, elementBonus) = DamageCalculator.CalculateSkillDamage(actor, target, skill);
            int actualDamage = target.TakeDamage(baseDamage);
            
            string critText = isCrit ? " 💥暴击!" : "";
            string elementText = elementBonus > 1 ? " ✨克制!" : elementBonus < 1 ? " 🛡️抵抗" : "";
            
            Console.WriteLine($"      ⚔️ {target.Name} 受到 {actualDamage} 伤害{critText}{elementText}");
            
            // 检查反击
            if (target.IsAlive && target.HasCounter && skill.TargetType == TargetType.SingleEnemy)
            {
                int counterDamage = actor.TakeDamage((int)(target.GetEffectiveAttack() * 0.5f));
                Console.WriteLine($"      ↩️ {target.Name} 反击！{actor.Name} 受到 {counterDamage} 伤害");
            }
            
            // 检查死亡
            if (!target.IsAlive)
            {
                Console.WriteLine($"      ☠️ {target.Name} 被击败！");
                battle.OnUnitDefeated(target);
            }
        }
    }
    
    private void ApplyStatusEffects(Skill skill, List<BattleUnit> targets)
    {
        foreach (var (effectType, value, duration, chance) in skill.Effects)
        {
            foreach (var target in targets)
            {
                if (DamageCalculator.CheckEffectHit(chance))
                {
                    var effect = new StatusEffect
                    {
                        Type = effectType,
                        Name = GetEffectName(effectType),
                        Value = value,
                        Duration = duration
                    };
                    target.AddStatusEffect(effect);
                }
            }
        }
    }
    
    private string GetEffectName(StatusEffectType type) => type switch
    {
        StatusEffectType.AttackUp => "攻击提升",
        StatusEffectType.DefenseUp => "防御提升",
        StatusEffectType.SpeedUp => "速度提升",
        StatusEffectType.Regeneration => "再生",
        StatusEffectType.Shield => "护盾",
        StatusEffectType.Counter => "反击姿态",
        StatusEffectType.AttackDown => "攻击下降",
        StatusEffectType.DefenseDown => "防御下降",
        StatusEffectType.SpeedDown => "速度下降",
        StatusEffectType.Poison => "中毒",
        StatusEffectType.Burn => "燃烧",
        StatusEffectType.Freeze => "冰冻",
        StatusEffectType.Paralysis => "麻痹",
        StatusEffectType.Silence => "沉默",
        StatusEffectType.Blind => "致盲",
        StatusEffectType.Sleep => "睡眠",
        StatusEffectType.Stun => "眩晕",
        _ => type.ToString()
    };
    
    public void Update(BattleSystem battle)
    {
        // 检查战斗是否结束
        if (!battle.EnemyTeam.Any(e => e.IsAlive))
        {
            battle.ChangePhase(BattlePhase.Victory);
            return;
        }
        
        if (!battle.PlayerTeam.Any(p => p.IsAlive))
        {
            battle.ChangePhase(BattlePhase.Defeat);
            return;
        }
        
        // 继续下一个行动
        if (!battle.ActionQueue.IsRoundComplete)
        {
            battle.ChangePhase(BattlePhase.TurnStart);
        }
        else
        {
            battle.ChangePhase(BattlePhase.TurnEnd);
        }
    }
    
    public void Exit(BattleSystem battle) { }
}

/// <summary>
/// 回合结束状态
/// </summary>
public class TurnEndState : IBattleState
{
    public BattlePhase Phase => BattlePhase.TurnEnd;
    
    public void Enter(BattleSystem battle)
    {
        Console.WriteLine($"\n  ═══════ 回合 {battle.ActionQueue.CurrentRound} 结束 ═══════");
        
        // 处理所有单位的回合结束
        foreach (var unit in battle.AllUnits.Where(u => u.IsAlive))
        {
            unit.TickStatusEffects();
            unit.TickSkillCooldowns();
            unit.RestoreMp(5); // 每回合恢复少量MP
        }
        
        // 显示当前状态
        Console.WriteLine("\n【战场状态】");
        foreach (var unit in battle.AllUnits.Where(u => u.IsAlive))
        {
            unit.PrintStatus();
        }
    }
    
    public void Update(BattleSystem battle)
    {
        // 检查战斗是否结束
        if (!battle.EnemyTeam.Any(e => e.IsAlive))
        {
            battle.ChangePhase(BattlePhase.Victory);
        }
        else if (!battle.PlayerTeam.Any(p => p.IsAlive))
        {
            battle.ChangePhase(BattlePhase.Defeat);
        }
        else
        {
            // 开始新回合
            battle.ChangePhase(BattlePhase.TurnStart);
        }
    }
    
    public void Exit(BattleSystem battle) { }
}

/// <summary>
/// 胜利状态
/// </summary>
public class VictoryState : IBattleState
{
    public BattlePhase Phase => BattlePhase.Victory;
    
    public void Enter(BattleSystem battle)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                      🎉  胜利！  🎉                          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        
        Console.WriteLine("\n【战斗统计】");
        Console.WriteLine($"  总回合数: {battle.ActionQueue.CurrentRound}");
        Console.WriteLine($"  存活队员: {battle.PlayerTeam.Count(p => p.IsAlive)}/{battle.PlayerTeam.Count}");
        
        battle.EndBattle(true);
    }
    
    public void Update(BattleSystem battle) { }
    public void Exit(BattleSystem battle) { }
}

/// <summary>
/// 失败状态
/// </summary>
public class DefeatState : IBattleState
{
    public BattlePhase Phase => BattlePhase.Defeat;
    
    public void Enter(BattleSystem battle)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                      💀  战败...  💀                         ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        
        battle.EndBattle(false);
    }
    
    public void Update(BattleSystem battle) { }
    public void Exit(BattleSystem battle) { }
}
