namespace StealthAIStateMachine;

/// <summary>
/// 2D向量
/// </summary>
public struct Vector2
{
    public float X { get; set; }
    public float Y { get; set; }
    
    public Vector2(float x, float y) { X = x; Y = y; }
    
    public static Vector2 Zero => new(0, 0);
    
    public float Length => MathF.Sqrt(X * X + Y * Y);
    
    public Vector2 Normalized
    {
        get
        {
            var len = Length;
            return len > 0 ? new Vector2(X / len, Y / len) : Zero;
        }
    }
    
    public static float Distance(Vector2 a, Vector2 b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
    
    public static float Angle(Vector2 from, Vector2 to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        return MathF.Atan2(dy, dx) * 180f / MathF.PI;
    }
    
    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator *(Vector2 a, float s) => new(a.X * s, a.Y * s);
    
    public override string ToString() => $"({X:F1}, {Y:F1})";
}

/// <summary>
/// 警戒等级
/// </summary>
public enum AlertLevel
{
    Unaware,        // 无察觉 (绿色)
    Suspicious,     // 怀疑中 (黄色)
    Alerted,        // 警觉 (橙色)
    Combat          // 战斗 (红色)
}

/// <summary>
/// AI状态
/// </summary>
public enum AIState
{
    Idle,           // 空闲
    Patrolling,     // 巡逻
    Investigating,  // 调查
    Chasing,        // 追击
    Attacking,      // 攻击
    Searching,      // 搜索
    Returning,      // 返回
    Alerting,       // 呼叫增援
    TakingCover,    // 寻找掩护
    Dead            // 死亡
}

/// <summary>
/// 感知类型
/// </summary>
public enum PerceptionType
{
    None,
    Visual,         // 视觉
    Sound,          // 声音
    Touch,          // 接触
    BodyFound,      // 发现尸体
    AlarmTriggered  // 警报触发
}

/// <summary>
/// 感知事件
/// </summary>
public record PerceptionEvent(
    PerceptionType Type,
    Vector2 Position,
    float Intensity,    // 0-1
    DateTime Timestamp
);

/// <summary>
/// 巡逻点
/// </summary>
public class PatrolPoint
{
    public Vector2 Position { get; set; }
    public float WaitTime { get; set; } = 2f;
    public float LookAngle { get; set; } = 0f;
}
