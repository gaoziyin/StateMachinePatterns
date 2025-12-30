namespace PushdownAutomaton;

// 为方便演示，我们将用户数据存放在一个静态类中
public static class UserProfile
{
    public static string UserName { get; set; } = "DefaultUser";
    public static string Email { get; set; } = "default@example.com";
}

// 为方便演示，我们将预订流程的数据存放在一个静态类中
public static class BookingContext
{
    public static string? Destination { get; set; }
    public static string? Date { get; set; }
}


#region Base State
/// <summary>
/// 提供默认空实现的基础状态类，避免在具体类中重复实现所有方法
/// </summary>
public abstract class BaseState : IState
{
    public virtual void Enter() {}
    public abstract void Update(PushdownStateMachine sm);
    public virtual void Exit() {}

    protected void PrintState(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"--- {GetType().Name} ---");
        Console.ResetColor();
        Console.WriteLine(message);
    }
}
#endregion


#region Booking States (Main Flow)

public class WelcomeState : BaseState
{
    public override void Update(PushdownStateMachine sm)
    {
        PrintState("欢迎来到旅行预订系统! 按任意键开始...");
        Console.ReadKey(true);
        sm.ChangeState(new SelectDestinationState());
    }
}

public class SelectDestinationState : BaseState
{
    public override void Update(PushdownStateMachine sm)
    {
        PrintState("请输入您的目的地 (例如, '北京', '上海'), 或按 'P' 编辑您的个人资料:");
        Console.Write("> ");
        var input = Console.ReadLine();

        if (input?.ToUpper() == "P")
        {
            sm.PushState(new ViewProfileState());
        }
        else if (!string.IsNullOrWhiteSpace(input))
        {
            BookingContext.Destination = input;
            sm.ChangeState(new SelectDatesState());
        }
    }
}

public class SelectDatesState : BaseState
{
    public override void Update(PushdownStateMachine sm)
    {
        PrintState($"已选择目的地: {BookingContext.Destination}. 请输入旅行日期 (例如, '2025-12-25'), 或按 'P' 编辑个人资料:");
        Console.Write("> ");
        var input = Console.ReadLine();

        if (input?.ToUpper() == "P")
        {
            sm.PushState(new ViewProfileState());
        }
        else if (!string.IsNullOrWhiteSpace(input))
        {
            BookingContext.Date = input;
            sm.ChangeState(new ConfirmState());
        }
    }
}

public class ConfirmState : BaseState
{
    public override void Update(PushdownStateMachine sm)
    {
        PrintState("预订信息确认:");
        Console.WriteLine($"目的地: {BookingContext.Destination}");
        Console.WriteLine($"日期: {BookingContext.Date}");
        Console.WriteLine("按 'Y' 确认预订, 或按任意键返回主菜单.");
        
        var key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.Y)
        {
            sm.ChangeState(new DoneState());
        }
        else
        {
            sm.ChangeState(new WelcomeState());
        }
    }
}

public class DoneState : BaseState
{
    public override void Update(PushdownStateMachine sm)
    {
        PrintState("感谢您的预订! 按任意键退出。");
        Console.ReadKey(true);
        // 清空栈来结束程序
        while(sm.CurrentState != null)
        {
            sm.PopState();
        }
    }
}
#endregion


#region Profile States (Sub-flow)

public class ViewProfileState : BaseState
{
    public override void Update(PushdownStateMachine sm)
    {
        PrintState("查看个人资料");
        Console.WriteLine($"用户名: {UserProfile.UserName}");
        Console.WriteLine($"邮箱: {UserProfile.Email}");
        Console.WriteLine("\n按 'E' 编辑, 或按 'S' 保存并返回上一页.");
        Console.Write("> ");
        var input = Console.ReadLine();

        if (input?.ToUpper() == "E")
        {
            sm.ChangeState(new EditProfileState());
        }
        else if (input?.ToUpper() == "S")
        {
            sm.PopState(); // 从子流程返回
        }
    }
}

public class EditProfileState : BaseState
{
    public override void Update(PushdownStateMachine sm)
    {
        PrintState("编辑个人资料");
        Console.Write("请输入新用户名: ");
        var newName = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(newName))
        {
            UserProfile.UserName = newName;
        }

        Console.Write("请输入新邮箱: ");
        var newEmail = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(newEmail))
        {
            UserProfile.Email = newEmail;
        }

        Console.WriteLine("资料已更新。按任意键返回查看资料...");
        Console.ReadKey(true);
        sm.ChangeState(new ViewProfileState()); // 返回查看状态
    }
}

#endregion
