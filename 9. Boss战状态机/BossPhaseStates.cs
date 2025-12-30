namespace BossFightStateMachine;

/// <summary>
/// Boss阶段状态接口
/// </summary>
public interface IBossPhaseState
{
    BossPhase Phase { get; }
    void Enter(BossController boss);
    void Update(BossController boss, float deltaTime);
    void Exit(BossController boss);
    BossBehavior? SelectNextBehavior(BossController boss);
}

/// <summary>
/// Boss阶段基类
/// </summary>
public abstract class BossPhaseBase : IBossPhaseState
{
    public abstract BossPhase Phase { get; }
    protected PhaseSkillSet SkillSet { get; set; } = new();
    protected float ActionCooldown { get; set; } = 0f;
    protected Random Random { get; } = new();
    
    public virtual void Enter(BossController boss)
    {
        boss.Stats.AttackPower *= SkillSet.AttackMultiplier;
        
        if (!string.IsNullOrEmpty(SkillSet.EntryDialogue))
        {
            Console.WriteLine($"\n  💬 {boss.Stats.Name}: \"{SkillSet.EntryDialogue}\"");
        }
        
        Console.WriteLine($"  ⚔️ 进入{GetPhaseName()}！攻击力 x{SkillSet.AttackMultiplier}");
    }
    
    public virtual void Update(BossController boss, float deltaTime)
    {
        ActionCooldown -= deltaTime;
        
        // 受击时增加怒气
        if (boss.CurrentBehavior == BossBehavior.TakingDamage)
        {
            boss.Stats.CurrentRage = Math.Min(boss.Stats.MaxRage, boss.Stats.CurrentRage + 5);
        }
    }
    
    public virtual void Exit(BossController boss)
    {
        boss.Stats.AttackPower /= SkillSet.AttackMultiplier;
    }
    
    public virtual BossBehavior? SelectNextBehavior(BossController boss)
    {
        if (ActionCooldown > 0) return null;
        
        var availableSkills = SkillSet.Skills
            .Where(s => !s.RequiresRage || boss.Stats.CurrentRage >= s.RageCost)
            .ToList();
        
        if (availableSkills.Count == 0) return BossBehavior.Idle;
        
        // 加权随机选择
        var totalWeight = availableSkills.Sum(s => s.Weight);
        var roll = Random.Next(totalWeight);
        var cumulative = 0;
        
        foreach (var skill in availableSkills)
        {
            cumulative += skill.Weight;
            if (roll < cumulative)
            {
                ActionCooldown = skill.Cooldown;
                if (skill.RequiresRage)
                {
                    boss.Stats.CurrentRage -= skill.RageCost;
                }
                return skill.Behavior;
            }
        }
        
        return BossBehavior.BasicAttack;
    }
    
    protected abstract string GetPhaseName();
}

/// <summary>
/// 第一阶段：常规战斗
/// </summary>
public class Phase1State : BossPhaseBase
{
    public override BossPhase Phase => BossPhase.Phase1;
    
    public Phase1State()
    {
        SkillSet = new PhaseSkillSet
        {
            Phase = BossPhase.Phase1,
            AttackMultiplier = 1.0f,
            SpeedMultiplier = 1.0f,
            EntryDialogue = "你胆敢挑战我？愚蠢的凡人！",
            Skills = new List<SkillConfig>
            {
                new() { Name = "爪击", Behavior = BossBehavior.BasicAttack, Damage = 100, Cooldown = 1.5f, Weight = 5 },
                new() { Name = "尾扫", Behavior = BossBehavior.AreaAttack, Damage = 80, Cooldown = 3f, Weight = 2 },
                new() { Name = "龙息", Behavior = BossBehavior.HeavyAttack, Damage = 150, Cooldown = 5f, Weight = 1 }
            }
        };
    }
    
    protected override string GetPhaseName() => "第一阶段";
}

