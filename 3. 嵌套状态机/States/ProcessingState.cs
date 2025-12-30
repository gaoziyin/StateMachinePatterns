namespace NestedStatePattern.States;

/// <summary>
/// 处理中状态 - 复合状态，包含多个子状态
/// </summary>
public class ProcessingState : CompositeState
{
    private bool _isComplete = false;
    
    protected override void InitializeSubStates()
    {
        _subStates.Clear();
        _subStates.Add(new ValidatingSubState(this));
        _subStates.Add(new PreparingSubState(this));
        _subStates.Add(new PackagingSubState(this));
    }
    
    protected override IState GetInitialSubState()
    {
        return _subStates[0]; // 从验证状态开始
    }
    
    public override void Handle(StateContext context)
    {
        base.Handle(context);
        
        // 检查子状态机是否完成
        if (IsSubStateMachineComplete())
        {
            Console.WriteLine("  [处理中] 所有子流程完成，准备发货");
            context.SetState(new ShippedState());
        }
    }
    
    public override string GetStateName() => "处理中";
    
    public override bool IsSubStateMachineComplete() => _isComplete;
    
    /// <summary>
    /// 推进到下一个子状态
    /// </summary>
    public void AdvanceToNextSubState(StateContext context)
    {
        if (_currentSubState == null) return;
        
        var currentIndex = _subStates.IndexOf(_currentSubState);
        if (currentIndex < _subStates.Count - 1)
        {
            SetSubState(context, _subStates[currentIndex + 1]);
        }
        else
        {
            _isComplete = true;
            Console.WriteLine("  [处理中] 子状态机已完成所有步骤");
        }
    }
}

#region 处理中的子状态

/// <summary>
/// 验证中子状态
/// </summary>
public class ValidatingSubState : SimpleState
{
    private readonly ProcessingState _parent;
    private int _validationStep = 0;
    
    public ValidatingSubState(ProcessingState parent)
    {
        _parent = parent;
    }
    
    public override void Handle(StateContext context)
    {
        _validationStep++;
        Console.WriteLine($"    [验证中] 执行验证步骤 {_validationStep}/2");
        
        if (_validationStep >= 2)
        {
            Console.WriteLine("    [验证中] 验证完成 ✓");
            _parent.AdvanceToNextSubState(context);
        }
    }
    
    public override void OnEnter(StateContext context)
    {
        Console.WriteLine("    → 进入子状态: 验证中");
        Console.WriteLine("    [验证中] 开始验证订单信息...");
    }
    
    public override void OnExit(StateContext context)
    {
        Console.WriteLine("    ← 退出子状态: 验证中");
    }
    
    public override string GetStateName() => "验证中";
}

/// <summary>
/// 准备中子状态
/// </summary>
public class PreparingSubState : SimpleState
{
    private readonly ProcessingState _parent;
    private int _prepareStep = 0;
    
    public PreparingSubState(ProcessingState parent)
    {
        _parent = parent;
    }
    
    public override void Handle(StateContext context)
    {
        _prepareStep++;
        Console.WriteLine($"    [准备中] 准备商品步骤 {_prepareStep}/2");
        
        if (_prepareStep >= 2)
        {
            Console.WriteLine("    [准备中] 商品准备完成 ✓");
            _parent.AdvanceToNextSubState(context);
        }
    }
    
    public override void OnEnter(StateContext context)
    {
        Console.WriteLine("    → 进入子状态: 准备中");
        Console.WriteLine("    [准备中] 开始准备商品...");
    }
    
    public override void OnExit(StateContext context)
    {
        Console.WriteLine("    ← 退出子状态: 准备中");
    }
    
    public override string GetStateName() => "准备中";
}

/// <summary>
/// 打包中子状态
/// </summary>
public class PackagingSubState : SimpleState
{
    private readonly ProcessingState _parent;
    private int _packStep = 0;
    
    public PackagingSubState(ProcessingState parent)
    {
        _parent = parent;
    }
    
    public override void Handle(StateContext context)
    {
        _packStep++;
        Console.WriteLine($"    [打包中] 打包步骤 {_packStep}/2");
        
        if (_packStep >= 2)
        {
            Console.WriteLine("    [打包中] 打包完成 ✓");
            _parent.AdvanceToNextSubState(context);
        }
    }
    
    public override void OnEnter(StateContext context)
    {
        Console.WriteLine("    → 进入子状态: 打包中");
        Console.WriteLine("    [打包中] 开始打包...");
    }
    
    public override void OnExit(StateContext context)
    {
        Console.WriteLine("    ← 退出子状态: 打包中");
    }
    
    public override string GetStateName() => "打包中";
}

#endregion
