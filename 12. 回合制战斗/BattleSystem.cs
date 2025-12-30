namespace TurnBasedBattleStateMachine;

/// <summary>
/// 战斗系统 - 核心控制器
/// </summary>
public class BattleSystem
{
    private readonly Dictionary<BattlePhase, IBattleState> _states = new();
    private IBattleState? _currentState;
    
    // 队伍
    public List<BattleUnit> PlayerTeam { get; } = new();
    public List<BattleUnit> EnemyTeam { get; } = new();
    public IEnumerable<BattleUnit> AllUnits => PlayerTeam.Concat(EnemyTeam);
    
    // 行动系统
    public ActionQueue ActionQueue { get; } = new();
    public BattleUnit? CurrentActor { get; set; }
    public ActionCommand? CurrentCommand { get; set; }
    
    // 状态
    public BattlePhase CurrentPhase => _currentState?.Phase ?? BattlePhase.NotStarted;
    public bool IsRunning { get; private set; }
    public bool? IsVictory { get; private set; }
    
    // 事件
    public event Action<BattleUnit>? OnUnitDefeatedEvent;
    public event Action<bool>? OnBattleEnd;
    
    public BattleSystem()
    {
        InitializeStates();
    }
    
    private void InitializeStates()
    {
        _states[BattlePhase.BattleStart] = new BattleStartState();
        _states[BattlePhase.TurnStart] = new TurnStartState();
        _states[BattlePhase.ActionSelect] = new ActionSelectState();
        _states[BattlePhase.ActionExecute] = new ActionExecuteState();
        _states[BattlePhase.TurnEnd] = new TurnEndState();
        _states[BattlePhase.Victory] = new VictoryState();
        _states[BattlePhase.Defeat] = new DefeatState();
    }
    
    /// <summary>
    /// 添加玩家角色
    /// </summary>
    public void AddPlayerUnit(BattleUnit unit)
    {
        unit.Faction = Faction.Player;
        PlayerTeam.Add(unit);
    }
    
    /// <summary>
    /// 添加敌人
    /// </summary>
    public void AddEnemyUnit(BattleUnit unit)
    {
        unit.Faction = Faction.Enemy;
        EnemyTeam.Add(unit);
    }
    
    /// <summary>
    /// 开始战斗
    /// </summary>
    public void StartBattle()
    {
        IsRunning = true;
        IsVictory = null;
        
        ActionQueue.Initialize(AllUnits);
        ChangePhase(BattlePhase.BattleStart);
        
        // 战斗主循环
        while (IsRunning)
        {
            _currentState?.Update(this);
            
            // 添加延迟使输出更易读
            Thread.Sleep(100);
        }
    }
    
    /// <summary>
    /// 改变战斗阶段
    /// </summary>
    public void ChangePhase(BattlePhase newPhase)
    {
        if (_states.TryGetValue(newPhase, out var state))
        {
            _currentState?.Exit(this);
            _currentState = state;
            _currentState.Enter(this);
        }
    }
    
    /// <summary>
    /// 单位被击败
    /// </summary>
    public void OnUnitDefeated(BattleUnit unit)
    {
        OnUnitDefeatedEvent?.Invoke(unit);
    }
    
    /// <summary>
    /// 结束战斗
    /// </summary>
    public void EndBattle(bool isVictory)
    {
        IsRunning = false;
        IsVictory = isVictory;
        OnBattleEnd?.Invoke(isVictory);
    }
}

/// <summary>
/// 技能工厂 - 创建预设技能
/// </summary>
public static class SkillFactory
{
    public static Skill CreateBasicAttack() => new()
    {
        Id = "basic_attack",
        Name = "普通攻击",
        Description = "基础物理攻击",
        ManaCost = 0,
        Cooldown = 0,
        BasePower = 1.0f,
        TargetType = TargetType.SingleEnemy
    };
    
    public static Skill CreateFireball() => new()
    {
        Id = "fireball",
        Name = "火球术",
        Description = "发射火球攻击敌人",
        ManaCost = 15,
        Cooldown = 0,
        BasePower = 1.5f,
        TargetType = TargetType.SingleEnemy,
        Element = Element.Fire,
        Effects = new()
        {
            (StatusEffectType.Burn, 0.1f, 3, 0.3f) // 30%概率造成燃烧
        }
    };
    
