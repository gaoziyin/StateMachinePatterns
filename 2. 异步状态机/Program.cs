using AsyncStatePattern;
using AsyncStatePattern.States;

/// <summary>
/// 异步状态模式演示程序
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("========== 异步状态模式演示 ==========\n");
        
        // 创建取消令牌源（用于演示取消功能）
        using var cts = new CancellationTokenSource();
        
        // 注册 Ctrl+C 取消处理
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\n[取消请求] 正在取消操作...");
            cts.Cancel();
        };
        
        try
        {
            // 创建异步上下文，初始状态为空闲状态
            var context = new AsyncContext(new IdleState());
            
            // 注册状态变更事件
            context.StateChanged += async (previousState, newState) =>
            {
                await Task.Run(() =>
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [事件] 状态已从 '{previousState.GetStateName()}' 变更为 '{newState.GetStateName()}'");
                });
            };
            
            // 执行多次异步请求
            for (int i = 1; i <= 3; i++)
            {
                Console.WriteLine($"\n{"="u8.ToArray().Length}========== 第 {i} 轮任务 ==========");
                
                // 空闲 -> 处理
                await context.RequestAsync(cts.Token);
                
                // 处理 -> 完成/失败
                await context.RequestAsync(cts.Token);
                
                // 完成/失败 -> 空闲（或重试）
                await context.RequestAsync(cts.Token);
                
                // 如果还在重试状态，继续处理
                while (context.GetCurrentState() is RetryingState or FailedState)
                {
                    await context.RequestAsync(cts.Token);
                }
                
                Console.WriteLine($"\n第 {i} 轮任务完成，当前状态: {context.GetCurrentState().GetStateName()}");
                
                if (i < 3)
                {
                    Console.WriteLine("\n等待 1 秒后开始下一轮...");
                    await Task.Delay(1000, cts.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n[取消] 操作已被用户取消");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[错误] {ex.Message}");
        }
        
        Console.WriteLine("\n========== 演示结束 ==========");
    }
}
