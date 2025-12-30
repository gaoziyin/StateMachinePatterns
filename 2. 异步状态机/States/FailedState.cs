namespace AsyncStatePattern.States;

/// <summary>
/// 失败状态 - 任务执行失败
/// </summary>
public class FailedState : IAsyncState
{
    private readonly string _errorMessage;
    private int _retryCount = 0;
    private const int MaxRetries = 3;
    
    public FailedState(string errorMessage, int retryCount = 0)
    {
        _errorMessage = errorMessage;
        _retryCount = retryCount;
    }
    
    public async Task HandleAsync(AsyncContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 失败状态: 错误信息 - {_errorMessage}");
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 失败状态: 当前重试次数 {_retryCount}/{MaxRetries}");
        
        if (_retryCount < MaxRetries)
        {
            // 指数退避延迟
            var delay = (int)Math.Pow(2, _retryCount) * 500;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 失败状态: 等待 {delay}ms 后重试...");
            
            await Task.Delay(delay, cancellationToken);
            
            // 重试：返回处理状态
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 失败状态: 开始重试");
            await context.SetStateAsync(new RetryingState(_retryCount + 1), cancellationToken);
        }
        else
        {
            // 超过最大重试次数
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 失败状态: 已达到最大重试次数，放弃任务");
            await Task.Delay(500, cancellationToken);
            
            // 返回空闲状态
            await context.SetStateAsync(new IdleState(), cancellationToken);
        }
    }
    
    public async Task OnEnterAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] → 进入失败状态 ✗");
        await Task.CompletedTask;
    }
    
    public async Task OnExitAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ← 退出失败状态");
        await Task.CompletedTask;
    }
    
    public string GetStateName() => $"失败状态 (Failed - 重试: {_retryCount})";
}
