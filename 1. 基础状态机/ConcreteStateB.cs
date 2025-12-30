namespace StatePattern;

/// <summary>
/// 具体状态B - 实现状态特定的行为
/// </summary>
public class ConcreteStateB : IState
{
    public void Handle(Context context)
    {
        Console.WriteLine("状态B正在处理请求...");
        Console.WriteLine("执行状态B特有的业务逻辑");
        
        // 根据条件转换到状态C
        Console.WriteLine("满足转换条件，切换到状态C");
        context.SetState(new ConcreteStateC());
    }
    
    public string GetStateName() => "状态B";
}
