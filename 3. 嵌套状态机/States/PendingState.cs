namespace NestedStatePattern.States;

/// <summary>
/// 待处理状态 - 订单初始状态
/// </summary>
public class PendingState : SimpleState
{
    public override void Handle(StateContext context)
    {
        Console.WriteLine("  [待处理] 订单已确认，开始处理...");
        
        // 转换到处理中状态（复合状态）
        context.SetState(new ProcessingState());
    }
    
    public override string GetStateName() => "待处理";
}
