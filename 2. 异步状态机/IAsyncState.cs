namespace AsyncStatePattern;

/// <summary>
/// 异步状态接口 - 定义所有具体异步状态必须实现的操作
/// </summary>
public interface IAsyncState
{
    /// <summary>
    /// 异步处理请求并可能改变状态
    /// </summary>
    /// <param name="context">上下文对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task HandleAsync(AsyncContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 进入状态时的异步回调
    /// </summary>
    Task OnEnterAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 退出状态时的异步回调
    /// </summary>
    Task OnExitAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取当前状态名称
    /// </summary>
    string GetStateName();
}
