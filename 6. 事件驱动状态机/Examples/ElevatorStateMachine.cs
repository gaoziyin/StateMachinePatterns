namespace EventDrivenStatePattern.Examples;

/// <summary>
/// 电梯状态
/// </summary>
public enum ElevatorState
{
    Idle,           // 空闲
    MovingUp,       // 上升中
    MovingDown,     // 下降中
    DoorsOpening,   // 开门中
    DoorsOpen,      // 门已开
    DoorsClosing,   // 关门中
    Emergency,      // 紧急状态
    Maintenance     // 维护状态
}

/// <summary>
/// 电梯事件
/// </summary>
public enum ElevatorEvent
{
    CallUp,         // 向上呼叫
    CallDown,       // 向下呼叫
    FloorReached,   // 到达楼层
    OpenDoors,      // 开门
    CloseDoors,     // 关门
    DoorsOpened,    // 门已开完
    DoorsClosed,    // 门已关完
    Obstruction,    // 障碍物
    EmergencyStop,  // 紧急停止
    Reset,          // 重置
    StartMaintenance, // 开始维护
    EndMaintenance  // 结束维护
}

/// <summary>
/// 电梯控制系统
/// </summary>
public class Elevator
{
    public int CurrentFloor { get; private set; } = 1;
    public int TargetFloor { get; private set; } = 1;
    public int MaxFloor { get; } = 20;
    public int MinFloor { get; } = 1;
    public bool HasObstruction { get; private set; }
    
    private readonly StateMachine<ElevatorState, ElevatorEvent> _stateMachine;
    private readonly Queue<int> _upQueue = new();
    private readonly Queue<int> _downQueue = new();
    
    public ElevatorState CurrentState => _stateMachine.CurrentState;
    
    public Elevator()
    {
        _stateMachine = new StateMachine<ElevatorState, ElevatorEvent>(ElevatorState.Idle);
        ConfigureStateMachine();
    }
    
    private void ConfigureStateMachine()
    {
        // 空闲状态
        _stateMachine.Configure(ElevatorState.Idle)
            .PermitIf(ElevatorEvent.CallUp, ElevatorState.MovingUp,
                () => HasUpCalls(), "有向上请求")
            .PermitIf(ElevatorEvent.CallDown, ElevatorState.MovingDown,
                () => HasDownCalls(), "有向下请求")
            .Permit(ElevatorEvent.OpenDoors, ElevatorState.DoorsOpening)
            .Permit(ElevatorEvent.EmergencyStop, ElevatorState.Emergency)
            .Permit(ElevatorEvent.StartMaintenance, ElevatorState.Maintenance)
            .OnEntry(() => Console.WriteLine($"    🛗 电梯空闲，当前楼层: {CurrentFloor}"));
            
        // 上升中
        _stateMachine.Configure(ElevatorState.MovingUp)
            .Permit(ElevatorEvent.FloorReached, ElevatorState.DoorsOpening)
            .PermitIf(ElevatorEvent.CallUp, ElevatorState.MovingUp,
                () => true, "继续上升")
            .Permit(ElevatorEvent.EmergencyStop, ElevatorState.Emergency)
            .OnEntry(() =>
            {
                if (_upQueue.Count > 0)
                {
                    TargetFloor = _upQueue.Dequeue();
                }
                Console.WriteLine($"    ⬆️ 电梯上升中: {CurrentFloor} → {TargetFloor}");
            })
            .OnExit(() =>
            {
                CurrentFloor = TargetFloor;
                Console.WriteLine($"    📍 到达楼层: {CurrentFloor}");
            });
            
        // 下降中
        _stateMachine.Configure(ElevatorState.MovingDown)
            .Permit(ElevatorEvent.FloorReached, ElevatorState.DoorsOpening)
            .PermitIf(ElevatorEvent.CallDown, ElevatorState.MovingDown,
                () => true, "继续下降")
            .Permit(ElevatorEvent.EmergencyStop, ElevatorState.Emergency)
            .OnEntry(() =>
            {
                if (_downQueue.Count > 0)
                {
                    TargetFloor = _downQueue.Dequeue();
                }
                Console.WriteLine($"    ⬇️ 电梯下降中: {CurrentFloor} → {TargetFloor}");
            })
            .OnExit(() =>
            {
                CurrentFloor = TargetFloor;
                Console.WriteLine($"    📍 到达楼层: {CurrentFloor}");
            });
            
        // 开门中
        _stateMachine.Configure(ElevatorState.DoorsOpening)
            .Permit(ElevatorEvent.DoorsOpened, ElevatorState.DoorsOpen)
            .Permit(ElevatorEvent.EmergencyStop, ElevatorState.Emergency)
            .OnEntry(() => Console.WriteLine($"    🚪 门正在打开..."));
            
        // 门已开
        _stateMachine.Configure(ElevatorState.DoorsOpen)
            .Permit(ElevatorEvent.CloseDoors, ElevatorState.DoorsClosing)
            .Permit(ElevatorEvent.EmergencyStop, ElevatorState.Emergency)
            .Ignore(ElevatorEvent.OpenDoors) // 门已开，忽略开门请求
            .OnEntry(() => Console.WriteLine($"    🚪 门已打开，请进出"));
            
        // 关门中
        _stateMachine.Configure(ElevatorState.DoorsClosing)
            .Permit(ElevatorEvent.DoorsClosed, ElevatorState.Idle)
            .PermitIf(ElevatorEvent.Obstruction, ElevatorState.DoorsOpening,
                () => HasObstruction, "检测到障碍物")
            .Permit(ElevatorEvent.OpenDoors, ElevatorState.DoorsOpening)
            .Permit(ElevatorEvent.EmergencyStop, ElevatorState.Emergency)
            .OnEntry(() => Console.WriteLine($"    🚪 门正在关闭..."))
            .OnExit(() => HasObstruction = false);
            
        // 紧急状态
        _stateMachine.Configure(ElevatorState.Emergency)
            .Permit(ElevatorEvent.Reset, ElevatorState.Idle)
            .Ignore(ElevatorEvent.CallUp)
            .Ignore(ElevatorEvent.CallDown)
            .Ignore(ElevatorEvent.OpenDoors)
            .Ignore(ElevatorEvent.CloseDoors)
            .OnEntry(() => Console.WriteLine($"    🚨 紧急状态! 电梯停止运行"));
            
        // 维护状态
        _stateMachine.Configure(ElevatorState.Maintenance)
            .Permit(ElevatorEvent.EndMaintenance, ElevatorState.Idle)
            .Ignore(ElevatorEvent.CallUp)
            .Ignore(ElevatorEvent.CallDown)
            .OnEntry(() => Console.WriteLine($"    🔧 维护模式，电梯暂停服务"));
            
        // 订阅事件
        _stateMachine.OnTransitioned += (sender, args) =>
        {
            Console.WriteLine($"  [电梯状态] {args.Source} → {args.Destination}");
        };
    }
    
