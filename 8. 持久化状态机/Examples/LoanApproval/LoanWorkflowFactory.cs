using PersistentStatePattern.Core;

namespace PersistentStatePattern.Examples.LoanApproval;

/// <summary>
/// 贷款审批工作流工厂
/// </summary>
public static class LoanWorkflowFactory
{
    /// <summary>
    /// 创建新的贷款申请
    /// </summary>
    public static async Task<PersistentStateMachine> CreateAsync(
        IStateStore store,
        string applicantName,
        decimal amount,
        decimal interestRate,
        CancellationToken cancellationToken = default)
    {
        var machine = new PersistentStateMachine("LoanApproval", store, "Draft");
        
        // 注册所有状态
        ConfigureStates(machine);
        
        // 设置初始上下文
        machine.Context["applicantName"] = applicantName;
        machine.Context["amount"] = amount;
        machine.Context["interestRate"] = interestRate;
        machine.Context["applicationId"] = $"LOAN-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        
        // 初始化
        await machine.InitializeAsync(cancellationToken);
        
        return machine;
    }
    
    /// <summary>
    /// 加载现有贷款申请
    /// </summary>
    public static async Task<PersistentStateMachine?> LoadAsync(
        Guid instanceId,
        IStateStore store,
        CancellationToken cancellationToken = default)
    {
        return await PersistentStateMachine.LoadAsync(
            instanceId,
            store,
            ConfigureStates,
            cancellationToken
        );
    }
    
    /// <summary>
    /// 配置状态机的所有状态
    /// </summary>
    public static PersistentStateMachine ConfigureStates(PersistentStateMachine machine)
    {
        return machine.RegisterStates(
            new DraftState(),
            new SubmittedState(),
            new UnderReviewState(),
            new DocumentsPendingState(),
            new ManagerApprovalState(),
            new ReadyForUnderwritingState(),
            new UnderwritingState(),
            new ApprovedState(),
            new SignedState(),
            new DisbursedState(),
            new RejectedState(),
            new CancelledState(),
            new ExpiredState()
        );
    }
}
