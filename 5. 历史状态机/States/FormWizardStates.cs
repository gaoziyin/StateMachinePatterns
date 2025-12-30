namespace HistoryStatePattern.States;

/// <summary>
/// 表单向导 - 主状态（包含步骤子状态）
/// </summary>
public class FormWizardState : IState
{
    private IState _currentStep;
    private readonly Dictionary<string, object> _formData = new();
    
    public string StateName => $"表单向导({_currentStep.StateName})";
    public IState CurrentStep => _currentStep;
    public Dictionary<string, object> FormData => _formData;
    
    public FormWizardState()
    {
        _currentStep = new Step1_PersonalInfoState(this);
    }
    
    public FormWizardState(IState step, Dictionary<string, object> formData)
    {
        _currentStep = step;
        foreach (var kvp in formData)
        {
            _formData[kvp.Key] = kvp.Value;
        }
    }
    
    public void OnEnter(StateMachineContext context)
    {
        Console.WriteLine($"  → 进入: {StateName}");
        _currentStep.OnEnter(context);
    }
    
    public void OnExit(StateMachineContext context)
    {
        _currentStep.OnExit(context);
        Console.WriteLine($"  ← 退出: {StateName}");
    }
    
    public void Handle(StateMachineContext context)
    {
        _currentStep.Handle(context);
    }
    
    public void SetStep(IState step, StateMachineContext context)
    {
        _currentStep.OnExit(context);
        Console.WriteLine($"  [步骤] {_currentStep.StateName} → {step.StateName}");
        _currentStep = step;
        _currentStep.OnEnter(context);
    }
    
    public void SetData(string key, object value)
    {
        _formData[key] = value;
        Console.WriteLine($"  [数据] {key} = {value}");
    }
    
    public StateSnapshot CreateSnapshot()
    {
        var stepSnapshot = _currentStep.CreateSnapshot();
        return new StateSnapshot(
            "表单向导",
            new Dictionary<string, object>(_formData),
            stepSnapshot
        );
    }
    
    public void RestoreFromSnapshot(StateSnapshot snapshot, StateMachineContext context)
    {
        _formData.Clear();
        foreach (var kvp in snapshot.StateData)
        {
            _formData[kvp.Key] = kvp.Value;
        }
        
        if (snapshot.SubStateSnapshot != null)
        {
            // 根据快照恢复步骤状态
            _currentStep = CreateStepFromSnapshot(snapshot.SubStateSnapshot);
            _currentStep.OnEnter(context);
        }
        
        Console.WriteLine($"  [恢复] 状态: {StateName}");
        Console.WriteLine($"  [恢复] 数据: {string.Join(", ", _formData.Select(kv => $"{kv.Key}={kv.Value}"))}");
    }
    
    private IState CreateStepFromSnapshot(StateSnapshot snapshot)
    {
        return snapshot.StateName switch
        {
            "步骤1-个人信息" => new Step1_PersonalInfoState(this),
            "步骤2-联系方式" => new Step2_ContactInfoState(this),
            "步骤3-偏好设置" => new Step3_PreferencesState(this),
            "步骤4-确认提交" => new Step4_ConfirmState(this),
            _ => new Step1_PersonalInfoState(this)
        };
    }
}

/// <summary>
/// 步骤基类
/// </summary>
public abstract class StepStateBase : IState
{
    protected readonly FormWizardState _wizard;
    
    public abstract string StateName { get; }
    
    protected StepStateBase(FormWizardState wizard)
    {
        _wizard = wizard;
    }
    
    public virtual void OnEnter(StateMachineContext context)
    {
        Console.WriteLine($"    → 进入步骤: {StateName}");
    }
    
    public virtual void OnExit(StateMachineContext context)
    {
        Console.WriteLine($"    ← 退出步骤: {StateName}");
    }
    
    public abstract void Handle(StateMachineContext context);
    
    public virtual StateSnapshot CreateSnapshot()
    {
        return new StateSnapshot(StateName, new Dictionary<string, object>());
    }
    
