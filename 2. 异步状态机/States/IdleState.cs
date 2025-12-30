namespace AsyncStatePattern.States;

/// <summary>
/// 空闲状态 - 等待任务开始
/// </summary>
public class IdleState : IAsyncState
{
    public async Task HandleAsync(AsyncContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 空闲状态: 准备开始处理任务...");
        
        // 模拟一些准备工作
        await Task.Delay(500, cancellationToken);
        
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 空闲状态: 准备完成，开始处理");
        
        // 转换到处理状态
        await context.SetStateAsync(new ProcessingState(), cancellationToken);
    }
    
    public async Task OnEnterAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] → 进入空闲状态");
        await Task.CompletedTask;
    }
    
    public async Task OnExitAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ← 退出空闲状态");
        await Task.CompletedTask;
    }
    
    public string GetStateName() => "空闲状态 (Idle)";
}
