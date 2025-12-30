namespace ParallelStatePattern.Regions;

/// <summary>
/// Buff状态枚举（使用Flags支持多个Buff叠加）
/// </summary>
[Flags]
public enum BuffState
{
    None = 0,
    SpeedUp = 1,      // 加速
    SlowDown = 2,     // 减速
    Poisoned = 4,     // 中毒
    Regenerating = 8, // 回血
    Invincible = 16   // 无敌
}

/// <summary>
/// Buff状态区域 - 管理角色的增益/减益效果
/// </summary>
public class BuffRegion : IStateRegion
{
    private BuffState _activeBuffs = BuffState.None;
    private readonly Dictionary<BuffState, float> _buffTimers = new();
    private readonly Dictionary<BuffState, float> _buffDurations = new();
    
    public string RegionName => "Buff";
    public string CurrentStateName => _activeBuffs == BuffState.None ? "无" : _activeBuffs.ToString();
    public BuffState ActiveBuffs => _activeBuffs;
    
    public void Update(GameCharacter character, float deltaTime)
    {
        var buffsToRemove = new List<BuffState>();
        
        foreach (var buff in _buffTimers.Keys.ToList())
        {
            _buffTimers[buff] -= deltaTime;
            
            // 应用Buff效果
            ApplyBuffEffect(buff, character, deltaTime);
            
            if (_buffTimers[buff] <= 0)
            {
                buffsToRemove.Add(buff);
            }
        }
        
        foreach (var buff in buffsToRemove)
        {
            RemoveBuff(buff, character);
        }
    }
    
    public bool HandleEvent(GameCharacter character, GameEvent gameEvent)
    {
        switch (gameEvent.Type)
        {
            case GameEventType.ApplySpeed:
                AddBuff(BuffState.SpeedUp, 5f, character);
                character.SpeedModifier *= 1.5f;
                Console.WriteLine($"    ⚡ 获得加速效果! 持续5秒");
                return true;
                
            case GameEventType.ApplySlow:
                AddBuff(BuffState.SlowDown, 3f, character);
                character.SpeedModifier *= 0.5f;
                Console.WriteLine($"    🐌 被减速了! 持续3秒");
                return true;
                
            case GameEventType.ApplyPoison:
                AddBuff(BuffState.Poisoned, 4f, character);
                Console.WriteLine($"    ☠️ 中毒了! 持续4秒");
                return true;
                
            case GameEventType.RemoveBuff:
                if (gameEvent.Data is BuffState buffToRemove)
                {
                    RemoveBuff(buffToRemove, character);
                    return true;
                }
                break;
                
            case GameEventType.Die:
                // 死亡时清除所有Buff
                ClearAllBuffs(character);
                return true;
                
            case GameEventType.Revive:
                // 复活时获得短暂无敌
                AddBuff(BuffState.Invincible, 3f, character);
                AddBuff(BuffState.Regenerating, 5f, character);
                Console.WriteLine($"    ✨ 复活! 获得3秒无敌和5秒回血");
                return true;
        }
        
        return false;
    }
    
    private void AddBuff(BuffState buff, float duration, GameCharacter character)
    {
        _activeBuffs |= buff;
        _buffTimers[buff] = duration;
        _buffDurations[buff] = duration;
        Console.WriteLine($"  [Buff] 添加: {buff} ({duration}秒)");
    }
    
    private void RemoveBuff(BuffState buff, GameCharacter character)
    {
        if (_activeBuffs.HasFlag(buff))
        {
            _activeBuffs &= ~buff;
            _buffTimers.Remove(buff);
            _buffDurations.Remove(buff);
            
            // 移除Buff效果
            RemoveBuffEffect(buff, character);
            Console.WriteLine($"  [Buff] 移除: {buff}");
        }
    }
    
    private void ClearAllBuffs(GameCharacter character)
    {
        foreach (var buff in _buffTimers.Keys.ToList())
        {
            RemoveBuffEffect(buff, character);
        }
        _activeBuffs = BuffState.None;
        _buffTimers.Clear();
        _buffDurations.Clear();
        Console.WriteLine($"  [Buff] 清除所有Buff");
    }
    
    private void ApplyBuffEffect(BuffState buff, GameCharacter character, float deltaTime)
    {
        switch (buff)
        {
            case BuffState.Poisoned:
                // 每秒扣血
                character.TakeDamage((int)(5 * deltaTime), silent: true);
                break;
                
            case BuffState.Regenerating:
                // 每秒回血
                character.Heal((int)(10 * deltaTime));
                break;
        }
    }
    
    private void RemoveBuffEffect(BuffState buff, GameCharacter character)
    {
        switch (buff)
        {
            case BuffState.SpeedUp:
                character.SpeedModifier /= 1.5f;
                break;
            case BuffState.SlowDown:
                character.SpeedModifier /= 0.5f;
                break;
        }
    }
    
    public bool HasBuff(BuffState buff) => _activeBuffs.HasFlag(buff);
    
    public float GetBuffRemainingTime(BuffState buff)
    {
        return _buffTimers.TryGetValue(buff, out var time) ? time : 0f;
    }
    
    public void Reset()
    {
        _activeBuffs = BuffState.None;
        _buffTimers.Clear();
        _buffDurations.Clear();
    }
}
