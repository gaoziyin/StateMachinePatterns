namespace StatePattern;

/// <summary>
/// 具体状态A - 实现状态特定的行为
/// </summary>
public class ConcreteStateA : IState
{
    public void Handle(Context context)
    {
        Console.WriteLine("状态A正在处理请求...");
        Console.WriteLine("执行状态A特有的业务逻辑");
        
        // 根据条件转换到状态B
        Console.WriteLine("满足转换条件，切换到状态B");
        context.SetState(new ConcreteStateB());
    }
    
    public string GetStateName() => "状态A";
}
