using PersistentStatePattern.Core;

namespace PersistentStatePattern.Examples.LoanApproval;

/// <summary>
/// 贷款申请触发器
/// </summary>
public static class LoanTriggers
{
    public const string Submit = "Submit";
    public const string StartReview = "StartReview";
    public const string RequestDocuments = "RequestDocuments";
    public const string ProvideDocuments = "ProvideDocuments";
    public const string ApproveReview = "ApproveReview";
    public const string RejectReview = "RejectReview";
    public const string StartUnderwriting = "StartUnderwriting";
    public const string ApproveUnderwriting = "ApproveUnderwriting";
    public const string RejectUnderwriting = "RejectUnderwriting";
    public const string Sign = "Sign";
    public const string Disburse = "Disburse";
    public const string Cancel = "Cancel";
    public const string Expire = "Expire";
}

/// <summary>
/// 草稿状态
/// </summary>
public class DraftState : PersistentStateBase
{
    public override string Name => "Draft";
    
    public DraftState()
    {
        Permit(LoanTriggers.Submit, "Submitted");
        Permit(LoanTriggers.Cancel, "Cancelled");
    }
    
    public override Task OnEnterAsync(IStateMachineContext context, CancellationToken cancellationToken = default)
    {
        context.Set("stage", "申请阶段");
        context.Set("startTime", DateTime.UtcNow);
        Console.WriteLine("    [贷款] 贷款申请已创建，请填写申请信息");
        return Task.CompletedTask;
    }
}

/// <summary>
/// 已提交状态
/// </summary>
public class SubmittedState : PersistentStateBase
{
    public override string Name => "Submitted";
    
    public SubmittedState()
    {
        Permit(LoanTriggers.StartReview, "UnderReview");
        Permit(LoanTriggers.Cancel, "Cancelled");
    }
    
    public override Task OnEnterAsync(IStateMachineContext context, CancellationToken cancellationToken = default)
    {
        context.Set("submittedAt", DateTime.UtcNow);
        Console.WriteLine("    [贷款] 申请已提交，等待分配审核人员");
        return Task.CompletedTask;
    }
}

/// <summary>
/// 审核中状态
/// </summary>
public class UnderReviewState : PersistentStateBase
{
    public override string Name => "UnderReview";
    
    public UnderReviewState()
    {
        Permit(LoanTriggers.RequestDocuments, "DocumentsPending");
        PermitIf(LoanTriggers.ApproveReview, "ReadyForUnderwriting", 
            ctx => ctx.Get<decimal>("amount") <= 100000);
        PermitIf(LoanTriggers.ApproveReview, "ManagerApproval",
            ctx => ctx.Get<decimal>("amount") > 100000);
        Permit(LoanTriggers.RejectReview, "Rejected");
    }
    
    public override Task OnEnterAsync(IStateMachineContext context, CancellationToken cancellationToken = default)
    {
        context.Set("stage", "审核阶段");
        context.Set("reviewStartedAt", DateTime.UtcNow);
        Console.WriteLine("    [贷款] 开始初步审核...");
        return Task.CompletedTask;
    }
}

/// <summary>
/// 等待材料状态
/// </summary>
public class DocumentsPendingState : PersistentStateBase
{
    public override string Name => "DocumentsPending";
    
    public DocumentsPendingState()
    {
        Permit(LoanTriggers.ProvideDocuments, "UnderReview");
        Permit(LoanTriggers.Expire, "Expired");
        Permit(LoanTriggers.Cancel, "Cancelled");
    }
    
    public override Task OnEnterAsync(IStateMachineContext context, CancellationToken cancellationToken = default)
    {
        context.Set("documentsRequestedAt", DateTime.UtcNow);
        Console.WriteLine("    [贷款] 需要补充材料，请在7天内提交");
        return Task.CompletedTask;
    }
}

/// <summary>
/// 经理审批状态（大额贷款）
/// </summary>
public class ManagerApprovalState : PersistentStateBase
{
    public override string Name => "ManagerApproval";
    
    public ManagerApprovalState()
    {
        Permit(LoanTriggers.ApproveReview, "ReadyForUnderwriting");
        Permit(LoanTriggers.RejectReview, "Rejected");
    }
    
    public override Task OnEnterAsync(IStateMachineContext context, CancellationToken cancellationToken = default)
    {
        var amount = context.Get<decimal>("amount");
        Console.WriteLine($"    [贷款] 大额贷款({amount:C})需要经理审批");
        return Task.CompletedTask;
    }
}

/// <summary>
/// 准备核保状态
/// </summary>
public class ReadyForUnderwritingState : PersistentStateBase
{
    public override string Name => "ReadyForUnderwriting";
    
