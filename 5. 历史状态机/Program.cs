using HistoryStatePattern;
using HistoryStatePattern.States;

/// <summary>
/// 历史状态机演示程序
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           历史状态机演示 - 支持撤销/重做              ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════╝");
        
        // 演示1: 表单向导
        DemoFormWizard();
        
        // 演示2: 文档编辑器
        DemoDocumentEditor();
    }
    
    static void DemoFormWizard()
    {
        Console.WriteLine("\n\n========== 演示1: 表单向导 (带暂停/恢复) ==========\n");
        
        var context = new StateMachineContext(new FormWizardState());
        
        context.PrintStatus();
        
        // 完成步骤1
        Console.WriteLine("\n--- 填写步骤1 ---");
        context.Handle();
        context.PrintStatus();
        
        // 完成步骤2
        Console.WriteLine("\n--- 填写步骤2 ---");
        context.Handle();
        context.PrintStatus();
        
        // 暂停 - 模拟用户离开
        Console.WriteLine("\n--- 用户暂时离开，暂停状态机 ---");
        context.Pause();
        context.PrintStatus();
        
        // 尝试操作（会被阻止）
        Console.WriteLine("\n--- 尝试在暂停时操作 ---");
        context.Handle();
        
        // 恢复
        Console.WriteLine("\n--- 用户返回，恢复状态机 ---");
        context.Resume();
        context.PrintStatus();
        
        // 继续完成步骤3
        Console.WriteLine("\n--- 填写步骤3 ---");
        context.Handle();
        
        // 撤销回到步骤3
        Console.WriteLine("\n--- 撤销：回到上一步 ---");
        context.Undo();
        context.PrintStatus();
        
        // 再撤销
        Console.WriteLine("\n--- 再次撤销 ---");
        context.Undo();
        context.PrintStatus();
        
        // 重做
        Console.WriteLine("\n--- 重做：恢复撤销的操作 ---");
        context.Redo();
        context.PrintStatus();
        
        // 打印历史
        context.PrintHistory();
        
        // 完成剩余步骤
        Console.WriteLine("\n--- 完成剩余步骤 ---");
        context.Handle(); // 步骤3
        context.Handle(); // 步骤4 - 提交
        
        context.PrintStatus();
    }
    
    static void DemoDocumentEditor()
    {
        Console.WriteLine("\n\n========== 演示2: 文档编辑器 (撤销/重做) ==========\n");
        
        var editor = new DocumentEditorState();
        var context = new StateMachineContext(editor);
        
        context.PrintStatus();
        
        // 输入文本
        Console.WriteLine("\n--- 输入 'Hello' ---");
        ((DocumentEditorState)context.CurrentState).InsertText("Hello", context);
        context.Handle();
        
        Console.WriteLine("\n--- 输入 ' World' ---");
        ((DocumentEditorState)context.CurrentState).InsertText(" World", context);
        context.Handle();
        
        Console.WriteLine("\n--- 输入 '!' ---");
        ((DocumentEditorState)context.CurrentState).InsertText("!", context);
        context.Handle();
        
        context.PrintStatus();
        context.PrintHistory();
        
        // 撤销
        Console.WriteLine("\n--- 撤销 (移除 '!') ---");
        context.Undo();
        context.Handle();
        
        Console.WriteLine("\n--- 撤销 (移除 ' World') ---");
        context.Undo();
        context.Handle();
        
        context.PrintStatus();
        
        // 重做
        Console.WriteLine("\n--- 重做 (恢复 ' World') ---");
        context.Redo();
        context.Handle();
        
        // 输入新内容（清除重做栈）
        Console.WriteLine("\n--- 输入 ' C#' (清除重做历史) ---");
        ((DocumentEditorState)context.CurrentState).InsertText(" C#", context);
        context.Handle();
        
        // 尝试重做（应该无效）
        Console.WriteLine("\n--- 尝试重做 ---");
        context.Redo();
        
        context.PrintStatus();
        context.PrintHistory();
        
        Console.WriteLine("\n╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    演示结束                            ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════╝");
    }
}
