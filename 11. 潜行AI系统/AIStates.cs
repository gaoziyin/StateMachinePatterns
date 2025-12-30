namespace StealthAIStateMachine;

/// <summary>
/// AI状态接口
/// </summary>
public interface IAIState
{
    AIState State { get; }
    void Enter(GuardAI guard);
    void Update(GuardAI guard, float deltaTime);
    void Exit(GuardAI guard);
}

/// <summary>
/// 空闲状态
/// </summary>
public class IdleState : IAIState
{
    public AIState State => AIState.Idle;
    private float _idleTimer;
    
    public void Enter(GuardAI guard)
    {
        _idleTimer = 0;
        Console.WriteLine($"  [{guard.Name}] 进入空闲状态，原地警戒");
    }
    
    public void Update(GuardAI guard, float deltaTime)
    {
        _idleTimer += deltaTime;
        
        // 空闲一段时间后开始巡逻
        if (_idleTimer > 3f && guard.PatrolPoints.Count > 0)
        {
            guard.ChangeState(AIState.Patrolling);
        }
    }
    
    public void Exit(GuardAI guard) { }
}

/// <summary>
/// 巡逻状态
/// </summary>
public class PatrollingState : IAIState
{
    public AIState State => AIState.Patrolling;
    private int _currentPointIndex;
    private float _waitTimer;
    private bool _isWaiting;
    
    public void Enter(GuardAI guard)
    {
        _currentPointIndex = 0;
        _waitTimer = 0;
        _isWaiting = false;
        Console.WriteLine($"  [{guard.Name}] 开始巡逻路线");
    }
    
    public void Update(GuardAI guard, float deltaTime)
    {
        if (guard.PatrolPoints.Count == 0) return;
        
        var target = guard.PatrolPoints[_currentPointIndex];
        
        if (_isWaiting)
        {
            _waitTimer += deltaTime;
            guard.FacingAngle = target.LookAngle;
            
            if (_waitTimer >= target.WaitTime)
            {
                _isWaiting = false;
                _currentPointIndex = (_currentPointIndex + 1) % guard.PatrolPoints.Count;
            }
        }
        else
        {
            var distance = Vector2.Distance(guard.Position, target.Position);
            
            if (distance < 0.5f)
            {
                _isWaiting = true;
                _waitTimer = 0;
                guard.Position = target.Position;
            }
            else
            {
                // 移动向目标点
                var direction = (target.Position - guard.Position).Normalized;
                guard.Position = guard.Position + direction * guard.MoveSpeed * deltaTime;
                guard.FacingAngle = Vector2.Angle(guard.Position, target.Position);
            }
        }
    }
    
    public void Exit(GuardAI guard)
    {
        Console.WriteLine($"  [{guard.Name}] 中断巡逻");
    }
}

/// <summary>
/// 调查状态
/// </summary>
public class InvestigatingState : IAIState
{
    public AIState State => AIState.Investigating;
    private Vector2 _investigatePoint;
    private float _investigateTimer;
    private const float MaxInvestigateTime = 10f;
    
    public void Enter(GuardAI guard)
    {
        _investigatePoint = guard.Memory.LastKnownPlayerPosition ?? guard.Position;
        _investigateTimer = 0;
        Console.WriteLine($"  [{guard.Name}] 🔍 前往调查可疑地点 {_investigatePoint}");
    }
    
    public void Update(GuardAI guard, float deltaTime)
    {
        _investigateTimer += deltaTime;
        
        var distance = Vector2.Distance(guard.Position, _investigatePoint);
        
        if (distance < 1f)
        {
            // 到达调查点，四处查看
            guard.FacingAngle += 60 * deltaTime;
            
            if (_investigateTimer > 5f)
            {
                Console.WriteLine($"  [{guard.Name}] 没有发现异常...");
                guard.ChangeState(AIState.Returning);
            }
        }
        else
        {
            // 移动向调查点
            var direction = (_investigatePoint - guard.Position).Normalized;
            guard.Position = guard.Position + direction * guard.MoveSpeed * 0.8f * deltaTime;
            guard.FacingAngle = Vector2.Angle(guard.Position, _investigatePoint);
        }
        
        if (_investigateTimer > MaxInvestigateTime)
        {
            guard.ChangeState(AIState.Returning);
        }
    }
    
    public void Exit(GuardAI guard)
    {
        guard.AlertLevel = AlertLevel.Unaware;
        guard.Perception.ResetDetection();
    }
}

/// <summary>
/// 追击状态
/// </summary>
public class ChasingState : IAIState
{
    public AIState State => AIState.Chasing;
    private float _lostTimer;
    
    public void Enter(GuardAI guard)
    {
        _lostTimer = 0;
        guard.AlertLevel = AlertLevel.Combat;
        Console.WriteLine($"  [{guard.Name}] ⚔️ 发现入侵者！开始追击！");
    }
    
