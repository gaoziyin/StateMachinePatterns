namespace VehicleStateMachine;

/// <summary>
/// 载具控制器
/// </summary>
public class Vehicle
{
    private readonly Dictionary<VehicleState, IVehicleState> _states = new();
    private IVehicleState? _currentState;
    
    // 配置
    public VehicleConfig Config { get; }
    
    // 物理状态
    public PhysicsState Physics { get; } = new();
    
    // 损坏系统
    public DamageSystem Damage { get; }
    
    // 当前地面
    public SurfaceType CurrentSurface { get; set; } = SurfaceType.Asphalt;
    
    // 轮胎类型
    public TireType CurrentTires { get; set; } = TireType.AllSeason;
    
    // 氮气
    public float NitroAmount { get; set; }
    
    // 状态
    public VehicleState CurrentState => _currentState?.State ?? VehicleState.Parked;
    
    // 统计
    public float TotalDistance { get; private set; }
    public float MaxSpeedReached { get; private set; }
    public int CollisionCount { get; private set; }
    
    // 事件
    public event Action<VehicleState, VehicleState>? OnStateChanged;
    public event Action<CollisionInfo>? OnCollision;
    public event Action? OnDestroyed;
    
    public Vehicle(VehicleConfig config)
    {
        Config = config;
        Damage = new DamageSystem
        {
            MaxHealth = config.MaxHealth,
            CurrentHealth = config.MaxHealth
        };
        NitroAmount = config.NitroCapacity;
        
        InitializeStates();
        _currentState = _states[VehicleState.Parked];
    }
    
    private void InitializeStates()
    {
        _states[VehicleState.Parked] = new ParkedState();
        _states[VehicleState.Idle] = new IdleState();
        _states[VehicleState.Accelerating] = new AcceleratingState();
        _states[VehicleState.Cruising] = new CruisingState();
        _states[VehicleState.Braking] = new BrakingState();
        _states[VehicleState.Reversing] = new ReversingState();
        _states[VehicleState.Drifting] = new DriftingState();
        _states[VehicleState.Airborne] = new AirborneState();
        _states[VehicleState.Damaged] = new DamagedState();
        _states[VehicleState.Exploded] = new ExplodedState();
        _states[VehicleState.Recovering] = new RecoveringState();
    }
    
    /// <summary>
    /// 更新载具
    /// </summary>
    public void Update(VehicleInput input, float deltaTime)
    {
        // 更新物理
        _currentState?.Update(this, input, deltaTime);
        
        // 统计
        TotalDistance += Physics.Speed / 3600f * deltaTime; // km
        MaxSpeedReached = Math.Max(MaxSpeedReached, Physics.Speed);
        
        // 氮气恢复（非使用时）
        if (!input.NitroBoost && NitroAmount < Config.NitroCapacity)
        {
            NitroAmount += 5 * deltaTime;
        }
        
        // 发动机冷却
        if (Physics.EngineTemp > 90)
        {
            Physics.EngineTemp -= 2 * deltaTime;
        }
        
        // 检查损坏状态
        if (Damage.HasMajorDamage && CurrentState != VehicleState.Damaged && 
            CurrentState != VehicleState.Exploded && CurrentState != VehicleState.Recovering)
        {
            ChangeState(VehicleState.Damaged);
        }
    }
    
    /// <summary>
    /// 改变状态
    /// </summary>
    public void ChangeState(VehicleState newState)
    {
        if (_states.TryGetValue(newState, out var state))
        {
            var oldState = CurrentState;
            _currentState?.Exit(this);
            _currentState = state;
            _currentState.Enter(this);
            OnStateChanged?.Invoke(oldState, newState);
        }
    }
    
    /// <summary>
    /// 应用碰撞
    /// </summary>
    public void ApplyCollision(CollisionInfo collision)
    {
        CollisionCount++;
        OnCollision?.Invoke(collision);
        
        Console.WriteLine($"  💥 碰撞! 撞击 {collision.CollidedWith}, 冲击力: {collision.ImpactForce:F0}");
        
        // 计算伤害
        float damage = collision.ImpactForce * 0.5f;
        Damage.ApplyDamage(damage, Config.ArmorRating);
        
        // 速度损失
        Physics.Speed *= 0.5f;
        
        if (Damage.IsDestroyed)
        {
            OnDestroyed?.Invoke();
            ChangeState(VehicleState.Exploded);
        }
        else if (Damage.HasMajorDamage)
        {
            ChangeState(VehicleState.Damaged);
        }
    }
    
    /// <summary>
    /// 触发腾空
    /// </summary>
    public void TriggerAirborne(float height)
    {
        Physics.Height = height;
        ChangeState(VehicleState.Airborne);
    }
    
    /// <summary>
    /// 重置/重生
    /// </summary>
    public void Respawn()
    {
        ChangeState(VehicleState.Recovering);
    }
    
    /// <summary>
    /// 维修
    /// </summary>
    public void Repair(float amount)
    {
        Damage.Repair(amount);
        Console.WriteLine($"  🔧 维修完成! 耐久: {Damage.HealthPercent:P0}");
    }
    
    /// <summary>
    /// 填充氮气
    /// </summary>
    public void RefillNitro()
    {
        NitroAmount = Config.NitroCapacity;
        Console.WriteLine("  ⛽ 氮气已填满!");
    }
    
