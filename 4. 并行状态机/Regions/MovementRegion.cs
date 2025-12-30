namespace ParallelStatePattern.Regions;

/// <summary>
/// 移动状态枚举
/// </summary>
public enum MovementState
{
    Idle,       // 站立
    Walking,    // 行走
    Running,    // 奔跑
    Jumping,    // 跳跃中
    Falling,    // 下落中
    Dead        // 死亡
}

/// <summary>
/// 移动状态区域 - 管理角色的移动状态
/// </summary>
public class MovementRegion : StateRegionBase<MovementState>
{
    private float _jumpTimer = 0f;
    private const float JumpDuration = 0.5f;
    
    public float CurrentSpeed { get; private set; } = 0f;
    
    public MovementRegion() : base("移动", MovementState.Idle) { }
    
    public override void Update(GameCharacter character, float deltaTime)
    {
        switch (_currentState)
        {
            case MovementState.Idle:
                CurrentSpeed = 0f;
                break;
                
            case MovementState.Walking:
                CurrentSpeed = 5f * character.SpeedModifier;
                break;
                
            case MovementState.Running:
                CurrentSpeed = 10f * character.SpeedModifier;
                break;
                
            case MovementState.Jumping:
                _jumpTimer += deltaTime;
                if (_jumpTimer >= JumpDuration)
                {
                    TransitionTo(MovementState.Falling, character);
                }
                break;
                
            case MovementState.Falling:
                _jumpTimer += deltaTime;
                if (_jumpTimer >= JumpDuration * 2)
                {
                    TransitionTo(MovementState.Idle, character);
                    _jumpTimer = 0f;
                }
                break;
        }
    }
    
    public override bool HandleEvent(GameCharacter character, GameEvent gameEvent)
    {
        if (_currentState == MovementState.Dead && gameEvent.Type != GameEventType.Revive)
            return false;
            
        switch (gameEvent.Type)
        {
            case GameEventType.StartMove:
                if (_currentState == MovementState.Idle)
                {
                    TransitionTo(MovementState.Walking, character);
                    return true;
                }
                break;
                
            case GameEventType.StopMove:
                if (_currentState is MovementState.Walking or MovementState.Running)
                {
                    TransitionTo(MovementState.Idle, character);
                    return true;
                }
                break;
                
            case GameEventType.StartRun:
                if (_currentState is MovementState.Idle or MovementState.Walking)
                {
                    TransitionTo(MovementState.Running, character);
                    return true;
                }
                break;
                
            case GameEventType.Jump:
                if (_currentState is MovementState.Idle or MovementState.Walking or MovementState.Running)
                {
                    _jumpTimer = 0f;
                    TransitionTo(MovementState.Jumping, character);
                    return true;
                }
                break;
                
            case GameEventType.Die:
                TransitionTo(MovementState.Dead, character);
                return true;
                
            case GameEventType.Revive:
                if (_currentState == MovementState.Dead)
                {
                    TransitionTo(MovementState.Idle, character);
                    return true;
                }
                break;
        }
        
        return false;
    }
    
    protected override void OnEnter(MovementState state, GameCharacter character)
    {
        Console.WriteLine($"    → 进入移动状态: {state}");
    }
}
