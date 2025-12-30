using ParallelStatePattern;
using ParallelStatePattern.Regions;

/// <summary>
/// 并行状态机演示程序 - 游戏角色系统
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║        并行状态机演示 - 游戏角色状态系统              ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════╝\n");
        
        // 创建游戏角色
        var hero = new GameCharacter("勇者");
        
        // 注册状态变更事件
        hero.OnStateChanged += (snapshot) =>
        {
            Console.WriteLine($"  [状态快照] {snapshot}");
        };
        
        // 打印初始状态
        hero.PrintStatus();
        
        Console.WriteLine("\n====== 场景1: 基础移动 ======");
        hero.SendEvent(new GameEvent(GameEventType.StartMove));
        SimulateTime(hero, 0.5f);
        
        hero.SendEvent(new GameEvent(GameEventType.StartRun));
        SimulateTime(hero, 0.5f);
        
        hero.SendEvent(new GameEvent(GameEventType.Jump));
        SimulateTime(hero, 1.2f); // 跳跃+下落
        
        hero.PrintStatus();
        
        Console.WriteLine("\n====== 场景2: 战斗动作 ======");
        hero.SendEvent(new GameEvent(GameEventType.Attack));
        SimulateTime(hero, 1.0f);
        
        hero.SendEvent(new GameEvent(GameEventType.CastSpell));
        SimulateTime(hero, 2.0f);
        
        hero.SendEvent(new GameEvent(GameEventType.Block));
        hero.SendEvent(new GameEvent(GameEventType.StartMove)); // 防御时尝试移动
        hero.PrintStatus();
        SimulateTime(hero, 0.5f);
        hero.SendEvent(new GameEvent(GameEventType.StopMove)); // 停止防御
        
        Console.WriteLine("\n====== 场景3: Buff效果 ======");
        hero.SendEvent(new GameEvent(GameEventType.ApplySpeed));
        hero.SendEvent(new GameEvent(GameEventType.StartRun));
        hero.PrintStatus();
        
        Console.WriteLine("\n等待Buff效果...");
        SimulateTime(hero, 3.0f);
        
        hero.SendEvent(new GameEvent(GameEventType.ApplyPoison));
        hero.SendEvent(new GameEvent(GameEventType.ApplySlow));
        hero.PrintStatus();
        
        Console.WriteLine("\n中毒持续伤害...");
        for (int i = 0; i < 4; i++)
        {
            SimulateTime(hero, 1.0f);
            Console.WriteLine($"  HP: {hero.CurrentHealth}/{hero.MaxHealth}");
        }
        
        Console.WriteLine("\n====== 场景4: 受伤与防御 ======");
        hero.SendEvent(new GameEvent(GameEventType.Block));
        hero.SendEvent(new GameEvent(GameEventType.TakeDamage, 30));
        hero.SendEvent(new GameEvent(GameEventType.StopMove)); // 停止防御
        
        hero.SendEvent(new GameEvent(GameEventType.TakeDamage, 30));
        hero.PrintStatus();
        
        Console.WriteLine("\n====== 场景5: 死亡与复活 ======");
        hero.SendEvent(new GameEvent(GameEventType.TakeDamage, 100));
        hero.PrintStatus();
        
        Console.WriteLine("\n3秒后复活...");
        SimulateTime(hero, 3.0f);
        hero.Revive();
        hero.PrintStatus();
        
        Console.WriteLine("\n复活无敌期间受到攻击...");
        hero.SendEvent(new GameEvent(GameEventType.TakeDamage, 50));
        hero.PrintStatus();
        
        Console.WriteLine("\n====== 场景6: 并行状态演示 ======");
        Console.WriteLine("同时进行多个状态:");
        hero.SendEvent(new GameEvent(GameEventType.StartRun));
        hero.SendEvent(new GameEvent(GameEventType.Attack));
        hero.SendEvent(new GameEvent(GameEventType.ApplySpeed));
        hero.PrintStatus();
        
        Console.WriteLine("\n持续更新状态...");
        for (int i = 0; i < 5; i++)
        {
            SimulateTime(hero, 0.3f);
            Console.WriteLine($"  时间 +0.3s | 状态: {hero.GetStateSnapshot()}");
        }
        
        hero.PrintStatus();
        
        Console.WriteLine("\n╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    演示结束                            ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════╝");
    }
    
    /// <summary>
    /// 模拟时间流逝
    /// </summary>
    static void SimulateTime(GameCharacter character, float totalTime)
    {
        const float deltaTime = 0.1f;
        for (float t = 0; t < totalTime; t += deltaTime)
        {
            character.Update(deltaTime);
        }
    }
}