    public static Skill CreateBlizzard() => new()
    {
        Id = "blizzard",
        Name = "暴风雪",
        Description = "冰霜风暴攻击全体敌人",
        ManaCost = 30,
        Cooldown = 2,
        BasePower = 0.8f,
        TargetType = TargetType.AllEnemies,
        Element = Element.Water,
        Effects = new()
        {
            (StatusEffectType.Freeze, 0f, 1, 0.2f), // 20%概率冰冻
            (StatusEffectType.SpeedDown, 0.3f, 2, 0.5f) // 50%概率减速
        }
    };
    
    public static Skill CreateThunderStrike() => new()
    {
        Id = "thunder_strike",
        Name = "雷击",
        Description = "召唤雷电攻击敌人",
        ManaCost = 20,
        Cooldown = 1,
        BasePower = 1.8f,
        TargetType = TargetType.SingleEnemy,
        Element = Element.Thunder,
        Effects = new()
        {
            (StatusEffectType.Paralysis, 0f, 1, 0.25f) // 25%概率麻痹
        }
    };
    
    public static Skill CreateHeal() => new()
    {
        Id = "heal",
        Name = "治愈术",
        Description = "恢复单个友方HP",
        ManaCost = 20,
        Cooldown = 0,
        BasePower = 0.3f, // 恢复30%最大HP
        TargetType = TargetType.SingleAlly,
        IsHealing = true
    };
    
    public static Skill CreateGroupHeal() => new()
    {
        Id = "group_heal",
        Name = "群体治愈",
        Description = "恢复全体友方HP",
        ManaCost = 40,
        Cooldown = 2,
        BasePower = 0.2f,
        TargetType = TargetType.AllAllies,
        IsHealing = true
    };
    
    public static Skill CreatePoisonBlade() => new()
    {
        Id = "poison_blade",
        Name = "毒刃",
        Description = "毒素攻击",
        ManaCost = 10,
        Cooldown = 0,
        BasePower = 1.2f,
        TargetType = TargetType.SingleEnemy,
        Effects = new()
        {
            (StatusEffectType.Poison, 0.08f, 3, 0.5f) // 50%概率中毒
        }
    };
    
    public static Skill CreateDefenseUp() => new()
    {
        Id = "defense_up",
        Name = "铁壁",
        Description = "提升自身防御力",
        ManaCost = 15,
        Cooldown = 3,
        BasePower = 0f,
        TargetType = TargetType.Self,
        Effects = new()
        {
            (StatusEffectType.DefenseUp, 0.5f, 3, 1f) // 100%提升50%防御
        }
    };
    
    public static Skill CreateBerserk() => new()
    {
        Id = "berserk",
        Name = "狂暴",
        Description = "大幅提升攻击但降低防御",
        ManaCost = 20,
        Cooldown = 4,
        BasePower = 0f,
        TargetType = TargetType.Self,
        Effects = new()
        {
            (StatusEffectType.AttackUp, 0.8f, 3, 1f),
            (StatusEffectType.DefenseDown, 0.3f, 3, 1f)
        }
    };
    
    public static Skill CreateCounterStance() => new()
    {
        Id = "counter_stance",
        Name = "反击姿态",
        Description = "进入反击状态",
        ManaCost = 10,
        Cooldown = 2,
        BasePower = 0f,
        TargetType = TargetType.Self,
        Effects = new()
        {
            (StatusEffectType.Counter, 1f, 2, 1f)
        }
    };
    
    public static Skill CreateShieldBash() => new()
    {
        Id = "shield_bash",
        Name = "盾击",
        Description = "用盾牌攻击并可能眩晕",
        ManaCost = 15,
        Cooldown = 2,
        BasePower = 0.8f,
        TargetType = TargetType.SingleEnemy,
        Effects = new()
        {
            (StatusEffectType.Stun, 0f, 1, 0.4f) // 40%概率眩晕
        }
    };
    
    public static Skill CreateRegeneration() => new()
    {
        Id = "regeneration",
        Name = "再生",
        Description = "持续恢复HP",
        ManaCost = 25,
        Cooldown = 3,
        BasePower = 0f,
        TargetType = TargetType.SingleAlly,
        IsHealing = true,
        Effects = new()
        {
            (StatusEffectType.Regeneration, 0.1f, 3, 1f)
        }
    };
    