    public virtual void RestoreFromSnapshot(StateSnapshot snapshot, StateMachineContext context)
    {
        OnEnter(context);
    }
}

/// <summary>
/// 步骤1 - 个人信息
/// </summary>
public class Step1_PersonalInfoState : StepStateBase
{
    public override string StateName => "步骤1-个人信息";
    
    public Step1_PersonalInfoState(FormWizardState wizard) : base(wizard) { }
    
    public override void Handle(StateMachineContext context)
    {
        Console.WriteLine("    [步骤1] 填写个人信息...");
        
        // 模拟填写数据
        _wizard.SetData("姓名", "张三");
        _wizard.SetData("年龄", 28);
        
        // 转到下一步
        _wizard.SetStep(new Step2_ContactInfoState(_wizard), context);
    }
}

/// <summary>
/// 步骤2 - 联系方式
/// </summary>
public class Step2_ContactInfoState : StepStateBase
{
    public override string StateName => "步骤2-联系方式";
    
    public Step2_ContactInfoState(FormWizardState wizard) : base(wizard) { }
    
    public override void Handle(StateMachineContext context)
    {
        Console.WriteLine("    [步骤2] 填写联系方式...");
        
        _wizard.SetData("邮箱", "zhangsan@example.com");
        _wizard.SetData("电话", "13800138000");
        
        _wizard.SetStep(new Step3_PreferencesState(_wizard), context);
    }
}

/// <summary>
/// 步骤3 - 偏好设置
/// </summary>
public class Step3_PreferencesState : StepStateBase
{
    public override string StateName => "步骤3-偏好设置";
    
    public Step3_PreferencesState(FormWizardState wizard) : base(wizard) { }
    
    public override void Handle(StateMachineContext context)
    {
        Console.WriteLine("    [步骤3] 设置偏好...");
        
        _wizard.SetData("语言", "中文");
        _wizard.SetData("通知", true);
        
        _wizard.SetStep(new Step4_ConfirmState(_wizard), context);
    }
}

/// <summary>
/// 步骤4 - 确认提交
/// </summary>
public class Step4_ConfirmState : StepStateBase
{
    public override string StateName => "步骤4-确认提交";
    
    public Step4_ConfirmState(FormWizardState wizard) : base(wizard) { }
    
    public override void Handle(StateMachineContext context)
    {
        Console.WriteLine("    [步骤4] 确认提交...");
        Console.WriteLine("\n    ╔═══════════════════════════════════════╗");
        Console.WriteLine("    ║           表单数据汇总                 ║");
        Console.WriteLine("    ╠═══════════════════════════════════════╣");
        
        foreach (var kvp in _wizard.FormData)
        {
            Console.WriteLine($"    ║  {kvp.Key,-10}: {kvp.Value,-20} ║");
        }
        
        Console.WriteLine("    ╚═══════════════════════════════════════╝");
        Console.WriteLine("    ✓ 表单提交成功!");
        
        // 转换到完成状态
        context.TransitionTo(new CompletedState());
    }
}

/// <summary>
/// 完成状态
/// </summary>
public class CompletedState : IState
{
    public string StateName => "已完成";
    
    public void OnEnter(StateMachineContext context)
    {
        Console.WriteLine($"  → 进入: {StateName}");
        Console.WriteLine("  🎉 流程完成!");
    }
    
    public void OnExit(StateMachineContext context)
    {
        Console.WriteLine($"  ← 退出: {StateName}");
    }
    
    public void Handle(StateMachineContext context)
    {
        Console.WriteLine("  [已完成] 可以撤销返回修改");
    }
    
    public StateSnapshot CreateSnapshot()
    {
        return new StateSnapshot(StateName, new Dictionary<string, object>());
    }
    
    public void RestoreFromSnapshot(StateSnapshot snapshot, StateMachineContext context)
    {
        OnEnter(context);
    }
}
