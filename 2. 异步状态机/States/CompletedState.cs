namespace AsyncStatePattern.States;

/// <summary>
/// 完成状态 - 任务成功完成
/// </summary>
public class CompletedState : IAsyncState
{
    public async Task HandleAsync(AsyncContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 完成状态: 任务已成功完成!");
        
        // 模拟清理工作
        await Task.Delay(200, cancellationToken);
        
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 完成状态: 清理完成，返回空闲状态");
        
        // 返回空闲状态，准备下一个任务
        await context.SetStateAsync(new IdleState(), cancellationToken);
    }
    
    public async Task OnEnterAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] → 进入完成状态 ✓");
        await Task.CompletedTask;
    }
    
    public async Task OnExitAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ← 退出完成状态");
        await Task.CompletedTask;
    }
    
    public string GetStateName() => "完成状态 (Completed)";
}
