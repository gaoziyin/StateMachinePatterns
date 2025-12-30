namespace StealthAIStateMachine;

/// <summary>
/// 感知系统 - 处理视觉和听觉检测
/// </summary>
public class PerceptionSystem
{
    private readonly GuardAI _owner;
    
    // 视觉参数
    public float ViewDistance { get; set; } = 15f;
    public float ViewAngle { get; set; } = 90f; // 视野锥角度
    public float PeripheralAngle { get; set; } = 150f; // 余光角度
    public float PeripheralMultiplier { get; set; } = 0.3f; // 余光检测力度
    
    // 听觉参数
    public float HearingRange { get; set; } = 10f;
    public float HearingMultiplier { get; set; } = 1f;
    
    // 检测累积
    private float _visualDetection = 0f;
    private float _audioDetection = 0f;
    
    public float VisualDetection => _visualDetection;
    public float AudioDetection => _audioDetection;
    public float TotalDetection => Math.Min(1f, _visualDetection + _audioDetection);
    
    public PerceptionSystem(GuardAI owner)
    {
        _owner = owner;
    }
    
    /// <summary>
    /// 检测目标
    /// </summary>
    public PerceptionEvent? DetectTarget(Vector2 targetPos, bool targetIsMoving, bool targetInLight, bool targetCrouching)
    {
        var distance = Vector2.Distance(_owner.Position, targetPos);
        var angle = Math.Abs(Vector2.Angle(_owner.Position, targetPos) - _owner.FacingAngle);
        if (angle > 180) angle = 360 - angle;
        
        // 视觉检测
        var visualEvent = CheckVisualDetection(targetPos, distance, angle, targetIsMoving, targetInLight, targetCrouching);
        
        // 听觉检测
        var audioEvent = CheckAudioDetection(targetPos, distance, targetIsMoving, targetCrouching);
        
        // 返回更强的感知
        if (visualEvent != null && audioEvent != null)
        {
            return visualEvent.Intensity > audioEvent.Intensity ? visualEvent : audioEvent;
        }
        
        return visualEvent ?? audioEvent;
    }
    
    private PerceptionEvent? CheckVisualDetection(Vector2 targetPos, float distance, float angle, bool isMoving, bool inLight, bool isCrouching)
    {
        if (distance > ViewDistance) return null;
        
        float intensity = 0f;
        
        // 检查是否在视野内
        if (angle <= ViewAngle / 2)
        {
            // 正面视野
            intensity = 1f - (distance / ViewDistance);
        }
        else if (angle <= PeripheralAngle / 2)
        {
            // 余光区域
            intensity = (1f - (distance / ViewDistance)) * PeripheralMultiplier;
        }
        else
        {
            return null; // 完全看不见
        }
        
        // 修正因素
        if (isMoving) intensity *= 1.5f;
        if (inLight) intensity *= 1.3f;
        if (isCrouching) intensity *= 0.5f;
        
        // 距离修正（越近越容易发现）
        if (distance < 3f) intensity *= 2f;
        
        intensity = Math.Clamp(intensity, 0f, 1f);
        
        if (intensity > 0.05f)
        {
            _visualDetection = Math.Min(1f, _visualDetection + intensity * 0.1f);
            return new PerceptionEvent(PerceptionType.Visual, targetPos, intensity, DateTime.Now);
        }
        
        return null;
    }
    
    private PerceptionEvent? CheckAudioDetection(Vector2 targetPos, float distance, bool isMoving, bool isCrouching)
    {
        if (!isMoving) return null;
        if (distance > HearingRange) return null;
        
        float intensity = (1f - (distance / HearingRange)) * HearingMultiplier;
        
        if (isCrouching) intensity *= 0.3f; // 蹲走很安静
        
        intensity = Math.Clamp(intensity, 0f, 1f);
        
        if (intensity > 0.1f)
        {
            _audioDetection = Math.Min(1f, _audioDetection + intensity * 0.05f);
            return new PerceptionEvent(PerceptionType.Sound, targetPos, intensity, DateTime.Now);
        }
        
        return null;
    }
    
