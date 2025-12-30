using ComboSystemStateMachine;

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║              格斗连招系统演示 - 输入缓冲与连招树               ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");

var combo = new ComboStateMachine();

// 订阅事件
combo.OnComboEnded += (hits, damage) =>
{
    if (hits >= 5)
        Console.WriteLine("  🏆 EXCELLENT!");
    else if (hits >= 3)
        Console.WriteLine("  ✨ GREAT!");
};

combo.OnSpecialMoveExecuted += move =>
{
    Console.WriteLine($"  🔥 必杀技: {move}!");
};

Console.WriteLine("\n\n========== 演示1: 基础连招 ==========");
Console.WriteLine("输入: LP → LP → LK → HP\n");

combo.PrintStatus();

// 轻拳起手
combo.ProcessInput(InputButton.LightPunch);
SimulateFrames(combo, 8);

// 轻拳连打
combo.ProcessInput(InputButton.LightPunch);
SimulateFrames(combo, 8);

// 轻脚
combo.ProcessInput(InputButton.LightKick);
SimulateFrames(combo, 10);

// 重拳
combo.ProcessInput(InputButton.HeavyPunch);
SimulateFrames(combo, 30);

combo.PrintStatus();


Console.WriteLine("\n\n========== 演示2: 必杀技取消 ==========");
Console.WriteLine("输入: LP → HP → ↓↘→ + LP (波动拳取消)\n");

// 轻拳起手
combo.ProcessInput(InputButton.LightPunch);
SimulateFrames(combo, 8);

// 重拳
combo.ProcessInput(InputButton.HeavyPunch);
SimulateFrames(combo, 5);

// 波动拳指令 ↓↘→ + LP
combo.ProcessInput(InputButton.Down);
SimulateFrames(combo, 2);
combo.ProcessInput(InputButton.Down | InputButton.Forward);
SimulateFrames(combo, 2);
combo.ProcessInput(InputButton.Forward);
SimulateFrames(combo, 2);
combo.ProcessInput(InputButton.LightPunch);
SimulateFrames(combo, 52);

combo.PrintStatus();


Console.WriteLine("\n\n========== 演示3: 升龙拳 ==========");
Console.WriteLine("输入: →↓↘ + LP\n");

// 升龙拳指令
combo.ProcessInput(InputButton.Forward);
SimulateFrames(combo, 2);
combo.ProcessInput(InputButton.Down);
SimulateFrames(combo, 2);
combo.ProcessInput(InputButton.Down | InputButton.Forward);
SimulateFrames(combo, 2);
combo.ProcessInput(InputButton.LightPunch);
SimulateFrames(combo, 50);

combo.PrintStatus();


Console.WriteLine("\n\n========== 演示4: 浮空连招 + 超必杀 ==========");
Console.WriteLine("输入: LP → LK → HK(浮空) → ↓↘→↓↘→ + LP (超必杀)\n");

// 先积攒气量
for (int i = 0; i < 5; i++)
{
    combo.ProcessInput(InputButton.LightPunch);
    SimulateFrames(combo, 20);
}

combo.PrintStatus();

Console.WriteLine("\n--- 开始连招 ---\n");

// 轻拳起手
combo.ProcessInput(InputButton.LightPunch);
SimulateFrames(combo, 6);

// 轻脚
combo.ProcessInput(InputButton.LightKick);
SimulateFrames(combo, 8);

// 重脚（浮空技）
combo.ProcessInput(InputButton.HeavyKick);
SimulateFrames(combo, 15);

// 超必杀取消 ↓↘→↓↘→ + LP
combo.ProcessInput(InputButton.Down);
SimulateFrames(combo, 1);
combo.ProcessInput(InputButton.Down | InputButton.Forward);
SimulateFrames(combo, 1);
combo.ProcessInput(InputButton.Forward);
SimulateFrames(combo, 1);
combo.ProcessInput(InputButton.Down);
SimulateFrames(combo, 1);
combo.ProcessInput(InputButton.Down | InputButton.Forward);
SimulateFrames(combo, 1);
combo.ProcessInput(InputButton.Forward);
SimulateFrames(combo, 1);
combo.ProcessInput(InputButton.LightPunch);
SimulateFrames(combo, 80);

combo.PrintStatus();


Console.WriteLine("\n\n========== 帧数据说明 ==========");
Console.WriteLine(@"
招式帧数格式: 前摇/判定/后摇
- 前摇(Startup): 按下按键到攻击判定生效的帧数
- 判定(Active): 攻击判定持续的帧数
- 后摇(Recovery): 攻击判定结束到可以行动的帧数

取消窗口:
- Chain: 轻攻击可以在判定帧取消为更重的攻击
- Cancel: 普通技可以在判定帧取消为必杀技
- Super Cancel: 必杀技可以在命中时取消为超必杀

伤害缩放:
- 每增加一个连段，伤害减少10%
- 最低缩放到10%
");

Console.WriteLine("\n演示完成！");


void SimulateFrames(ComboStateMachine machine, int frames)
{
    for (int i = 0; i < frames; i++)
    {
        machine.Update();
    }
}
