namespace StatePattern;

/// <summary>
/// 具体状态C - 实现状态特定的行为
/// </summary>
public class ConcreteStateC : IState
{
    public void Handle(Context context)
    {
        Console.WriteLine("状态C正在处理请求...");
        Console.WriteLine("执行状态C特有的业务逻辑");
        
        // 回到状态A，形成循环
        Console.WriteLine("满足转换条件，切换回状态A");
        context.SetState(new ConcreteStateA());
    }
    
    public string GetStateName() => "状态C";
}