    /// <summary>
    /// 检测点是否在视野内
    /// </summary>
    public bool IsInLineOfSight(Vector2 targetPos)
    {
        var distance = Vector2.Distance(_owner.Position, targetPos);
        var angle = Math.Abs(Vector2.Angle(_owner.Position, targetPos) - _owner.FacingAngle);
        if (angle > 180) angle = 360 - angle;
        
        return distance <= ViewDistance && angle <= ViewAngle / 2;
    }
    
    /// <summary>
    /// 衰减检测值
    /// </summary>
    public void DecayDetection(float deltaTime)
    {
        _visualDetection = Math.Max(0, _visualDetection - 0.05f * deltaTime);
        _audioDetection = Math.Max(0, _audioDetection - 0.1f * deltaTime);
    }
    
    /// <summary>
    /// 重置检测
    /// </summary>
    public void ResetDetection()
    {
        _visualDetection = 0;
        _audioDetection = 0;
    }
}

/// <summary>
/// 记忆系统 - 记住玩家最后位置和可疑事件
/// </summary>
public class MemorySystem
{
    private readonly List<MemoryEntry> _memories = new();
    private readonly float _memoryDuration = 30f; // 记忆持续秒数
    
    public Vector2? LastKnownPlayerPosition { get; private set; }
    public DateTime? LastSeenTime { get; private set; }
    public IReadOnlyList<MemoryEntry> Memories => _memories;
    
    /// <summary>
    /// 记录玩家位置
    /// </summary>
    public void RememberPlayerPosition(Vector2 position)
    {
        LastKnownPlayerPosition = position;
        LastSeenTime = DateTime.Now;
        
        AddMemory(new MemoryEntry
        {
            Type = MemoryType.PlayerSighting,
            Position = position,
            Timestamp = DateTime.Now
        });
    }
    
    /// <summary>
    /// 记录可疑事件
    /// </summary>
    public void RememberSuspiciousEvent(Vector2 position, string description)
    {
        AddMemory(new MemoryEntry
        {
            Type = MemoryType.SuspiciousActivity,
            Position = position,
            Description = description,
            Timestamp = DateTime.Now
        });
    }
    
    /// <summary>
    /// 记录发现的尸体
    /// </summary>
    public void RememberBodyFound(Vector2 position)
    {
        AddMemory(new MemoryEntry
        {
            Type = MemoryType.BodyFound,
            Position = position,
            Timestamp = DateTime.Now
        });
    }
    
    private void AddMemory(MemoryEntry entry)
    {
        _memories.Add(entry);
        
        // 限制记忆数量
        if (_memories.Count > 20)
        {
            _memories.RemoveAt(0);
        }
    }
    
    /// <summary>
    /// 清理过期记忆
    /// </summary>
    public void CleanupOldMemories()
    {
        var cutoff = DateTime.Now.AddSeconds(-_memoryDuration);
        _memories.RemoveAll(m => m.Timestamp < cutoff);
        
        if (LastSeenTime.HasValue && LastSeenTime.Value < cutoff)
        {
            LastKnownPlayerPosition = null;
            LastSeenTime = null;
        }
    }
    
    /// <summary>
    /// 获取最近的搜索点
    /// </summary>
    public Vector2? GetSearchPoint()
    {
        var recentMemories = _memories
            .Where(m => m.Type != MemoryType.BodyFound)
            .OrderByDescending(m => m.Timestamp)
            .Take(3)
            .ToList();
        
        if (recentMemories.Count == 0) return null;
        
        // 返回最近记忆的位置附近随机点
        var memory = recentMemories[new Random().Next(recentMemories.Count)];
        var offset = new Vector2(
            (float)(new Random().NextDouble() - 0.5) * 5,
            (float)(new Random().NextDouble() - 0.5) * 5
        );
        return memory.Position + offset;
    }
}

public class MemoryEntry
{
    public MemoryType Type { get; set; }
    public Vector2 Position { get; set; }
    public string? Description { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum MemoryType
{
    PlayerSighting,
    SuspiciousActivity,
    BodyFound,
    AlarmHeard
}
