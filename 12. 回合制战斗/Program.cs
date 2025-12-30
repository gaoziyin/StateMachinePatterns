using TurnBasedBattleStateMachine;

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                   🎲 回合制战斗系统演示 🎲                     ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");

// 创建战斗系统
var battle = new BattleSystem();

// 添加玩家队伍
Console.WriteLine("\n【组建队伍】");
Console.WriteLine("  👤 战士阿瑟 - 高血量、高攻击，可以狂暴");
Console.WriteLine("  👤 法师梅林 - 强力魔法，可以群攻");
Console.WriteLine("  👤 牧师艾莉 - 治疗专精");
Console.WriteLine("  👤 盗贼夏恩 - 高速度，可以沉默和下毒");

battle.AddPlayerUnit(CharacterFactory.CreateWarrior("阿瑟"));
battle.AddPlayerUnit(CharacterFactory.CreateMage("梅林"));
battle.AddPlayerUnit(CharacterFactory.CreateHealer("艾莉"));
battle.AddPlayerUnit(CharacterFactory.CreateRogue("夏恩"));

// 添加敌人
Console.WriteLine("\n【敌人出现】");
Console.WriteLine("  👹 哥布林 x2 - 速度快，会下毒");
Console.WriteLine("  👹 兽人战士 - 高血量，会狂暴");
Console.WriteLine("  👹 黑暗法师 - 强力魔法，会沉默");

battle.AddEnemyUnit(CharacterFactory.CreateGoblin("哥布林A"));
battle.AddEnemyUnit(CharacterFactory.CreateGoblin("哥布林B"));
battle.AddEnemyUnit(CharacterFactory.CreateOrc("兽人战士"));
battle.AddEnemyUnit(CharacterFactory.CreateDarkMage("黑暗法师"));

// 订阅事件
battle.OnUnitDefeatedEvent += unit =>
{
    var icon = unit.Faction == Faction.Player ? "😢" : "🎯";
    Console.WriteLine($"      {icon} {unit.Name} 已阵亡！");
};

battle.OnBattleEnd += victory =>
{
    if (victory)
    {
        Console.WriteLine("\n  🏆 获得经验值 500！");
        Console.WriteLine("  🏆 获得金币 200！");
        Console.WriteLine("  🏆 获得物品: 治愈药水 x2");
    }
    else
    {
        Console.WriteLine("\n  💔 队伍全灭...");
    }
};

// 开始战斗
Console.WriteLine("\n按任意键开始战斗...");
Console.ReadKey(true);

battle.StartBattle();

// 战斗结束后显示系统说明
Console.WriteLine("\n\n════════════════════════════════════════════════════════════════");
Console.WriteLine("                         系统说明");
Console.WriteLine("════════════════════════════════════════════════════════════════");
Console.WriteLine(@"
【战斗阶段】
  BattleStart → TurnStart → ActionSelect → ActionExecute → TurnEnd
       ↑                                                      │
       └──────────────────────────────────────────────────────┘

【行动顺序】
  按速度从高到低决定行动顺序
  速度相同时玩家优先

【伤害计算】
  基础伤害 = 攻击力 × 技能威力
  实际伤害 = 基础伤害 × (1 - 防御/(防御+100))
  暴击 (10%): 伤害 × 1.5
  元素克制: 伤害 × 1.5 / 0.75

【元素相克】
  火 → 风 → 土 → 雷 → 水 → 火
  光 ↔ 暗

【状态效果】
  Buff: 攻击↑ 防御↑ 速度↑ 再生 护盾 反击
  Debuff: 攻击↓ 防御↓ 速度↓ 中毒 燃烧 冰冻 麻痹 沉默 致盲 睡眠 眩晕
");

Console.WriteLine("演示完成！");
