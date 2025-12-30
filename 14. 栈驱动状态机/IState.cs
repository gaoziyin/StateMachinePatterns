namespace PushdownAutomaton;

/// <summary>
/// 状态接口 - 定义所有状态共享的生命周期方法
/// </summary>
public interface IState
{
    /// <summary>
    /// 当状态进入时调用
    /// </summary>
    void Enter();
    
    /// <summary>
    /// 每帧更新时调用，处理状态逻辑
    /// </summary>
    void Update(PushdownStateMachine sm);
    
    /// <summary>
    /// 当状态退出时调用
    /// </summary>
    void Exit();
}
