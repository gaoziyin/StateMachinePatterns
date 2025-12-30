namespace AsyncStatePattern.States;

/// <summary>
/// 处理状态 - 正在执行异步任务
/// </summary>
public class ProcessingState : IAsyncState
{
    private int _progress = 0;
    
    public async Task HandleAsync(AsyncContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 处理状态: 开始执行异步任务...");
        
        // 模拟异步处理过程，带进度更新
        for (int i = 1; i <= 5; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            await Task.Delay(300, cancellationToken);
            _progress = i * 20;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 处理状态: 进度 {_progress}%");
        }
        
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 处理状态: 任务处理完成");
        
        // 模拟随机结果：80%成功，20%失败
        var random = new Random();
        if (random.Next(100) < 80)
        {
            await context.SetStateAsync(new CompletedState(), cancellationToken);
        }
        else
        {
            await context.SetStateAsync(new FailedState("随机模拟失败"), cancellationToken);
        }
    }
    
    public async Task OnEnterAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] → 进入处理状态");
        _progress = 0;
        await Task.CompletedTask;
    }
    
    public async Task OnExitAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ← 退出处理状态 (最终进度: {_progress}%)");
        await Task.CompletedTask;
    }
    
    public string GetStateName() => "处理状态 (Processing)";
}
