namespace ComboSystemStateMachine;

/// <summary>
/// 输入按键
/// </summary>
[Flags]
public enum InputButton
{
    None = 0,
    LightPunch = 1,     // 轻拳 (LP)
    HeavyPunch = 2,     // 重拳 (HP)
    LightKick = 4,      // 轻脚 (LK)
    HeavyKick = 8,      // 重脚 (HK)
    Forward = 16,       // 前
    Back = 32,          // 后
    Up = 64,            // 上
    Down = 128,         // 下
    Special = 256       // 特殊键
}

/// <summary>
/// 角色状态
/// </summary>
public enum CharacterState
{
    Idle,           // 站立
    Walking,        // 行走
    Crouching,      // 蹲下
    Jumping,        // 跳跃
    Attacking,      // 攻击中
    HitStun,        // 受击硬直
    BlockStun,      // 格挡硬直
    Knockdown,      // 倒地
    GettingUp,      // 起身
    SpecialMove,    // 必杀技
    SuperMove       // 超必杀
}

/// <summary>
/// 攻击类型
/// </summary>
public enum AttackType
{
    Light,      // 轻攻击
    Medium,     // 中攻击
    Heavy,      // 重攻击
    Special,    // 必杀技
    Super       // 超必杀
}

/// <summary>
/// 攻击数据
/// </summary>
public class AttackData
{
    public string Name { get; set; } = "";
    public AttackType Type { get; set; }
    public int Damage { get; set; }
    public int StartupFrames { get; set; }    // 前摇帧数
    public int ActiveFrames { get; set; }     // 判定帧数
    public int RecoveryFrames { get; set; }   // 后摇帧数
    public int HitStun { get; set; }          // 受击硬直
    public int BlockStun { get; set; }        // 格挡硬直
    public int HitAdvantage { get; set; }     // 命中有利帧
    public bool Launcher { get; set; }        // 是否浮空技
    public bool GroundBounce { get; set; }    // 是否地面反弹
    public bool WallBounce { get; set; }      // 是否墙壁反弹
    public float MeterGain { get; set; }      // 获得气量
    public string Animation { get; set; } = "";
    
    public int TotalFrames => StartupFrames + ActiveFrames + RecoveryFrames;
    
    public override string ToString() =>
        $"{Name} (伤害:{Damage}, 帧数:{StartupFrames}/{ActiveFrames}/{RecoveryFrames})";
}

/// <summary>
/// 输入记录
/// </summary>
public record InputRecord(InputButton Button, int Frame, long Timestamp);

/// <summary>
/// 连招节点
/// </summary>
public class ComboNode
{
    public AttackData Attack { get; set; } = null!;
    public List<ComboLink> Links { get; set; } = new();
    public bool IsStarter { get; set; }       // 是否为起手式
    public bool IsEnder { get; set; }         // 是否为终结技
    public int CancelWindowStart { get; set; } // 取消窗口开始帧
    public int CancelWindowEnd { get; set; }   // 取消窗口结束帧
    
    public bool CanCancel(int currentFrame)
    {
        return currentFrame >= CancelWindowStart && currentFrame <= CancelWindowEnd;
    }
}

/// <summary>
/// 连招链接
/// </summary>
public class ComboLink
{
    public InputButton[] RequiredInput { get; set; } = Array.Empty<InputButton>();
    public ComboNode TargetNode { get; set; } = null!;
    public LinkType Type { get; set; }
    public int LinkWindow { get; set; } = 10; // 链接窗口帧数
    public bool RequiresMeter { get; set; }
    public float MeterCost { get; set; }
}

/// <summary>
/// 链接类型
/// </summary>
public enum LinkType
{
    Chain,      // 连锁（可在攻击中取消）
    Link,       // 链接（需要精确时机）
    Cancel,     // 取消（必杀技取消）
    SuperCancel // 超级取消
}
