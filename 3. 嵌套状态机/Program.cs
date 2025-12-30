using NestedStatePattern;
using NestedStatePattern.States;

/// <summary>
/// 嵌套状态模式演示程序 - 订单处理系统
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════╗");
        Console.WriteLine("║         嵌套状态模式演示 - 订单处理系统            ║");
        Console.WriteLine("╚════════════════════════════════════════════════════╝\n");
        
        // 创建订单上下文，初始状态为待处理
        var orderContext = new StateContext(new PendingState());
        
        // 注册状态变更事件
        orderContext.StateChanged += (previous, current) =>
        {
            Console.WriteLine($"  [事件] 主状态变更: {previous.GetStateName()} → {current.GetStateName()}");
        };
        
        int step = 0;
        bool isCompleted = false;
        
        while (!isCompleted)
        {
            step++;
            Console.WriteLine($"\n┌─────────────────────────────────────────┐");
            Console.WriteLine($"│  步骤 {step:D2}                                │");
            Console.WriteLine($"└─────────────────────────────────────────┘");
            
            // 打印当前状态树
            orderContext.PrintStateTree();
            
            // 处理请求
            orderContext.Request();
            
            // 检查是否完成
            if (orderContext.GetCurrentState() is CompletedState)
            {
                // 再处理一次以显示完成消息
                orderContext.Request();
                isCompleted = true;
            }
            
            // 限制最大步数（防止无限循环）
            if (step >= 20)
            {
                Console.WriteLine("\n[警告] 达到最大步数限制");
                break;
            }
        }
        
        Console.WriteLine("\n╔════════════════════════════════════════════════════╗");
        Console.WriteLine("║                   订单处理流程结束                 ║");
        Console.WriteLine("╚════════════════════════════════════════════════════╝");
        
        // 打印最终状态
        Console.WriteLine($"\n最终状态: {orderContext.GetFullStatePath()}");
        Console.WriteLine($"总步骤数: {step}");
    }
}
