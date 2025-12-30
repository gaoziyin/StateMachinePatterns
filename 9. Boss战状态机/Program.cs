using BossFightStateMachine;

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║              Boss战状态机演示 - 龙王之战                       ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");

// 创建Boss
var boss = new BossController("远古龙王", 10000);

// 订阅事件
boss.OnPhaseChanged += (oldPhase, newPhase) =>
{
    Console.WriteLine($"\n  📢 阶段变更: {oldPhase} → {newPhase}");
};

boss.OnSkillExecuted += skill =>
{
    Console.WriteLine($"  ⚡ 技能效果: {skill.Name} (伤害: {skill.Damage})");
};

boss.OnDefeated += () =>
{
    Console.WriteLine("\n  🏆 获得战利品: 龙王之心 x1, 远古龙鳞 x5, 10000金币");
};

// 开始战斗
boss.StartBattle();
boss.PrintStatus();

// 模拟战斗
Console.WriteLine("\n\n========== 战斗模拟开始 ==========\n");

var random = new Random();
var turn = 0;

while (boss.IsBattleActive && boss.CurrentPhase != BossPhase.Defeated)
{
    turn++;
    Console.WriteLine($"\n--- 回合 {turn} ---");
    
    // 模拟玩家攻击
    var playerDamage = random.Next(200, 500);
    var isCrit = random.NextDouble() < 0.2;
    if (isCrit)
    {
        playerDamage *= 2;
        Console.WriteLine("  💫 暴击！");
    }
    Console.WriteLine($"  🗡️ 玩家攻击！");
    boss.TakeDamage(playerDamage);
    
    // 随机眩晕效果
    if (random.NextDouble() < 0.1 && boss.CurrentPhase != BossPhase.Stunned)
    {
        Console.WriteLine("  ✨ 玩家释放眩晕技能！");
        boss.Stun(2f);
    }
    
    // Boss回合
    boss.Update(1f);
    
    // 打印状态（每5回合）
    if (turn % 5 == 0)
    {
        boss.PrintStatus();
    }
    
    // 防止无限循环
    if (turn > 100) break;
    
    // 模拟战斗节奏
    Thread.Sleep(100);
}

Console.WriteLine("\n\n========== 战斗结束 ==========");
boss.PrintStatus();

// 显示战斗统计
Console.WriteLine($"\n📊 战斗统计:");
Console.WriteLine($"   总回合数: {turn}");
Console.WriteLine($"   最终阶段: {boss.CurrentPhase}");
Console.WriteLine($"   剩余血量: {boss.Stats.CurrentHealth:F0}/{boss.Stats.MaxHealth}");

Console.WriteLine("\n\n========== 阶段转换演示 ==========\n");

// 创建新Boss演示快速阶段转换
var demoBoss = new BossController("测试龙", 1000);
demoBoss.StartBattle();
demoBoss.PrintStatus();

Console.WriteLine("\n--- 造成大量伤害触发阶段转换 ---");
demoBoss.TakeDamage(350); // 触发Phase2
demoBoss.Update(0.1f);
demoBoss.Update(2.1f); // 完成转换
demoBoss.PrintStatus();

demoBoss.TakeDamage(350); // 触发Phase3
demoBoss.Update(0.1f);
demoBoss.Update(2.1f);
demoBoss.PrintStatus();

demoBoss.TakeDamage(200); // 触发Enraged
demoBoss.Update(0.1f);
demoBoss.Update(2.1f);
demoBoss.PrintStatus();

demoBoss.TakeDamage(200); // 击败
demoBoss.Update(0.1f);
demoBoss.PrintStatus();

Console.WriteLine("\n演示完成！");
