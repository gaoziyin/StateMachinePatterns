namespace ParallelStatePattern.Regions;

/// <summary>
/// 动作状态枚举
/// </summary>
public enum ActionState
{
    Idle,       // 空闲
    Attacking,  // 攻击中
    Blocking,   // 防御中
    Casting,    // 施法中
    Stunned,    // 眩晕中
    Dead        // 死亡
}

/// <summary>
/// 动作状态区域 - 管理角色的战斗动作
/// </summary>
public class ActionRegion : StateRegionBase<ActionState>
{
    private float _actionTimer = 0f;
    private float _currentActionDuration = 0f;
    
    public bool CanMove => _currentState is ActionState.Idle or ActionState.Blocking;
    public bool IsActing => _currentState is ActionState.Attacking or ActionState.Casting;
    
    public ActionRegion() : base("动作", ActionState.Idle) { }
    
    public override void Update(GameCharacter character, float deltaTime)
    {
        switch (_currentState)
        {
            case ActionState.Attacking:
            case ActionState.Casting:
            case ActionState.Stunned:
                _actionTimer += deltaTime;
                if (_actionTimer >= _currentActionDuration)
                {
                    TransitionTo(ActionState.Idle, character);
                }
                break;
                
            case ActionState.Blocking:
                // 持续防御，降低移动速度
                character.SpeedModifier = 0.3f;
                break;
        }
    }
    
    public override bool HandleEvent(GameCharacter character, GameEvent gameEvent)
    {
        if (_currentState == ActionState.Dead && gameEvent.Type != GameEventType.Revive)
            return false;
            
        // 眩晕中无法执行动作
        if (_currentState == ActionState.Stunned && 
            gameEvent.Type is not (GameEventType.Die or GameEventType.Revive))
            return false;
            
        switch (gameEvent.Type)
        {
            case GameEventType.Attack:
                if (_currentState == ActionState.Idle)
                {
                    StartAction(ActionState.Attacking, 0.8f, character);
                    Console.WriteLine($"    ⚔️ 发动攻击! 伤害: {character.AttackPower}");
                    return true;
                }
                break;
                
            case GameEventType.Block:
                if (_currentState == ActionState.Idle)
                {
                    TransitionTo(ActionState.Blocking, character);
                    Console.WriteLine($"    🛡️ 开始防御");
                    return true;
                }
                break;
                
            case GameEventType.CastSpell:
                if (_currentState == ActionState.Idle)
                {
                    StartAction(ActionState.Casting, 1.5f, character);
                    Console.WriteLine($"    ✨ 开始施法...");
                    return true;
                }
                break;
                
            case GameEventType.ActionComplete:
            case GameEventType.StopMove:
                if (_currentState == ActionState.Blocking)
                {
                    character.SpeedModifier = 1.0f;
                    TransitionTo(ActionState.Idle, character);
                    return true;
                }
                break;
                
            case GameEventType.TakeDamage:
                if (_currentState != ActionState.Blocking)
                {
                    var damage = gameEvent.Data as int? ?? 10;
                    character.TakeDamage(damage);
                    
                    // 20%几率被眩晕
                    if (Random.Shared.Next(100) < 20)
                    {
                        StartAction(ActionState.Stunned, 1.0f, character);
                        Console.WriteLine($"    💫 被眩晕了!");
                    }
                    return true;
                }
                else
                {
                    var damage = gameEvent.Data as int? ?? 10;
                    var reducedDamage = damage / 3;
                    character.TakeDamage(reducedDamage);
                    Console.WriteLine($"    🛡️ 防御成功! 伤害减免: {damage} → {reducedDamage}");
                    return true;
                }
                
            case GameEventType.Die:
                TransitionTo(ActionState.Dead, character);
                return true;
                
            case GameEventType.Revive:
                if (_currentState == ActionState.Dead)
                {
                    TransitionTo(ActionState.Idle, character);
                    return true;
                }
                break;
        }
        
        return false;
    }
    
    private void StartAction(ActionState state, float duration, GameCharacter character)
    {
        _actionTimer = 0f;
        _currentActionDuration = duration;
        TransitionTo(state, character);
    }
    
    protected override void OnEnter(ActionState state, GameCharacter character)
    {
        Console.WriteLine($"    → 进入动作状态: {state}");
    }
    
    protected override void OnExit(ActionState state, GameCharacter character)
    {
        if (state == ActionState.Blocking)
        {
            character.SpeedModifier = 1.0f;
        }
    }
}
