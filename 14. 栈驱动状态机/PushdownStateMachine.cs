using System.Text;

namespace PushdownAutomaton;

/// <summary>
/// 栈驱动状态机 - 上下文类
/// </summary>
public class PushdownStateMachine
{
    private readonly Stack<IState> _stateStack = new();

    /// <summary>
    /// 当前活动状态（栈顶）
    /// </summary>
    public IState? CurrentState => _stateStack.Count > 0 ? _stateStack.Peek() : null;

    /// <summary>
    /// 压入一个新状态到栈顶，使其成为当前状态
    /// </summary>
    public void PushState(IState state)
    {
        CurrentState?.Exit();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[压栈] {state.GetType().Name}");
        Console.ResetColor();
        
        _stateStack.Push(state);
        state.Enter();
    }

    /// <summary>
    /// 弹出栈顶状态，恢复到前一个状态
    /// </summary>
    public void PopState()
    {
        if (CurrentState == null) return;
        
        CurrentState.Exit();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[弹栈] {CurrentState.GetType().Name}");
        Console.ResetColor();
        
        _stateStack.Pop();
        CurrentState?.Enter();
    }

    /// <summary>
    /// 替换栈顶状态，用于同一层级的状态转换
    /// </summary>
    public void ChangeState(IState state)
    {
        if (CurrentState != null)
        {
            CurrentState.Exit();
            _stateStack.Pop();
        }
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[切换] -> {state.GetType().Name}");
        Console.ResetColor();
        
        _stateStack.Push(state);
        state.Enter();
    }

    /// <summary>
    /// 将更新委托给当前状态
    /// </summary>
    public void Update()
    {
        if (CurrentState == null)
        {
            Console.WriteLine("状态栈为空，程序结束。");
            return;
        }

        PrintCurrentStack();
        CurrentState.Update(this);
    }

    /// <summary>
    /// 打印当前状态栈信息
    /// </summary>
    private void PrintCurrentStack()
    {
        var sb = new StringBuilder();
        sb.Append("当前状态栈: ");
        foreach (var state in _stateStack.Reverse())
        {
            sb.Append($"[{state.GetType().Name}]");
        }
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(sb.ToString());
        Console.ResetColor();
    }
}