/// <summary>
/// 第二阶段：强化战斗
/// </summary>
public class Phase2State : BossPhaseBase
{
    public override BossPhase Phase => BossPhase.Phase2;
    
    public Phase2State()
    {
        SkillSet = new PhaseSkillSet
        {
            Phase = BossPhase.Phase2,
            AttackMultiplier = 1.3f,
            SpeedMultiplier = 1.2f,
            EntryDialogue = "你比我想象的要强...是时候认真了！",
            Skills = new List<SkillConfig>
            {
                new() { Name = "连续爪击", Behavior = BossBehavior.BasicAttack, Damage = 120, Cooldown = 1.2f, Weight = 4 },
                new() { Name = "震地", Behavior = BossBehavior.AreaAttack, Damage = 100, Cooldown = 2.5f, Weight = 3 },
                new() { Name = "烈焰龙息", Behavior = BossBehavior.HeavyAttack, Damage = 200, Cooldown = 4f, Weight = 2 },
                new() { Name = "召唤龙崽", Behavior = BossBehavior.Summon, Damage = 0, Cooldown = 10f, Weight = 1 }
            }
        };
    }
    
    protected override string GetPhaseName() => "第二阶段";
}

/// <summary>
/// 第三阶段：狂暴前兆
/// </summary>
public class Phase3State : BossPhaseBase
{
    public override BossPhase Phase => BossPhase.Phase3;
    
    public Phase3State()
    {
        SkillSet = new PhaseSkillSet
        {
            Phase = BossPhase.Phase3,
            AttackMultiplier = 1.6f,
            SpeedMultiplier = 1.4f,
            EntryDialogue = "够了！我要让你见识真正的力量！",
            Skills = new List<SkillConfig>
            {
                new() { Name = "狂暴爪击", Behavior = BossBehavior.BasicAttack, Damage = 150, Cooldown = 1f, Weight = 3 },
                new() { Name = "翼风暴", Behavior = BossBehavior.AreaAttack, Damage = 130, Cooldown = 2f, Weight = 3 },
                new() { Name = "地狱龙息", Behavior = BossBehavior.HeavyAttack, Damage = 280, Cooldown = 3.5f, Weight = 2 },
                new() { Name = "传送突袭", Behavior = BossBehavior.Teleport, Damage = 100, Cooldown = 5f, Weight = 2 },
                new() { Name = "龙之咆哮", Behavior = BossBehavior.UltimateSkill, Damage = 400, Cooldown = 15f, Weight = 1, RequiresRage = true, RageCost = 50 }
            }
        };
    }
    
    protected override string GetPhaseName() => "第三阶段";
}

/// <summary>
/// 狂暴阶段：最后的疯狂
/// </summary>
public class EnragedState : BossPhaseBase
{
    public override BossPhase Phase => BossPhase.Enraged;
    private float _enrageTimer = 0f;
    
    public EnragedState()
    {
        SkillSet = new PhaseSkillSet
        {
            Phase = BossPhase.Enraged,
            AttackMultiplier = 2.5f,
            SpeedMultiplier = 2.0f,
            EntryDialogue = "啊啊啊！！！我要毁灭一切！！！",
            Skills = new List<SkillConfig>
            {
                new() { Name = "疯狂撕咬", Behavior = BossBehavior.BasicAttack, Damage = 200, Cooldown = 0.5f, Weight = 4 },
                new() { Name = "毁灭龙息", Behavior = BossBehavior.HeavyAttack, Damage = 500, Cooldown = 2f, Weight = 3 },
                new() { Name = "末日陨落", Behavior = BossBehavior.UltimateSkill, Damage = 800, Cooldown = 8f, Weight = 2 },
                new() { Name = "绝望治愈", Behavior = BossBehavior.Heal, Damage = -300, Cooldown = 20f, Weight = 1 }
            }
        };
    }
    
