namespace ParallelStatePattern;

/// <summary>
/// 状态区域接口 - 每个区域是一个独立的状态机
/// </summary>
public interface IStateRegion
{
    /// <summary>
    /// 区域名称
    /// </summary>
    string RegionName { get; }
    
    /// <summary>
    /// 当前状态名称
    /// </summary>
    string CurrentStateName { get; }
    
    /// <summary>
    /// 处理更新
    /// </summary>
    void Update(GameCharacter character, float deltaTime);
    
    /// <summary>
    /// 处理事件
    /// </summary>
    bool HandleEvent(GameCharacter character, GameEvent gameEvent);
    
    /// <summary>
    /// 重置到初始状态
    /// </summary>
    void Reset();
}

/// <summary>
/// 游戏事件类型
/// </summary>
public enum GameEventType
{
    // 移动相关
    StartMove,
    StopMove,
    StartRun,
    Jump,
    Land,
    
    // 动作相关
    Attack,
    Block,
    CastSpell,
    ActionComplete,
    
    // Buff相关
    ApplySpeed,
    ApplyPoison,
    ApplySlow,
    RemoveBuff,
    
    // 通用
    TakeDamage,
    Die,
    Revive
}

/// <summary>
/// 游戏事件
/// </summary>
public record GameEvent(GameEventType Type, object? Data = null);
