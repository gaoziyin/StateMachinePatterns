using StealthAIStateMachine;

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║              潜行AI系统演示 - 视野检测与警戒                   ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");

// 创建守卫
var guard = new GuardAI("守卫Alpha", new Vector2(0, 0));

// 设置巡逻路线
guard.AddPatrolPoint(new Vector2(0, 0), 2f, 0);
guard.AddPatrolPoint(new Vector2(10, 0), 2f, 90);
guard.AddPatrolPoint(new Vector2(10, 10), 2f, 180);
guard.AddPatrolPoint(new Vector2(0, 10), 2f, 270);

// 订阅事件
guard.OnStateChanged += (oldState, newState) =>
{
    Console.WriteLine($"\n  [状态变更] {oldState} → {newState}");
};

guard.OnAlertLevelChanged += level =>
{
    var symbol = level switch
    {
        AlertLevel.Unaware => "🟢",
        AlertLevel.Suspicious => "🟡",
        AlertLevel.Alerted => "🟠",
        AlertLevel.Combat => "🔴",
        _ => "⚪"
    };
    Console.WriteLine($"  [警戒等级] {symbol} {level}");
};

Console.WriteLine("\n\n========== 场景1: 巡逻中的守卫 ==========\n");

// 玩家在远处
var playerPos = new Vector2(50, 50);

guard.PrintStatus();

// 模拟巡逻
Console.WriteLine("\n--- 守卫开始巡逻 ---");
for (int i = 0; i < 30; i++)
{
    guard.Update(0.5f, playerPos, false, false, false);
}
guard.PrintStatus();


Console.WriteLine("\n\n========== 场景2: 玩家靠近（蹲行） ==========\n");

// 玩家悄悄靠近
Console.WriteLine("玩家蹲着慢慢靠近...\n");

for (int i = 0; i < 20; i++)
{
    playerPos = playerPos - new Vector2(2, 2);
    guard.Update(0.5f, playerPos, true, false, true); // 移动中，不在光下，蹲着
    
    if (i % 5 == 0)
    {
        Console.WriteLine($"  玩家位置: {playerPos}, 检测值: {guard.Perception.TotalDetection:P0}");
    }
}

guard.PrintStatus();


Console.WriteLine("\n\n========== 场景3: 玩家暴露在光下 ==========\n");

// 玩家站起来，被灯光照到
Console.WriteLine("玩家站起来被灯光照到！\n");

for (int i = 0; i < 15; i++)
{
    guard.Update(0.3f, playerPos, true, true, false); // 移动中，在光下，站立
    
    if (guard.AlertLevel == AlertLevel.Combat)
    {
        Console.WriteLine("  ⚠️ 守卫进入战斗状态！");
        break;
    }
}

guard.PrintStatus();


Console.WriteLine("\n\n========== 场景4: 追击与搜索 ==========\n");

// 模拟追击
Console.WriteLine("守卫开始追击...\n");

for (int i = 0; i < 10; i++)
{
    guard.Update(0.5f, playerPos, true, false, false);
}

// 玩家躲起来
Console.WriteLine("\n玩家躲进暗处，守卫丢失目标...\n");
playerPos = new Vector2(-100, -100); // 玩家消失

for (int i = 0; i < 20; i++)
{
    guard.Update(0.5f, playerPos, false, false, true);
    
    if (guard.CurrentState == AIState.Searching)
    {
        Console.WriteLine($"  守卫位置: {guard.Position}，正在搜索...");
    }
}

guard.PrintStatus();


Console.WriteLine("\n\n========== 场景5: 发现尸体 ==========\n");

// 重置守卫
guard = new GuardAI("守卫Beta", new Vector2(0, 0));
guard.AddPatrolPoint(new Vector2(0, 0), 2f, 0);
guard.AddPatrolPoint(new Vector2(5, 0), 2f, 90);

Console.WriteLine("守卫正在巡逻...\n");

for (int i = 0; i < 10; i++)
{
    guard.Update(0.5f, new Vector2(100, 100), false, false, false);
}

// 发现尸体
Console.WriteLine("\n守卫发现了一具尸体！\n");
guard.NotifyBodyFound(new Vector2(3, 2));

for (int i = 0; i < 10; i++)
{
    guard.Update(0.5f, new Vector2(100, 100), false, false, false);
}

guard.PrintStatus();


Console.WriteLine("\n\n========== 感知系统参数 ==========");
Console.WriteLine(@"
视觉检测:
  - 视野距离: 15米
  - 视野角度: 90° (正面)
  - 余光角度: 150°
  - 余光检测力: 30%

修正因素:
  - 移动中: +50% 检测
  - 光照下: +30% 检测
  - 蹲伏: -50% 检测
  - 近距离(<3m): +100% 检测

听觉检测:
  - 听觉范围: 10米
  - 蹲走: -70% 听觉检测

警戒等级:
  🟢 Unaware (0-30%): 无察觉
  🟡 Suspicious (30-60%): 有怀疑
  🟠 Alerted (60-100%): 已警觉
  🔴 Combat (100%): 进入战斗
");

Console.WriteLine("\n演示完成！");
