using PersistentStatePattern.Core;
using PersistentStatePattern.Storage;
using PersistentStatePattern.Examples.LoanApproval;

/// <summary>
/// 持久化状态机演示程序
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         持久化状态机演示 - 企业级贷款审批系统                 ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        
        // 演示1: 使用内存存储的完整流程
        await DemoInMemoryStoreAsync();
        
        // 演示2: 使用SQLite存储 + 检查点恢复
        await DemoSqliteStoreAsync();
        
        // 演示3: 从数据库恢复状态机
        await DemoRestoreFromDatabaseAsync();
        
        Console.WriteLine("\n\n所有演示完成！");
    }
    
    static async Task DemoInMemoryStoreAsync()
    {
        Console.WriteLine("\n\n========== 演示1: 内存存储 - 小额贷款审批流程 ==========\n");
        
        var store = new InMemoryStateStore();
        await store.InitializeAsync();
        
        // 创建贷款申请
        var loan = await LoanWorkflowFactory.CreateAsync(
            store,
            applicantName: "张三",
            amount: 50000m,
            interestRate: 0.065m
        );
        
        // 订阅事件
        loan.OnEvent += (sender, e) =>
        {
            if (e.EventType == StateMachineEventType.TransitionCompleted)
            {
                Console.WriteLine($"  [事件] {e.FromState} → {e.ToState}");
            }
        };
        
        loan.PrintStatus();
        
        // 执行完整流程
        Console.WriteLine("\n--- 提交申请 ---");
        await loan.FireAsync(LoanTriggers.Submit);
        
        Console.WriteLine("\n--- 开始审核 ---");
        await loan.FireAsync(LoanTriggers.StartReview);
        
        Console.WriteLine("\n--- 审核通过（小额贷款，直接进入核保）---");
        await loan.FireAsync(LoanTriggers.ApproveReview);
        
        Console.WriteLine("\n--- 开始核保 ---");
        await loan.FireAsync(LoanTriggers.StartUnderwriting);
        
        Console.WriteLine("\n--- 核保通过 ---");
        await loan.FireAsync(LoanTriggers.ApproveUnderwriting);
        
        Console.WriteLine("\n--- 签署合同 ---");
        await loan.FireAsync(LoanTriggers.Sign);
        
        Console.WriteLine("\n--- 放款 ---");
        await loan.FireAsync(LoanTriggers.Disburse);
        
        loan.PrintStatus();
        
        // 查看历史记录
        Console.WriteLine("\n--- 转换历史 ---");
        var history = await loan.GetHistoryAsync();
        foreach (var record in history)
        {
            Console.WriteLine($"  {record.Timestamp:HH:mm:ss} | {record.FromState,-20} → {record.ToState,-20} | {record.Trigger}");
        }
    }
    
    static async Task DemoSqliteStoreAsync()
    {
        Console.WriteLine("\n\n========== 演示2: SQLite存储 - 大额贷款 + 检查点 ==========\n");
        
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "loandb.sqlite");
        
        // 清理旧数据库
        if (File.Exists(dbPath))
            File.Delete(dbPath);
        
        await using var store = new SqliteStateStore(dbPath);
        await store.InitializeAsync();
        
        // 创建大额贷款申请
        var loan = await LoanWorkflowFactory.CreateAsync(
            store,
            applicantName: "李四",
            amount: 500000m,
            interestRate: 0.055m
        );
        
        var instanceId = loan.InstanceId;
        Console.WriteLine($"\n贷款申请ID: {instanceId}");
        
        loan.PrintStatus();
        
        // 提交并开始审核
        await loan.FireAsync(LoanTriggers.Submit);
        await loan.FireAsync(LoanTriggers.StartReview);
        
        // 创建检查点
        Console.WriteLine("\n--- 创建检查点（审核中）---");
        var checkpoint1 = await loan.CreateCheckpointAsync("审核开始前");
        
        // 请求补充材料
        Console.WriteLine("\n--- 请求补充材料 ---");
        await loan.FireAsync(LoanTriggers.RequestDocuments);
        loan.Context["documentsRequested"] = "收入证明, 房产证";
        
        // 提供材料
        Console.WriteLine("\n--- 提供材料 ---");
        await loan.FireAsync(LoanTriggers.ProvideDocuments);
        
        // 大额贷款需要经理审批
        Console.WriteLine("\n--- 审核通过（大额贷款，需要经理审批）---");
        await loan.FireAsync(LoanTriggers.ApproveReview);
        
        // 创建另一个检查点
        Console.WriteLine("\n--- 创建检查点（经理审批前）---");
        var checkpoint2 = await loan.CreateCheckpointAsync("经理审批前");
        
        loan.PrintStatus();
        
        // 模拟：需要回退到之前的检查点
        Console.WriteLine("\n--- 恢复到检查点1（重新审核）---");
        await loan.RestoreFromCheckpointAsync(checkpoint1);
        
        loan.PrintStatus();
        
        // 这次直接通过
        Console.WriteLine("\n--- 重新审核，直接通过 ---");
        await loan.FireAsync(LoanTriggers.ApproveReview);
        
        // 经理审批
        Console.WriteLine("\n--- 经理审批通过 ---");
        await loan.FireAsync(LoanTriggers.ApproveReview);
        
        // 完成剩余流程
        await loan.FireAsync(LoanTriggers.StartUnderwriting);
        await loan.FireAsync(LoanTriggers.ApproveUnderwriting);
        await loan.FireAsync(LoanTriggers.Sign);
        await loan.FireAsync(LoanTriggers.Disburse);
        
        loan.PrintStatus();
        
        // 查看所有检查点
        Console.WriteLine("\n--- 检查点列表 ---");
        var checkpoints = await loan.GetCheckpointsAsync();
        foreach (var cp in checkpoints)
        {
            Console.WriteLine($"  {cp.CreatedAt:HH:mm:ss} | {cp.CheckpointId:N} | 状态: {cp.State} | {cp.Description}");
        }
    }
    
    static async Task DemoRestoreFromDatabaseAsync()
    {
        Console.WriteLine("\n\n========== 演示3: 从数据库恢复状态机 ==========\n");
        
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "loandb.sqlite");
        
        await using var store = new SqliteStateStore(dbPath);
        await store.InitializeAsync();
        
        // 查询所有贷款申请
        Console.WriteLine("--- 查询数据库中的贷款申请 ---");
        var instances = await store.QueryInstancesAsync(machineType: "LoanApproval");
        
        foreach (var data in instances)
        {
            Console.WriteLine($"\n  实例: {data.InstanceId:N}");
            Console.WriteLine($"  状态: {data.CurrentState}");
            Console.WriteLine($"  版本: {data.Version}");
            Console.WriteLine($"  已完成: {data.IsCompleted}");
            Console.WriteLine($"  更新时间: {data.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
        }
        
        if (instances.Count > 0)
        {
            var instanceId = instances[0].InstanceId;
            Console.WriteLine($"\n--- 恢复实例: {instanceId:N} ---");
            
            var restored = await LoanWorkflowFactory.LoadAsync(instanceId, store);
            
            if (restored != null)
            {
                restored.PrintStatus();
                
                // 查看历史
                Console.WriteLine("\n--- 完整转换历史 ---");
                var history = await restored.GetHistoryAsync();
                Console.WriteLine($"\n  共 {history.Count} 条转换记录");
                foreach (var record in history)
                {
                    Console.WriteLine($"  {record.Timestamp:HH:mm:ss.fff} | {record.FromState,-20} → {record.ToState,-20} | {record.Trigger}");
                }
            }
        }
        
        Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    持久化状态机演示完成                         ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
    }
}
