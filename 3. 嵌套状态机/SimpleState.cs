namespace NestedStatePattern;

/// <summary>
/// 简单状态基类 - 不包含子状态
/// </summary>
public abstract class SimpleState : IState
{
    public abstract void Handle(StateContext context);
    
    public virtual void OnEnter(StateContext context)
    {
        Console.WriteLine($"  → 进入状态: {GetStateName()}");
    }
    
    public virtual void OnExit(StateContext context)
    {
        Console.WriteLine($"  ← 退出状态: {GetStateName()}");
    }
    
    public abstract string GetStateName();
    
    public bool IsComposite => false;
    
    public IState? GetActiveSubState() => null;
    
    public string GetFullStatePath() => GetStateName();
}
