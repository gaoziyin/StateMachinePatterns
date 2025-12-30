namespace NestedStatePattern.States;

/// <summary>
/// 已完成状态 - 订单最终状态
/// </summary>
public class CompletedState : SimpleState
{
    public override void Handle(StateContext context)
    {
        Console.WriteLine("  [已完成] 订单已完成，感谢您的购买！");
        Console.WriteLine("  [已完成] 订单流程结束");
    }
    
    public override void OnEnter(StateContext context)
    {
        Console.WriteLine("  → 进入状态: 已完成 ✓✓✓");
        Console.WriteLine("  ========================================");
        Console.WriteLine("  🎉 订单处理完成！");
        Console.WriteLine("  ========================================");
    }
    
    public override void OnExit(StateContext context)
    {
        Console.WriteLine("  ← 退出状态: 已完成");
    }
    
    public override string GetStateName() => "已完成";
}