    public static Skill CreateSilence() => new()
    {
        Id = "silence",
        Name = "沉默",
        Description = "封印敌人的技能",
        ManaCost = 20,
        Cooldown = 3,
        BasePower = 0.5f,
        TargetType = TargetType.SingleEnemy,
        Effects = new()
        {
            (StatusEffectType.Silence, 0f, 2, 0.6f)
        }
    };
}

/// <summary>
/// 角色工厂 - 创建预设角色
/// </summary>
public static class CharacterFactory
{
    public static BattleUnit CreateWarrior(string name) => new()
    {
        Name = name,
        Level = 10,
        MaxHp = 150,
        CurrentHp = 150,
        MaxMp = 40,
        CurrentMp = 40,
        Attack = 25,
        Defense = 20,
        Speed = 8,
        Element = Element.Fire,
        Skills =
        {
            SkillFactory.CreateBasicAttack(),
            SkillFactory.CreateBerserk(),
            SkillFactory.CreateShieldBash(),
            SkillFactory.CreateCounterStance()
        }
    };
    
    public static BattleUnit CreateMage(string name) => new()
    {
        Name = name,
        Level = 10,
        MaxHp = 80,
        CurrentHp = 80,
        MaxMp = 100,
        CurrentMp = 100,
        Attack = 30,
        Defense = 8,
        Speed = 12,
        Element = Element.Thunder,
        Skills =
        {
            SkillFactory.CreateBasicAttack(),
            SkillFactory.CreateFireball(),
            SkillFactory.CreateBlizzard(),
            SkillFactory.CreateThunderStrike()
        }
    };
    
    public static BattleUnit CreateHealer(string name) => new()
    {
        Name = name,
        Level = 10,
        MaxHp = 100,
        CurrentHp = 100,
        MaxMp = 80,
        CurrentMp = 80,
        Attack = 15,
        Defense = 12,
        Speed = 10,
        Element = Element.Light,
        Skills =
        {
            SkillFactory.CreateBasicAttack(),
            SkillFactory.CreateHeal(),
            SkillFactory.CreateGroupHeal(),
            SkillFactory.CreateRegeneration()
        }
    };
    
    public static BattleUnit CreateRogue(string name) => new()
    {
        Name = name,
        Level = 10,
        MaxHp = 90,
        CurrentHp = 90,
        MaxMp = 50,
        CurrentMp = 50,
        Attack = 28,
        Defense = 10,
        Speed = 18,
        Element = Element.Dark,
        Skills =
        {
            SkillFactory.CreateBasicAttack(),
            SkillFactory.CreatePoisonBlade(),
            SkillFactory.CreateSilence(),
            SkillFactory.CreateDefenseUp()
        }
    };
    
    public static BattleUnit CreateGoblin(string name) => new()
    {
        Name = name,
        Level = 5,
        MaxHp = 60,
        CurrentHp = 60,
        MaxMp = 20,
        CurrentMp = 20,
        Attack = 15,
        Defense = 5,
        Speed = 12,
        Element = Element.Earth,
        Skills =
        {
            SkillFactory.CreateBasicAttack(),
            SkillFactory.CreatePoisonBlade()
        }
    };
    
    public static BattleUnit CreateOrc(string name) => new()
    {
        Name = name,
        Level = 8,
        MaxHp = 120,
        CurrentHp = 120,
        MaxMp = 30,
        CurrentMp = 30,
        Attack = 22,
        Defense = 15,
        Speed = 6,
        Element = Element.Earth,
        Skills =
        {
            SkillFactory.CreateBasicAttack(),
            SkillFactory.CreateBerserk(),
            SkillFactory.CreateShieldBash()
        }
    };
    
    public static BattleUnit CreateDarkMage(string name) => new()
    {
        Name = name,
        Level = 10,
        MaxHp = 70,
        CurrentHp = 70,
        MaxMp = 90,
        CurrentMp = 90,
        Attack = 28,
        Defense = 6,
        Speed = 14,
        Element = Element.Dark,
        Skills =
        {
            SkillFactory.CreateBasicAttack(),
            SkillFactory.CreateFireball(),
            SkillFactory.CreateSilence(),
            SkillFactory.CreateBlizzard()
        }
    };
}