    public void Update(GuardAI guard, float deltaTime)
    {
        var targetPos = guard.Memory.LastKnownPlayerPosition;
        
        if (targetPos == null)
        {
            _lostTimer += deltaTime;
            if (_lostTimer > 5f)
            {
                Console.WriteLine($"  [{guard.Name}] 目标丢失，开始搜索...");
                guard.ChangeState(AIState.Searching);
            }
            return;
        }
        
        var distance = Vector2.Distance(guard.Position, targetPos.Value);
        
        if (distance < 2f)
        {
            // 进入攻击范围
            guard.ChangeState(AIState.Attacking);
        }
        else
        {
            // 追击目标
            var direction = (targetPos.Value - guard.Position).Normalized;
            guard.Position = guard.Position + direction * guard.RunSpeed * deltaTime;
            guard.FacingAngle = Vector2.Angle(guard.Position, targetPos.Value);
        }
    }
    
    public void Exit(GuardAI guard) { }
}

/// <summary>
/// 攻击状态
/// </summary>
public class AttackingState : IAIState
{
    public AIState State => AIState.Attacking;
    private float _attackCooldown;
    
    public void Enter(GuardAI guard)
    {
        _attackCooldown = 0;
        Console.WriteLine($"  [{guard.Name}] ⚔️ 进入攻击状态！");
    }
    
    public void Update(GuardAI guard, float deltaTime)
    {
        _attackCooldown -= deltaTime;
        
        if (_attackCooldown <= 0)
        {
            Console.WriteLine($"  [{guard.Name}] 💥 攻击！");
            _attackCooldown = 1.5f;
        }
        
        // 如果目标逃离，恢复追击
        var targetPos = guard.Memory.LastKnownPlayerPosition;
        if (targetPos != null)
        {
            var distance = Vector2.Distance(guard.Position, targetPos.Value);
            if (distance > 3f)
            {
                guard.ChangeState(AIState.Chasing);
            }
        }
    }
    
    public void Exit(GuardAI guard) { }
}

/// <summary>
/// 搜索状态
/// </summary>
public class SearchingState : IAIState
{
    public AIState State => AIState.Searching;
    private float _searchTimer;
    private Vector2? _searchPoint;
    private const float MaxSearchTime = 30f;
    
    public void Enter(GuardAI guard)
    {
        _searchTimer = 0;
        _searchPoint = guard.Memory.GetSearchPoint();
        guard.AlertLevel = AlertLevel.Alerted;
        Console.WriteLine($"  [{guard.Name}] 🔦 开始搜索区域...");
    }
    
    public void Update(GuardAI guard, float deltaTime)
    {
        _searchTimer += deltaTime;
        
        if (_searchPoint == null)
        {
            _searchPoint = guard.Memory.GetSearchPoint() ?? guard.Position + new Vector2(
                (float)(new Random().NextDouble() - 0.5) * 10,
                (float)(new Random().NextDouble() - 0.5) * 10
            );
        }
        
        var distance = Vector2.Distance(guard.Position, _searchPoint.Value);
        
        if (distance < 1f)
        {
            // 到达搜索点，寻找下一个
            guard.FacingAngle += 90 * deltaTime;
            _searchPoint = null;
        }
        else
        {
            var direction = (_searchPoint.Value - guard.Position).Normalized;
            guard.Position = guard.Position + direction * guard.MoveSpeed * 0.6f * deltaTime;
            guard.FacingAngle = Vector2.Angle(guard.Position, _searchPoint.Value);
        }
        
        if (_searchTimer > MaxSearchTime)
        {
            Console.WriteLine($"  [{guard.Name}] 搜索超时，返回岗位");
            guard.ChangeState(AIState.Returning);
        }
    }
    
    public void Exit(GuardAI guard)
    {
        guard.AlertLevel = AlertLevel.Suspicious;
    }
}

/// <summary>
/// 返回状态
/// </summary>
public class ReturningState : IAIState
{
    public AIState State => AIState.Returning;
    private Vector2 _homePosition;
    
    public void Enter(GuardAI guard)
    {
        _homePosition = guard.PatrolPoints.Count > 0 ? guard.PatrolPoints[0].Position : guard.Position;
        Console.WriteLine($"  [{guard.Name}] 返回岗位...");
    }
    
    public void Update(GuardAI guard, float deltaTime)
    {
        var distance = Vector2.Distance(guard.Position, _homePosition);
        
        if (distance < 0.5f)
        {
            guard.ChangeState(AIState.Patrolling);
        }
        else
        {
            var direction = (_homePosition - guard.Position).Normalized;
            guard.Position = guard.Position + direction * guard.MoveSpeed * deltaTime;
            guard.FacingAngle = Vector2.Angle(guard.Position, _homePosition);
        }
    }
    
    public void Exit(GuardAI guard)
    {
        guard.AlertLevel = AlertLevel.Unaware;
        guard.Perception.ResetDetection();
        Console.WriteLine($"  [{guard.Name}] 已返回岗位，恢复巡逻");
    }
}

/// <summary>
/// 呼叫增援状态
/// </summary>
public class AlertingState : IAIState
{
    public AIState State => AIState.Alerting;
    private float _alertTimer;
    
    public void Enter(GuardAI guard)
    {
        _alertTimer = 0;
        Console.WriteLine($"  [{guard.Name}] 📻 呼叫增援！");
    }
    
    public void Update(GuardAI guard, float deltaTime)
    {
        _alertTimer += deltaTime;
        
        if (_alertTimer > 2f)
        {
            Console.WriteLine($"  [{guard.Name}] 📻 增援已呼叫，开始追击！");
            guard.ChangeState(AIState.Chasing);
        }
    }
    
    public void Exit(GuardAI guard) { }
}