    public override void Enter(BossController boss)
    {
        base.Enter(boss);
        _enrageTimer = 0f;
        Console.WriteLine("  🔥 Boss进入狂暴状态！所有攻击大幅增强！");
        Console.WriteLine("  ⏰ 狂暴倒计时开始...30秒后释放毁灭技能！");
    }
    
    public override void Update(BossController boss, float deltaTime)
    {
        base.Update(boss, deltaTime);
        _enrageTimer += deltaTime;
        
        // 狂暴持续30秒后释放毁灭技能
        if (_enrageTimer >= 30f)
        {
            Console.WriteLine("\n  ☠️ 毁灭降临！Boss释放全屏必杀技！");
            boss.ExecuteSkill(new SkillConfig 
            { 
                Name = "灭世龙炎", 
                Behavior = BossBehavior.UltimateSkill, 
                Damage = 9999 
            });
            _enrageTimer = 0f;
        }
    }
    
    protected override string GetPhaseName() => "狂暴阶段";
}

/// <summary>
/// 眩晕状态
/// </summary>
public class StunnedState : IBossPhaseState
{
    public BossPhase Phase => BossPhase.Stunned;
    private float _stunDuration;
    private float _stunTimer;
    private BossPhase _previousPhase;
    
    public StunnedState(float duration = 3f)
    {
        _stunDuration = duration;
    }
    
    public void Enter(BossController boss)
    {
        _stunTimer = 0f;
        _previousPhase = boss.PreviousPhase;
        Console.WriteLine($"  💫 {boss.Stats.Name} 陷入眩晕！持续 {_stunDuration} 秒");
    }
    
    public void Update(BossController boss, float deltaTime)
    {
        _stunTimer += deltaTime;
        if (_stunTimer >= _stunDuration)
        {
            Console.WriteLine($"  💫 {boss.Stats.Name} 从眩晕中恢复！");
            boss.RecoverFromStun(_previousPhase);
        }
    }
    
    public void Exit(BossController boss)
    {
        Console.WriteLine("  ⚡ 眩晕结束，Boss恢复行动！");
    }
    
    public BossBehavior? SelectNextBehavior(BossController boss)
    {
        return BossBehavior.Recovering;
    }
}

/// <summary>
/// 阶段转换状态
/// </summary>
public class TransitioningState : IBossPhaseState
{
    public BossPhase Phase => BossPhase.Transitioning;
    private readonly BossPhase _targetPhase;
    private float _transitionTimer;
    private readonly float _transitionDuration = 2f;
    
    public TransitioningState(BossPhase targetPhase)
    {
        _targetPhase = targetPhase;
    }
    
    public void Enter(BossController boss)
    {
        _transitionTimer = 0f;
        Console.WriteLine($"\n  🔄 阶段转换中...目标: {_targetPhase}");
        Console.WriteLine("  ⚠️ Boss暂时无敌！");
    }
    
    public void Update(BossController boss, float deltaTime)
    {
        _transitionTimer += deltaTime;
        if (_transitionTimer >= _transitionDuration)
        {
            boss.CompleteTransition(_targetPhase);
        }
    }
    
    public void Exit(BossController boss) { }
    
    public BossBehavior? SelectNextBehavior(BossController boss)
    {
        return BossBehavior.Idle;
    }
}

/// <summary>
/// 击败状态
/// </summary>
public class DefeatedState : IBossPhaseState
{
    public BossPhase Phase => BossPhase.Defeated;
    
    public void Enter(BossController boss)
    {
        Console.WriteLine($"\n  ☠️ {boss.Stats.Name} 被击败了！");
        Console.WriteLine($"  💬 {boss.Stats.Name}: \"不...不可能...我是无敌的...\"");
        Console.WriteLine("  🎉 恭喜！战斗胜利！");
    }
    
    public void Update(BossController boss, float deltaTime) { }
    public void Exit(BossController boss) { }
    public BossBehavior? SelectNextBehavior(BossController boss) => null;
}
