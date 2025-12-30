namespace AsyncStatePattern.States;

/// <summary>
/// 重试状态 - 重新尝试执行任务
/// </summary>
public class RetryingState : IAsyncState
{
    private readonly int _retryCount;
    
    public RetryingState(int retryCount)
    {
        _retryCount = retryCount;
    }
    
    public async Task HandleAsync(AsyncContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 重试状态: 第 {_retryCount} 次重试开始...");
        
        // 模拟重试处理
        for (int i = 1; i <= 3; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            await Task.Delay(200, cancellationToken);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 重试状态: 重试进度 {i * 33}%");
        }
        
        // 模拟重试结果：重试次数越多，成功率越低
        var random = new Random();
        var successRate = 90 - (_retryCount * 20); // 第1次70%, 第2次50%, 第3次30%
        
        if (random.Next(100) < successRate)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 重试状态: 重试成功!");
            await context.SetStateAsync(new CompletedState(), cancellationToken);
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 重试状态: 重试失败");
            await context.SetStateAsync(new FailedState($"第 {_retryCount} 次重试失败", _retryCount), cancellationToken);
        }
    }
    
    public async Task OnEnterAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] → 进入重试状态 (第 {_retryCount} 次)");
        await Task.CompletedTask;
    }
    
    public async Task OnExitAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ← 退出重试状态");
        await Task.CompletedTask;
    }
    
    public string GetStateName() => $"重试状态 (Retrying - 第 {_retryCount} 次)";
}