    private bool HasUpCalls() => _upQueue.Count > 0;
    private bool HasDownCalls() => _downQueue.Count > 0;
    
    // 公共操作
    public void CallToFloor(int floor)
    {
        if (floor < MinFloor || floor > MaxFloor)
        {
            Console.WriteLine($"  [错误] 无效楼层: {floor}");
            return;
        }
        
        Console.WriteLine($"\n[呼叫] 请求到达楼层: {floor}");
        
        if (floor > CurrentFloor)
        {
            _upQueue.Enqueue(floor);
            _stateMachine.Fire(ElevatorEvent.CallUp);
        }
        else if (floor < CurrentFloor)
        {
            _downQueue.Enqueue(floor);
            _stateMachine.Fire(ElevatorEvent.CallDown);
        }
        else
        {
            _stateMachine.Fire(ElevatorEvent.OpenDoors);
        }
    }
    
    public void SimulateArrival()
    {
        _stateMachine.Fire(ElevatorEvent.FloorReached);
        _stateMachine.Fire(ElevatorEvent.DoorsOpened);
    }
    
    public void CloseDoors() => _stateMachine.Fire(ElevatorEvent.CloseDoors);
    public void OpenDoors() => _stateMachine.Fire(ElevatorEvent.OpenDoors);
    public void DoorsClosed() => _stateMachine.Fire(ElevatorEvent.DoorsClosed);
    public void EmergencyStop() => _stateMachine.Fire(ElevatorEvent.EmergencyStop);
    public void Reset() => _stateMachine.Fire(ElevatorEvent.Reset);
    public void StartMaintenance() => _stateMachine.Fire(ElevatorEvent.StartMaintenance);
    public void EndMaintenance() => _stateMachine.Fire(ElevatorEvent.EndMaintenance);
    
    public void SetObstruction(bool hasObstruction)
    {
        HasObstruction = hasObstruction;
        if (hasObstruction)
        {
            _stateMachine.Fire(ElevatorEvent.Obstruction);
        }
    }
    
    public void PrintStatus()
    {
        Console.WriteLine($"\n  ╔═══════════════════════════════════════════╗");
        Console.WriteLine($"  ║ 电梯状态: {CurrentState,-15}            ║");
        Console.WriteLine($"  ║ 当前楼层: {CurrentFloor,-3} 目标楼层: {TargetFloor,-3}         ║");
        Console.WriteLine($"  ║ 上行队列: [{string.Join(",", _upQueue),-10}]               ║");
        Console.WriteLine($"  ║ 下行队列: [{string.Join(",", _downQueue),-10}]               ║");
        Console.WriteLine($"  ╚═══════════════════════════════════════════╝");
    }
}
