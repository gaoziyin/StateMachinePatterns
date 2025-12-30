namespace PushdownAutomaton;

/// <summary>
/// 栈驱动状态机演示程序
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("========== 栈驱动状态机演示 ==========\n");
        
        var sm = new PushdownStateMachine();
        sm.PushState(new WelcomeState()); // 设置初始状态

        // 只要状态机还活动（栈不为空），就持续更新
        while (sm.CurrentState != null)
        {
            sm.Update();
            Console.WriteLine(); // 增加一些空行，使输出更清晰
        }
        
        Console.WriteLine("\n========== 演示结束 ==========");
    }
}
