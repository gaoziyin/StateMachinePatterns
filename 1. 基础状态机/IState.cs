namespace StatePattern;

/// <summary>
/// 状态接口 - 定义所有具体状态必须实现的操作
/// </summary>
public interface IState
{
    /// <summary>
    /// 处理请求并可能改变状态
    /// </summary>
    void Handle(Context context);
    
    /// <summary>
    /// 获取当前状态名称
    /// </summary>
    string GetStateName();
}
