using EventDrivenStatePattern.Examples;

/// <summary>
/// 事件驱动状态机演示程序
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║       事件驱动状态机演示 - 带守卫条件的状态转换        ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════╝");
        
        // 演示1: 订单状态机
        DemoOrderStateMachine();
        
        // 演示2: 电梯控制系统
        DemoElevatorStateMachine();
    }
    
    static void DemoOrderStateMachine()
    {
        Console.WriteLine("\n\n========== 演示1: 订单状态机 ==========\n");
        
        var order = new Order("ORD-2024-001", 299.99m);
        order.PrintStatus();
        
        // 正常流程
        Console.WriteLine("\n--- 正常订单流程 ---");
        order.Submit();
        order.PrintStatus();
        
        order.Pay();
        order.PrintStatus();
        
        order.StartProcessing();
        order.Ship();
        order.Deliver();
        order.Confirm();
        order.PrintStatus();
        
        // 尝试取消已完成订单
        Console.WriteLine("\n--- 尝试取消已完成订单 ---");
        order.Cancel();
        
        // 新订单 - 取消流程
        Console.WriteLine("\n--- 订单取消流程 ---");
        var order2 = new Order("ORD-2024-002", 599.00m);
        order2.Submit();
        order2.Cancel();
        order2.PrintStatus();
        
        // 新订单 - 退款流程
        Console.WriteLine("\n--- 订单退款流程 ---");
        var order3 = new Order("ORD-2024-003", 199.00m);
        order3.Submit();
        order3.Pay();
        order3.StartProcessing();
        order3.RequestRefund(); // 发货前可退款
        order3.ApproveRefund();
        order3.PrintStatus();
        
        // 打印状态图
        Console.WriteLine("\n--- 状态图 (DOT格式) ---");
        Console.WriteLine(order.GetStateDiagram());
    }
    
    static void DemoElevatorStateMachine()
    {
        Console.WriteLine("\n\n========== 演示2: 电梯控制系统 ==========\n");
        
        var elevator = new Elevator();
        elevator.PrintStatus();
        
        // 正常运行
        Console.WriteLine("\n--- 正常呼叫 ---");
        elevator.CallToFloor(5);
        elevator.SimulateArrival();
        elevator.PrintStatus();
        
        elevator.CloseDoors();
        elevator.DoorsClosed();
        elevator.PrintStatus();
        
        // 多层呼叫
        Console.WriteLine("\n--- 多层呼叫 ---");
        elevator.CallToFloor(10);
        elevator.SimulateArrival();
        
        elevator.CallToFloor(3);
        elevator.CloseDoors();
        elevator.DoorsClosed();
        
        elevator.CallToFloor(3); // 继续下降
        elevator.SimulateArrival();
        elevator.PrintStatus();
        
        // 障碍物检测
        Console.WriteLine("\n--- 障碍物检测 ---");
        elevator.CloseDoors();
        elevator.SetObstruction(true);
        elevator.PrintStatus();
        
        elevator.SetObstruction(false);
        elevator.CloseDoors();
        elevator.DoorsClosed();
        
        // 紧急停止
        Console.WriteLine("\n--- 紧急停止 ---");
        elevator.CallToFloor(15);
        elevator.EmergencyStop();
        elevator.PrintStatus();
        
        // 尝试呼叫（应该被忽略）
        Console.WriteLine("\n--- 紧急状态下尝试呼叫 ---");
        elevator.CallToFloor(1);
        
        // 重置
        Console.WriteLine("\n--- 重置电梯 ---");
        elevator.Reset();
        elevator.PrintStatus();
        
        // 维护模式
        Console.WriteLine("\n--- 维护模式 ---");
        elevator.StartMaintenance();
        elevator.CallToFloor(10); // 应该被忽略
        elevator.EndMaintenance();
        elevator.PrintStatus();
        
        Console.WriteLine("\n╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    演示结束                            ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════╝");
    }
}
