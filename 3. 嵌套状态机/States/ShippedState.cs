namespace NestedStatePattern.States;

/// <summary>
/// 已发货状态 - 复合状态，包含运输子状态
/// </summary>
public class ShippedState : CompositeState
{
    private bool _isComplete = false;
    
    protected override void InitializeSubStates()
    {
        _subStates.Clear();
        _subStates.Add(new PickedUpSubState(this));
        _subStates.Add(new InTransitSubState(this));
        _subStates.Add(new OutForDeliverySubState(this));
    }
    
    protected override IState GetInitialSubState()
    {
        return _subStates[0];
    }
    
    public override void Handle(StateContext context)
    {
        base.Handle(context);
        
        if (IsSubStateMachineComplete())
        {
            Console.WriteLine("  [已发货] 配送完成，订单已送达");
            context.SetState(new CompletedState());
        }
    }
    
    public override string GetStateName() => "已发货";
    
    public override bool IsSubStateMachineComplete() => _isComplete;
    
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
        }
    }
}

#region 发货的子状态

/// <summary>
/// 已揽收子状态
/// </summary>
public class PickedUpSubState : SimpleState
{
    private readonly ShippedState _parent;
    
    public PickedUpSubState(ShippedState parent)
    {
        _parent = parent;
    }
    
    public override void Handle(StateContext context)
    {
        Console.WriteLine("    [已揽收] 快递员已取件，准备运输");
        _parent.AdvanceToNextSubState(context);
    }
    
    public override void OnEnter(StateContext context)
    {
        Console.WriteLine("    → 进入子状态: 已揽收");
    }
    
    public override void OnExit(StateContext context)
    {
        Console.WriteLine("    ← 退出子状态: 已揽收");
    }
    
    public override string GetStateName() => "已揽收";
}

/// <summary>
/// 运输中子状态
/// </summary>
public class InTransitSubState : SimpleState
{
    private readonly ShippedState _parent;
    private int _transitStep = 0;
    
    public InTransitSubState(ShippedState parent)
    {
        _parent = parent;
    }
    
    public override void Handle(StateContext context)
    {
        _transitStep++;
        var locations = new[] { "分拣中心A", "转运中心", "分拣中心B" };
        
        if (_transitStep <= locations.Length)
        {
            Console.WriteLine($"    [运输中] 到达: {locations[_transitStep - 1]}");
        }
        
        if (_transitStep >= locations.Length)
        {
            Console.WriteLine("    [运输中] 到达目的地城市 ✓");
            _parent.AdvanceToNextSubState(context);
        }
    }
    
    public override void OnEnter(StateContext context)
    {
        Console.WriteLine("    → 进入子状态: 运输中");
    }
    
    public override void OnExit(StateContext context)
    {
        Console.WriteLine("    ← 退出子状态: 运输中");
    }
    
    public override string GetStateName() => "运输中";
}

/// <summary>
/// 派送中子状态
/// </summary>
public class OutForDeliverySubState : SimpleState
{
    private readonly ShippedState _parent;
    
    public OutForDeliverySubState(ShippedState parent)
    {
        _parent = parent;
    }
    
    public override void Handle(StateContext context)
    {
        Console.WriteLine("    [派送中] 快递员正在派送，已送达 ✓");
        _parent.AdvanceToNextSubState(context);
    }
    
    public override void OnEnter(StateContext context)
    {
        Console.WriteLine("    → 进入子状态: 派送中");
        Console.WriteLine("    [派送中] 快递员取件派送中...");
    }
    
    public override void OnExit(StateContext context)
    {
        Console.WriteLine("    ← 退出子状态: 派送中");
    }
    
    public override string GetStateName() => "派送中";
}

#endregion