    /// <summary>
    /// 打印状态
    /// </summary>
    public void PrintDashboard()
    {
        var speedBar = GenerateBar(Physics.Speed, Config.MaxSpeed, 20);
        var rpmBar = GenerateBar(Physics.RPM, 8000, 15);
        var healthBar = GenerateBar(Damage.CurrentHealth, Damage.MaxHealth, 10);
        var nitroBar = GenerateBar(NitroAmount, Config.NitroCapacity, 10);
        
        var gearDisplay = Physics.CurrentGear switch
        {
            Gear.Reverse => "R",
            Gear.Neutral => "N",
            _ => ((int)Physics.CurrentGear).ToString()
        };
        
        var stateIcon = CurrentState switch
        {
            VehicleState.Parked => "🅿️",
            VehicleState.Idle => "⚙️",
            VehicleState.Accelerating => "🚀",
            VehicleState.Cruising => "🚗",
            VehicleState.Braking => "🛑",
            VehicleState.Reversing => "◀️",
            VehicleState.Drifting => "🌀",
            VehicleState.Airborne => "🛫",
            VehicleState.Damaged => "⚠️",
            VehicleState.Exploded => "💥",
            VehicleState.Recovering => "🔄",
            _ => "❓"
        };
        
        Console.WriteLine("\n┌─────────────────────────────────────────────────────────────┐");
        Console.WriteLine($"│  🚗 {Config.Name,-20}    {stateIcon} {CurrentState,-15}     │");
        Console.WriteLine("├─────────────────────────────────────────────────────────────┤");
        Console.WriteLine($"│  速度  [{speedBar}] {Physics.Speed,6:F1} km/h          │");
        Console.WriteLine($"│  转速  [{rpmBar}] {Physics.RPM,6:F0} RPM  档位: {gearDisplay,2}  │");
        Console.WriteLine($"│  耐久  [{healthBar}] {Damage.HealthPercent,6:P0}                    │");
        Console.WriteLine($"│  氮气  [{nitroBar}] {NitroAmount / Config.NitroCapacity,6:P0}                    │");
        Console.WriteLine("├─────────────────────────────────────────────────────────────┤");
        Console.WriteLine($"│  路面: {CurrentSurface,-10} 轮胎: {CurrentTires,-12}            │");
        Console.WriteLine($"│  里程: {TotalDistance:F2} km  最高速: {MaxSpeedReached:F1} km/h              │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────┘");
    }
    
    private static string GenerateBar(float current, float max, int length)
    {
        int filled = max > 0 ? (int)(current / max * length) : 0;
        filled = Math.Clamp(filled, 0, length);
        return new string('█', filled) + new string('░', length - filled);
    }
}

/// <summary>
/// 载具工厂
/// </summary>
public static class VehicleFactory
{
    public static Vehicle CreateSportsCar(string name = "Sport GT")
    {
        return new Vehicle(new VehicleConfig
        {
            Name = name,
            Class = VehicleClass.Sport,
            MaxSpeed = 280,
            Acceleration = 10,
            BrakeForce = 18,
            MaxReverseSpeed = 40,
            SteeringSpeed = 2.5f,
            MaxSteeringAngle = 35,
            TractionControl = 0.85f,
            MaxHealth = 80,
            ArmorRating = 0.8f,
            NitroCapacity = 100,
            NitroBoostPower = 1.6f
        });
    }
    
    public static Vehicle CreateMuscleCar(string name = "Muscle V8")
    {
        return new Vehicle(new VehicleConfig
        {
            Name = name,
            Class = VehicleClass.Muscle,
            MaxSpeed = 240,
            Acceleration = 12,
            BrakeForce = 14,
            MaxReverseSpeed = 35,
            SteeringSpeed = 1.8f,
            MaxSteeringAngle = 30,
            TractionControl = 0.6f,
            DriftThreshold = 0.5f,
            MaxHealth = 100,
            ArmorRating = 1.2f,
            NitroCapacity = 120,
            NitroBoostPower = 1.8f
        });
    }
    
    public static Vehicle CreateOffroadTruck(string name = "Offroad Beast")
    {
        return new Vehicle(new VehicleConfig
        {
            Name = name,
            Class = VehicleClass.Offroad,
            MaxSpeed = 160,
            Acceleration = 6,
            BrakeForce = 12,
            MaxReverseSpeed = 30,
            SteeringSpeed = 1.5f,
            MaxSteeringAngle = 40,
            TractionControl = 0.95f,
            MaxHealth = 150,
            ArmorRating = 1.5f,
            NitroCapacity = 80,
            NitroBoostPower = 1.4f
        });
    }
    
    public static Vehicle CreateSupercar(string name = "Hyper X")
    {
        return new Vehicle(new VehicleConfig
        {
            Name = name,
            Class = VehicleClass.Super,
            MaxSpeed = 350,
            Acceleration = 14,
            BrakeForce = 22,
            MaxReverseSpeed = 50,
            SteeringSpeed = 3f,
            MaxSteeringAngle = 32,
            TractionControl = 0.95f,
            MaxHealth = 60,
            ArmorRating = 0.6f,
            NitroCapacity = 80,
            NitroBoostPower = 1.5f
        });
    }
}