    public ReadyForUnderwritingState()
    {
        Permit(LoanTriggers.StartUnderwriting, "Underwriting");
    }
    
    public override Task OnEnterAsync(IStateMachineContext context, CancellationToken cancellationToken = default)
    {
        context.Set("stage", "核保阶段");
        Console.WriteLine("    [贷款] 审核通过，准备进入核保环节");
        return Task.CompletedTask;
    }
}

/// <summary>
/// 核保中状态
/// </summary>
public class UnderwritingState : PersistentStateBase
{
    public override string Name => "Underwriting";
    
    public UnderwritingState()
    {
        Permit(LoanTriggers.ApproveUnderwriting, "Approved");
        Permit(LoanTriggers.RejectUnderwriting, "Rejected");
    }
    
    public override async Task OnEnterAsync(IStateMachineContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("    [贷款] 正在进行风险评估和核保...");
        
        // 模拟核保过程
        await Task.Delay(100, cancellationToken);
        
        // 计算信用评分
        var creditScore = new Random().Next(600, 850);
        context.Set("creditScore", creditScore);
        Console.WriteLine($"    [贷款] 信用评分: {creditScore}");
    }
}

/// <summary>
/// 已批准状态
/// </summary>
public class ApprovedState : PersistentStateBase
{
    public override string Name => "Approved";
    
    public ApprovedState()
    {
        Permit(LoanTriggers.Sign, "Signed");
        Permit(LoanTriggers.Expire, "Expired");
    }
    
    public override Task OnEnterAsync(IStateMachineContext context, CancellationToken cancellationToken = default)
    {
        context.Set("stage", "签约阶段");
        context.Set("approvedAt", DateTime.UtcNow);
        var amount = context.Get<decimal>("amount");
        var rate = context.Get<decimal>("interestRate");
        Console.WriteLine($"    [贷款] ✅ 贷款已批准！金额: {amount:C}, 利率: {rate:P2}");
        Console.WriteLine("    [贷款] 请在30天内完成签约");
        return Task.CompletedTask;
    }
}

/// <summary>
/// 已签约状态
/// </summary>
public class SignedState : PersistentStateBase
{
    public override string Name => "Signed";
    
    public SignedState()
    {
        Permit(LoanTriggers.Disburse, "Disbursed");
    }
    
    public override Task OnEnterAsync(IStateMachineContext context, CancellationToken cancellationToken = default)
    {
        context.Set("signedAt", DateTime.UtcNow);
        Console.WriteLine("    [贷款] 合同已签署，准备放款");
        return Task.CompletedTask;
    }
}

/// <summary>
/// 已放款状态（最终状态）
/// </summary>
public class DisbursedState : FinalStateBase
{
    public override string Name => "Disbursed";
    
    public override Task OnEnterAsync(IStateMachineContext context, CancellationToken cancellationToken = default)
    {
        context.Set("stage", "已完成");
        context.Set("disbursedAt", DateTime.UtcNow);
        var amount = context.Get<decimal>("amount");
        Console.WriteLine($"    [贷款] 💰 {amount:C} 已放款至您的账户");
        return base.OnEnterAsync(context, cancellationToken);
    }
}

/// <summary>
/// 已拒绝状态（最终状态）
/// </summary>
public class RejectedState : FinalStateBase
{
    public override string Name => "Rejected";
    
    public override Task OnEnterAsync(IStateMachineContext context, CancellationToken cancellationToken = default)
    {
        context.Set("stage", "已拒绝");
        context.Set("rejectedAt", DateTime.UtcNow);
        Console.WriteLine("    [贷款] ❌ 很抱歉，您的贷款申请未通过");
        return base.OnEnterAsync(context, cancellationToken);
    }
}

/// <summary>
/// 已取消状态（最终状态）
/// </summary>
public class CancelledState : FinalStateBase
{
    public override string Name => "Cancelled";
    
    public override Task OnEnterAsync(IStateMachineContext context, CancellationToken cancellationToken = default)
    {
        context.Set("stage", "已取消");
        context.Set("cancelledAt", DateTime.UtcNow);
        Console.WriteLine("    [贷款] 贷款申请已取消");
        return base.OnEnterAsync(context, cancellationToken);
    }
}

/// <summary>
/// 已过期状态（最终状态）
/// </summary>
public class ExpiredState : FinalStateBase
{
    public override string Name => "Expired";
    
    public override Task OnEnterAsync(IStateMachineContext context, CancellationToken cancellationToken = default)
    {
        context.Set("stage", "已过期");
        context.Set("expiredAt", DateTime.UtcNow);
        Console.WriteLine("    [贷款] 贷款申请已过期");
        return base.OnEnterAsync(context, cancellationToken);
    }
}
