namespace StatePattern;

/// <summary>
/// 状态模式演示程序
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("========== 状态模式演示 ==========\n");
        
        // 创建上下文对象，初始状态为状态A
        var context = new Context(new ConcreteStateA());
        
        // 执行多次请求，观察状态转换
        for (int i = 1; i <= 5; i++)
        {
            Console.WriteLine($"\n========== 第 {i} 次请求 ==========");
            context.Request();
        }
        
        Console.WriteLine("\n========== 演示结束 ==========");
    }
}
