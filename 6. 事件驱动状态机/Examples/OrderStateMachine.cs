namespace EventDrivenStatePattern.Examples;

/// <summary>
/// 订单状态
/// </summary>
public enum OrderState
{
    Created,        // 已创建
    Pending,        // 待支付
    Paid,           // 已支付
    Processing,     // 处理中
    Shipped,        // 已发货
    Delivered,      // 已送达
    Completed,      // 已完成
    Cancelled,      // 已取消
    Refunding,      // 退款中
    Refunded        // 已退款
}

/// <summary>
/// 订单事件
/// </summary>
public enum OrderEvent
{
    Submit,         // 提交订单
    Pay,            // 支付
    PaymentFailed,  // 支付失败
    StartProcessing,// 开始处理
    Ship,           // 发货
    Deliver,        // 送达
    Confirm,        // 确认收货
    Cancel,         // 取消
    RequestRefund,  // 申请退款
    ApproveRefund,  // 批准退款
    RejectRefund    // 拒绝退款
}

/// <summary>
/// 订单实体
/// </summary>
public class Order
{
    public string OrderId { get; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public bool IsPaid { get; private set; }
    public bool CanCancel => !IsPaid || (DateTime.Now - PaidAt!.Value).TotalHours < 24;
    
    private readonly StateMachine<OrderState, OrderEvent> _stateMachine;
    
    public OrderState CurrentState => _stateMachine.CurrentState;
    
    public Order(string orderId, decimal amount)
    {
        OrderId = orderId;
        Amount = amount;
        CreatedAt = DateTime.Now;
        
        _stateMachine = new StateMachine<OrderState, OrderEvent>(OrderState.Created);
        ConfigureStateMachine();
    }
    
    private void ConfigureStateMachine()
    {
        // 已创建状态
        _stateMachine.Configure(OrderState.Created)
            .Permit(OrderEvent.Submit, OrderState.Pending)
            .Permit(OrderEvent.Cancel, OrderState.Cancelled)
            .OnEntry(() => Console.WriteLine($"    📝 订单 {OrderId} 已创建"))
            .OnExit(() => Console.WriteLine($"    ← 离开创建状态"));
            
        // 待支付状态
        _stateMachine.Configure(OrderState.Pending)
            .Permit(OrderEvent.Pay, OrderState.Paid)
            .Permit(OrderEvent.PaymentFailed, OrderState.Pending) // 可重试
            .PermitIf(OrderEvent.Cancel, OrderState.Cancelled, 
                () => CanCancel, "检查是否可取消")
            .OnEntry(() => Console.WriteLine($"    💰 等待支付，金额: ¥{Amount}"))
            .OnExit(() => Console.WriteLine($"    ← 离开待支付状态"));
            
        // 已支付状态
        _stateMachine.Configure(OrderState.Paid)
            .Permit(OrderEvent.StartProcessing, OrderState.Processing)
            .PermitIf(OrderEvent.Cancel, OrderState.Refunding,
                () => CanCancel, "24小时内可取消")
            .OnEntry(() =>
            {
                IsPaid = true;
                PaidAt = DateTime.Now;
                Console.WriteLine($"    ✅ 支付成功! 时间: {PaidAt:HH:mm:ss}");
            });
            
        // 处理中状态
        _stateMachine.Configure(OrderState.Processing)
            .Permit(OrderEvent.Ship, OrderState.Shipped)
            .PermitIf(OrderEvent.RequestRefund, OrderState.Refunding,
                () => !ShippedAt.HasValue, "发货前可退款")
            .OnEntry(() => Console.WriteLine($"    �icing 订单处理中..."));
            
        // 已发货状态
        _stateMachine.Configure(OrderState.Shipped)
            .Permit(OrderEvent.Deliver, OrderState.Delivered)
            .Ignore(OrderEvent.Cancel) // 发货后忽略取消
            .OnEntry(() =>
            {
                ShippedAt = DateTime.Now;
                Console.WriteLine($"    🚚 已发货! 时间: {ShippedAt:HH:mm:ss}");
            });
            
        // 已送达状态
        _stateMachine.Configure(OrderState.Delivered)
            .Permit(OrderEvent.Confirm, OrderState.Completed)
            .Permit(OrderEvent.RequestRefund, OrderState.Refunding)
            .OnEntry(() => Console.WriteLine($"    📦 包裹已送达!"));
            
        // 已完成状态
        _stateMachine.Configure(OrderState.Completed)
            .PermitIf(OrderEvent.RequestRefund, OrderState.Refunding,
                () => (DateTime.Now - CompletedAt!.Value).TotalDays <= 7,
                "7天内可申请退款")
            .OnEntry(() =>
            {
                CompletedAt = DateTime.Now;
                Console.WriteLine($"    🎉 订单完成! 感谢您的购买!");
            });
            
        // 已取消状态
        _stateMachine.Configure(OrderState.Cancelled)
            .OnEntry(() => Console.WriteLine($"    ❌ 订单已取消"));
            
        // 退款中状态
        _stateMachine.Configure(OrderState.Refunding)
            .Permit(OrderEvent.ApproveRefund, OrderState.Refunded)
            .Permit(OrderEvent.RejectRefund, OrderState.Completed)
            .OnEntry(() => Console.WriteLine($"    🔄 退款申请处理中..."));
            
        // 已退款状态
        _stateMachine.Configure(OrderState.Refunded)
            .OnEntry(() => Console.WriteLine($"    💸 退款成功! 金额: ¥{Amount}"));
            
        // 订阅事件
        _stateMachine.OnTransitioned += (sender, args) =>
        {
            Console.WriteLine($"  [订单状态变更] {args.Source} → {args.Destination} (触发: {args.Trigger})");
        };
        
        _stateMachine.OnUnhandledTrigger += (sender, args) =>
        {
            Console.WriteLine($"  [警告] 当前状态 {args.State} 不能处理事件 {args.Trigger}");
        };
    }
    
    // 对外暴露的操作方法
    public void Submit() => _stateMachine.Fire(OrderEvent.Submit);
    public void Pay() => _stateMachine.Fire(OrderEvent.Pay);
    public void PaymentFailed() => _stateMachine.Fire(OrderEvent.PaymentFailed);
    public void StartProcessing() => _stateMachine.Fire(OrderEvent.StartProcessing);
    public void Ship() => _stateMachine.Fire(OrderEvent.Ship);
    public void Deliver() => _stateMachine.Fire(OrderEvent.Deliver);
    public void Confirm() => _stateMachine.Fire(OrderEvent.Confirm);
    public void Cancel() => _stateMachine.Fire(OrderEvent.Cancel);
    public void RequestRefund() => _stateMachine.Fire(OrderEvent.RequestRefund);
    public void ApproveRefund() => _stateMachine.Fire(OrderEvent.ApproveRefund);
    public void RejectRefund() => _stateMachine.Fire(OrderEvent.RejectRefund);
    
    public bool CanPerform(OrderEvent evt) => _stateMachine.CanFire(evt);
    
    public IEnumerable<OrderEvent> GetAvailableActions() => _stateMachine.GetPermittedTriggers();
    
    public void PrintStatus()
    {
        Console.WriteLine($"\n  ┌─────────────────────────────────────────┐");
        Console.WriteLine($"  │ 订单: {OrderId,-15} 状态: {CurrentState,-10} │");
        Console.WriteLine($"  │ 金额: ¥{Amount,-8} 已支付: {(IsPaid ? "是" : "否"),-10}  │");
        Console.WriteLine($"  │ 可用操作: {string.Join(", ", GetAvailableActions()),-25} │");
        Console.WriteLine($"  └─────────────────────────────────────────┘");
    }
    
    public string GetStateDiagram() => _stateMachine.GenerateDotGraph();
}
