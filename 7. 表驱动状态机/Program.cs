using TableDrivenStatePattern;

/// <summary>
/// 表驱动状态机演示程序
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         表驱动状态机演示 - 配置化状态定义             ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════╝");
        
        // 演示1: 使用JSON配置的交通灯
        DemoTrafficLight();
        
        // 演示2: 使用Builder API构建的自动售货机
        DemoVendingMachineWithBuilder();
        
        // 演示3: 动态修改状态机
        DemoDynamicModification();
    }
    
    static void DemoTrafficLight()
    {
        Console.WriteLine("\n\n========== 演示1: 交通灯状态机 (JSON配置) ==========\n");
        
        // 从JSON加载状态机定义
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", "traffic-light.json");
        
        StateMachineDefinition definition;
        if (File.Exists(configPath))
        {
            definition = StateMachineLoader.LoadFromFile(configPath);
        }
        else
        {
            // 如果文件不存在，使用内嵌的JSON
            definition = CreateTrafficLightDefinition();
        }
        
        var machine = new TableDrivenStateMachine(definition);
        
        // 订阅事件
        machine.OnStateChanged += (from, to, evt) =>
        {
            Console.WriteLine($"  [状态变更] {from} → {to} (事件: {evt})");
        };
        
        machine.PrintStatus();
        
        // 正常循环
        Console.WriteLine("\n--- 正常交通灯循环 ---");
        for (int i = 0; i < 2; i++)
        {
            machine.Send("TIMER");
            machine.Send("TIMER");
            machine.Send("TIMER");
        }
        
        machine.PrintStatus();
        
        // 紧急模式
        Console.WriteLine("\n--- 紧急模式 ---");
        machine.Send("EMERGENCY");
        machine.Send("TIMER"); // 应该被阻止
        
        machine.PrintStatus();
        
        // 重置
        Console.WriteLine("\n--- 重置 ---");
        machine.Send("RESET");
        machine.PrintStatus();
    }
    
    static StateMachineDefinition CreateTrafficLightDefinition()
    {
        return StateMachineLoader.CreateBuilder("traffic-light", "交通灯状态机")
            .WithVersion("1.0")
            .WithInitialState("red")
            .WithContext("cycleCount", 0)
            .WithContext("isEmergency", false)
            .AddState("red")
                .WithDescription("红灯 - 停止")
                .OnEntry("log", new() { ["message"] = "🔴 红灯亮起" })
                .OnEntry("increment", new() { ["key"] = "cycleCount", ["amount"] = 1 })
                .On("TIMER", "green", "!isEmergency")
                .On("EMERGENCY", "red")
            .AddState("green")
                .WithDescription("绿灯 - 通行")
                .OnEntry("log", new() { ["message"] = "🟢 绿灯亮起" })
                .On("TIMER", "yellow")
                .On("EMERGENCY", "red")
            .AddState("yellow")
                .WithDescription("黄灯 - 准备停止")
                .OnEntry("log", new() { ["message"] = "🟡 黄灯亮起" })
                .On("TIMER", "red")
                .On("EMERGENCY", "red")
            .Build();
    }
    
    static void DemoVendingMachineWithBuilder()
    {
        Console.WriteLine("\n\n========== 演示2: 自动售货机 (Builder API) ==========\n");
        
        var definition = StateMachineLoader.CreateBuilder("vending-machine", "自动售货机")
            .WithVersion("1.0")
            .WithInitialState("idle")
            .WithContext("balance", 0)
            .WithContext("selectedItem", "")
            .WithContext("itemPrice", 0)
            .AddState("idle")
                .WithDescription("等待投币")
                .OnEntry("log", new() { ["message"] = "💰 欢迎使用，请投币" })
                .On("INSERT_COIN", "hasCredit")
            .AddState("hasCredit")
                .WithDescription("已投币")
                .OnEntry("log", new() { ["message"] = "💵 余额: ${balance} 元" })
                .On("INSERT_COIN", "hasCredit")
                .On("SELECT_ITEM", "checkBalance")
                .On("REFUND", "idle")
            .AddState("checkBalance")
                .WithDescription("检查余额")
                .On("SUFFICIENT", "dispensing", "balance > itemPrice")
                .On("INSUFFICIENT", "hasCredit")
            .AddState("dispensing")
                .WithDescription("出货中")
                .OnEntry("log", new() { ["message"] = "📦 出货中..." })
                .OnEntry("delay", new() { ["ms"] = 200 })
                .On("COMPLETE", "idle")
            .Build();
            
        var machine = new TableDrivenStateMachine(definition);
        
        machine.PrintStatus();
        
        // 模拟购买流程
        Console.WriteLine("\n--- 购买流程 ---");
        
        // 投币
        machine.Send("INSERT_COIN", new() { ["balance"] = 5 });
        machine.PrintStatus();
        
        // 追加投币
        machine.Send("INSERT_COIN", new() { ["balance"] = 10 });
        
        // 选择商品
        machine.Send("SELECT_ITEM", new() 
        { 
            ["selectedItem"] = "可乐",
            ["itemPrice"] = 3
        });
        
        // 检查余额 - 足够
        machine.Send("SUFFICIENT");
        
        // 出货完成
        machine.Send("COMPLETE");
        
        machine.PrintStatus();
    }
    
    static void DemoDynamicModification()
    {
        Console.WriteLine("\n\n========== 演示3: 动态修改状态机 ==========\n");
        
        // 创建初始定义
        var definition = StateMachineLoader.CreateBuilder("workflow", "工作流")
            .WithVersion("1.0")
            .WithInitialState("draft")
            .AddState("draft")
                .WithDescription("草稿")
                .OnEntry("log", new() { ["message"] = "📝 创建草稿" })
                .On("SUBMIT", "pending")
            .AddState("pending")
                .WithDescription("待审批")
                .On("APPROVE", "approved")
                .On("REJECT", "rejected")
            .AddState("approved")
                .WithDescription("已通过")
                .AsFinal()
            .AddState("rejected")
                .WithDescription("已拒绝")
                .On("RESUBMIT", "draft")
            .Build();
            
        Console.WriteLine("--- 原始状态机 ---");
        Console.WriteLine(StateMachineLoader.ToJson(definition));
        
        // 动态添加新状态
        definition.States["reviewing"] = new StateDefinition
        {
            Name = "审核中",
            Description = "人工审核",
            On = new Dictionary<string, TransitionDefinition>
            {
                ["COMPLETE_REVIEW"] = new TransitionDefinition { Target = "pending" }
            },
            OnEntry = new List<ActionDefinition>
            {
                new() { Type = "log", Params = new() { ["message"] = "🔍 开始人工审核" } }
            }
        };
        
        // 修改现有转换
        definition.States["draft"].On!["SUBMIT"] = new TransitionDefinition { Target = "reviewing" };
        
        Console.WriteLine("\n--- 修改后的状态机 ---");
        
        var machine = new TableDrivenStateMachine(definition);
        
        machine.Send("SUBMIT");
        machine.Send("COMPLETE_REVIEW");
        machine.Send("APPROVE");
        
        machine.PrintStatus();
        
        Console.WriteLine("\n╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    演示结束                            ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════╝");
    }
}
