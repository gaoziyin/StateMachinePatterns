namespace ComboSystemStateMachine;

/// <summary>
/// 输入缓冲系统
/// </summary>
public class InputBuffer
{
    private readonly Queue<InputRecord> _buffer = new();
    private readonly int _bufferSize;
    private readonly int _bufferWindow; // 缓冲窗口（帧）
    private int _currentFrame;
    
    public InputBuffer(int bufferSize = 60, int bufferWindow = 8)
    {
        _bufferSize = bufferSize;
        _bufferWindow = bufferWindow;
    }
    
    /// <summary>
    /// 添加输入
    /// </summary>
    public void AddInput(InputButton button)
    {
        var record = new InputRecord(button, _currentFrame, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        _buffer.Enqueue(record);
        
        // 保持缓冲区大小
        while (_buffer.Count > _bufferSize)
        {
            _buffer.Dequeue();
        }
    }
    
    /// <summary>
    /// 更新帧数
    /// </summary>
    public void Update()
    {
        _currentFrame++;
        
        // 清除过期输入
        var expireFrame = _currentFrame - _bufferWindow * 10;
        while (_buffer.Count > 0 && _buffer.Peek().Frame < expireFrame)
        {
            _buffer.Dequeue();
        }
    }
    
    /// <summary>
    /// 检查输入序列
    /// </summary>
    public bool CheckSequence(InputButton[] sequence, int windowFrames = 30)
    {
        if (sequence.Length == 0) return false;
        
        var inputs = _buffer.ToArray().Reverse().ToArray();
        var seqIndex = sequence.Length - 1;
        var startFrame = _currentFrame;
        
        foreach (var input in inputs)
        {
            if (startFrame - input.Frame > windowFrames) break;
            
            if (input.Button.HasFlag(sequence[seqIndex]))
            {
                seqIndex--;
                if (seqIndex < 0) return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查最近是否有指定输入
    /// </summary>
    public bool HasRecentInput(InputButton button, int windowFrames = 8)
    {
        var inputs = _buffer.ToArray().Reverse().ToArray();
        
        foreach (var input in inputs)
        {
            if (_currentFrame - input.Frame > windowFrames) break;
            if (input.Button.HasFlag(button)) return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 获取最近输入
    /// </summary>
    public InputRecord? GetLatestInput()
    {
        return _buffer.Count > 0 ? _buffer.Last() : null;
    }
    
    /// <summary>
    /// 清空缓冲区
    /// </summary>
    public void Clear()
    {
        _buffer.Clear();
    }
    
    public int CurrentFrame => _currentFrame;
}

/// <summary>
/// 指令检测器 - 检测特殊输入指令
/// </summary>
public class MotionDetector
{
    private readonly InputBuffer _buffer;
    
    // 预定义指令
    public static readonly InputButton[] QuarterCircleForward = 
        { InputButton.Down, InputButton.Down | InputButton.Forward, InputButton.Forward };
    
    public static readonly InputButton[] QuarterCircleBack = 
        { InputButton.Down, InputButton.Down | InputButton.Back, InputButton.Back };
    
    public static readonly InputButton[] DragonPunch = 
        { InputButton.Forward, InputButton.Down, InputButton.Down | InputButton.Forward };
    
    public static readonly InputButton[] HalfCircleForward = 
        { InputButton.Back, InputButton.Down | InputButton.Back, InputButton.Down, 
          InputButton.Down | InputButton.Forward, InputButton.Forward };
    
    public static readonly InputButton[] ChargeBack = 
        { InputButton.Back, InputButton.Back, InputButton.Forward };
    
    public static readonly InputButton[] DoubleTap =
        { InputButton.Forward, InputButton.Forward };
    
    public MotionDetector(InputBuffer buffer)
    {
        _buffer = buffer;
    }
    
    /// <summary>
    /// 检测波动拳指令 (↓↘→ + P)
    /// </summary>
    public bool DetectHadouken(InputButton attackButton)
    {
        return _buffer.CheckSequence(QuarterCircleForward.Append(attackButton).ToArray());
    }
    
    /// <summary>
    /// 检测升龙拳指令 (→↓↘ + P)
    /// </summary>
    public bool DetectShoryuken(InputButton attackButton)
    {
        return _buffer.CheckSequence(DragonPunch.Append(attackButton).ToArray());
    }
    
    /// <summary>
    /// 检测旋风腿指令 (↓↙← + K)
    /// </summary>
    public bool DetectTatsumaki(InputButton attackButton)
    {
        return _buffer.CheckSequence(QuarterCircleBack.Append(attackButton).ToArray());
    }
}
