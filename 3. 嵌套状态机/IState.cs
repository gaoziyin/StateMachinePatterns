namespace NestedStatePattern;

/// <summary>
/// 状态接口 - 支持嵌套的状态定义
/// </summary>
public interface IState
{
    /// <summary>
    /// 处理请求
    /// </summary>
    void Handle(StateContext context);
    
    /// <summary>
    /// 进入状态
    /// </summary>
    void OnEnter(StateContext context);
    
    /// <summary>
    /// 退出状态
    /// </summary>
    void OnExit(StateContext context);
    
    /// <summary>
    /// 获取状态名称
    /// </summary>
    string GetStateName();
    
    /// <summary>
    /// 是否为复合状态（包含子状态）
    /// </summary>
    bool IsComposite { get; }
    
    /// <summary>
    /// 获取当前活动的子状态（如果是复合状态）
    /// </summary>
    IState? GetActiveSubState();
    
    /// <summary>
    /// 获取完整的状态路径
    /// </summary>
    string GetFullStatePath();
}
