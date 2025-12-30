namespace VehicleStateMachine;

/// <summary>
/// 载具状态接口
/// </summary>
public interface IVehicleState
{
    VehicleState State { get; }
    void Enter(Vehicle vehicle);
    void Update(Vehicle vehicle, VehicleInput input, float deltaTime);
    void Exit(Vehicle vehicle);
}

/// <summary>
/// 停放状态
/// </summary>
public class ParkedState : IVehicleState
{
    public VehicleState State => VehicleState.Parked;
    
    public void Enter(Vehicle vehicle)
    {
        vehicle.Physics.Speed = 0;
        vehicle.Physics.RPM = 0;
        vehicle.Physics.CurrentGear = Gear.Neutral;
        Console.WriteLine("  🅿️ 车辆已停放");
    }
    
    public void Update(Vehicle vehicle, VehicleInput input, float deltaTime)
    {
        // 按油门启动
        if (input.Throttle > 0.1f)
        {
            vehicle.ChangeState(VehicleState.Idle);
        }
    }
    
    public void Exit(Vehicle vehicle)
    {
        Console.WriteLine("  🔑 启动引擎...");
    }
}

/// <summary>
/// 怠速状态
/// </summary>
public class IdleState : IVehicleState
{
    public VehicleState State => VehicleState.Idle;
    private float _idleTimer;
    
    public void Enter(Vehicle vehicle)
    {
        _idleTimer = 0;
        vehicle.Physics.RPM = 800;
        vehicle.Physics.CurrentGear = Gear.Neutral;
        Console.WriteLine("  ⚙️ 怠速中");
    }
    
    public void Update(Vehicle vehicle, VehicleInput input, float deltaTime)
    {
        _idleTimer += deltaTime;
        
        // 怠速RPM波动
        vehicle.Physics.RPM = 800 + MathF.Sin(_idleTimer * 2) * 50;
        
        if (input.Throttle > 0.1f)
        {
            vehicle.Physics.CurrentGear = Gear.First;
            vehicle.ChangeState(VehicleState.Accelerating);
        }
        else if (input.Brake > 0.5f && input.GearRequest == Gear.Reverse)
        {
            vehicle.Physics.CurrentGear = Gear.Reverse;
            vehicle.ChangeState(VehicleState.Reversing);
        }
        
        // 自然减速到停止
        if (vehicle.Physics.Speed > 0)
        {
            vehicle.Physics.Speed = Math.Max(0, vehicle.Physics.Speed - 5 * deltaTime);
        }
    }
    
    public void Exit(Vehicle vehicle) { }
}

/// <summary>
/// 加速状态
/// </summary>
public class AcceleratingState : IVehicleState
{
    public VehicleState State => VehicleState.Accelerating;
    
    public void Enter(Vehicle vehicle)
    {
        Console.WriteLine("  🚀 加速中!");
    }
    
    public void Update(Vehicle vehicle, VehicleInput input, float deltaTime)
    {
        var config = vehicle.Config;
        var physics = vehicle.Physics;
        var damage = vehicle.Damage;
        var surface = vehicle.CurrentSurface;
        
        // 计算加速度
        float acceleration = config.Acceleration * input.Throttle;
        acceleration *= damage.EngineEfficiency;
        acceleration *= GetSurfaceTraction(surface);
        
        // 氮气加速
        if (input.NitroBoost && vehicle.NitroAmount > 0)
        {
            acceleration *= config.NitroBoostPower;
            vehicle.NitroAmount -= config.NitroConsumption * deltaTime;
            Console.WriteLine("  🔥 氮气加速!");
        }
        
        // 更新速度
        float maxSpeed = config.MaxSpeed * damage.SpeedModifier;
        physics.Speed = Math.Min(maxSpeed, physics.Speed + acceleration * deltaTime * 3.6f); // 转换为km/h
        
        // 更新RPM和档位
        UpdateTransmission(vehicle, input);
        
        // 检查漂移条件
        if (input.Handbrake && physics.Speed > 30 && Math.Abs(input.Steering) > 0.5f)
        {
            vehicle.ChangeState(VehicleState.Drifting);
            return;
        }
        
        // 检查刹车
        if (input.Brake > 0.3f)
        {
            vehicle.ChangeState(VehicleState.Braking);
            return;
        }
        
        // 巡航条件
        if (input.Throttle < 0.2f && physics.Speed > 20)
        {
            vehicle.ChangeState(VehicleState.Cruising);
            return;
        }
        
        // 回到怠速
        if (physics.Speed < 5 && input.Throttle < 0.1f)
        {
            vehicle.ChangeState(VehicleState.Idle);
        }
    }
    
    private void UpdateTransmission(Vehicle vehicle, VehicleInput input)
    {
        var physics = vehicle.Physics;
        var config = vehicle.Config;
        
        // 简化的自动换挡逻辑
        float speedRatio = physics.Speed / config.MaxSpeed;
        int targetGear = (int)Math.Ceiling(speedRatio * config.GearCount);
        targetGear = Math.Clamp(targetGear, 1, config.GearCount);
        
        physics.CurrentGear = (Gear)targetGear;
        
        // 计算RPM
        float gearRatio = config.GearRatios[targetGear - 1];
        physics.RPM = 1000 + (physics.Speed / config.MaxSpeed) * 6000 / gearRatio;
        physics.RPM = Math.Clamp(physics.RPM, 1000, 8000);
    }
    
    private float GetSurfaceTraction(SurfaceType surface) => surface switch
    {
        SurfaceType.Asphalt => 1.0f,
        SurfaceType.Concrete => 0.95f,
        SurfaceType.Dirt => 0.7f,
        SurfaceType.Grass => 0.6f,
        SurfaceType.Sand => 0.5f,
        SurfaceType.Ice => 0.2f,
        SurfaceType.Water => 0.4f,
        _ => 1.0f
    };
    
    public void Exit(Vehicle vehicle) { }
}

/// <summary>
/// 巡航状态
/// </summary>
public class CruisingState : IVehicleState
{
    public VehicleState State => VehicleState.Cruising;
    
    public void Enter(Vehicle vehicle)
    {
        Console.WriteLine("  🚗 巡航中");
    }
    
    public void Update(Vehicle vehicle, VehicleInput input, float deltaTime)
    {
        var physics = vehicle.Physics;
        
        // 自然减速（空气阻力等）
        float dragDeceleration = physics.Speed * 0.01f;
        physics.Speed = Math.Max(0, physics.Speed - dragDeceleration * deltaTime * 3.6f);
        
        if (input.Throttle > 0.3f)
        {
            vehicle.ChangeState(VehicleState.Accelerating);
        }
        else if (input.Brake > 0.1f)
        {
            vehicle.ChangeState(VehicleState.Braking);
        }
        else if (physics.Speed < 15)
        {
            vehicle.ChangeState(VehicleState.Idle);
        }
    }
    
    public void Exit(Vehicle vehicle) { }
}

/// <summary>
/// 刹车状态
/// </summary>
public class BrakingState : IVehicleState
{
    public VehicleState State => VehicleState.Braking;
    
    public void Enter(Vehicle vehicle)
    {
        Console.WriteLine("  🛑 刹车中");
    }
    
    public void Update(Vehicle vehicle, VehicleInput input, float deltaTime)
    {
        var physics = vehicle.Physics;
        var config = vehicle.Config;
        
        // 刹车减速
        float brakeDeceleration = config.BrakeForce * input.Brake;
        physics.Speed = Math.Max(0, physics.Speed - brakeDeceleration * deltaTime * 3.6f);
        
        // 检查ABS（防抱死）
        if (physics.Speed > 20 && input.Brake > 0.9f)
        {
            physics.TractionLevel = 0.7f + MathF.Sin(DateTime.Now.Ticks * 0.00001f) * 0.3f;
        }
        
        if (physics.Speed < 3)
        {
            vehicle.ChangeState(VehicleState.Idle);
        }
        else if (input.Brake < 0.1f)
        {
            if (input.Throttle > 0.2f)
                vehicle.ChangeState(VehicleState.Accelerating);
            else
                vehicle.ChangeState(VehicleState.Cruising);
        }
    }
    
    public void Exit(Vehicle vehicle)
    {
        vehicle.Physics.TractionLevel = 1f;
    }
}

/// <summary>
/// 倒车状态
/// </summary>
public class ReversingState : IVehicleState
{
    public VehicleState State => VehicleState.Reversing;
    
    public void Enter(Vehicle vehicle)
    {
        vehicle.Physics.CurrentGear = Gear.Reverse;
        Console.WriteLine("  ◀️ 倒车中");
    }
    
    public void Update(Vehicle vehicle, VehicleInput input, float deltaTime)
    {
        var physics = vehicle.Physics;
        var config = vehicle.Config;
        
        if (input.Throttle > 0.1f)
        {
            // 加速倒车
            float acceleration = config.Acceleration * 0.5f * input.Throttle;
            physics.Speed = Math.Min(config.MaxReverseSpeed, physics.Speed + acceleration * deltaTime * 3.6f);
        }
        else
        {
            // 减速
            physics.Speed = Math.Max(0, physics.Speed - 5 * deltaTime);
        }
        
        if (input.GearRequest != Gear.Reverse || physics.Speed < 1)
        {
            vehicle.ChangeState(VehicleState.Idle);
        }
    }
    
    public void Exit(Vehicle vehicle)
    {
        vehicle.Physics.CurrentGear = Gear.Neutral;
    }
}

/// <summary>
/// 漂移状态
/// </summary>
public class DriftingState : IVehicleState
{
    public VehicleState State => VehicleState.Drifting;
    private float _driftAngle;
    private float _driftScore;
    
    public void Enter(Vehicle vehicle)
    {
        _driftAngle = 0;
        _driftScore = 0;
        Console.WriteLine("  🌀 开始漂移!");
    }
    
    public void Update(Vehicle vehicle, VehicleInput input, float deltaTime)
    {
        var physics = vehicle.Physics;
        var config = vehicle.Config;
        
        // 漂移角度
        _driftAngle = input.Steering * 45f;
        physics.SlipAngle = _driftAngle;
        
        // 计算漂移分数
        _driftScore += Math.Abs(_driftAngle) * physics.Speed * 0.01f * deltaTime;
        
        // 漂移时的速度损失较小
        float speedLoss = 2f * deltaTime;
        physics.Speed = Math.Max(20, physics.Speed - speedLoss);
        
        // 轮胎磨损
        vehicle.Damage.WheelHealth -= 0.5f * deltaTime;
        
        // 维持漂移需要手刹
        if (!input.Handbrake || Math.Abs(input.Steering) < 0.3f || physics.Speed < 25)
        {
            Console.WriteLine($"  🏆 漂移得分: {_driftScore:F0}");
            if (input.Throttle > 0.3f)
                vehicle.ChangeState(VehicleState.Accelerating);
            else
                vehicle.ChangeState(VehicleState.Cruising);
        }
    }
    
    public void Exit(Vehicle vehicle)
    {
        vehicle.Physics.SlipAngle = 0;
    }
}

/// <summary>
/// 腾空状态
/// </summary>
public class AirborneState : IVehicleState
{
    public VehicleState State => VehicleState.Airborne;
    private float _airTime;
    private float _maxHeight;
    
    public void Enter(Vehicle vehicle)
    {
        _airTime = 0;
        _maxHeight = vehicle.Physics.Height;
        vehicle.Physics.IsGrounded = false;
        Console.WriteLine("  🛫 腾空!");
    }
    
    public void Update(Vehicle vehicle, VehicleInput input, float deltaTime)
    {
        _airTime += deltaTime;
        
        // 模拟重力
        vehicle.Physics.Height -= 9.8f * _airTime * deltaTime;
        
        // 空中姿态控制
        vehicle.Physics.AngularVelocity = input.Steering * 30f;
        
        // 着陆检测
        if (vehicle.Physics.Height <= 0)
        {
            vehicle.Physics.Height = 0;
            vehicle.Physics.IsGrounded = true;
            
            // 着陆冲击
            float impactForce = _maxHeight * 10f;
            if (impactForce > 50)
            {
                vehicle.ApplyCollision(new CollisionInfo(impactForce, 90, "地面"));
            }
            
            Console.WriteLine($"  🛬 着陆! 滞空时间: {_airTime:F2}秒");
            
            if (vehicle.Damage.IsDestroyed)
            {
                vehicle.ChangeState(VehicleState.Exploded);
            }
            else if (vehicle.Physics.Speed > 20)
            {
                vehicle.ChangeState(VehicleState.Cruising);
            }
            else
            {
                vehicle.ChangeState(VehicleState.Idle);
            }
        }
    }
    
    public void Exit(Vehicle vehicle)
    {
        vehicle.Physics.AngularVelocity = 0;
    }
}

/// <summary>
/// 损坏状态
/// </summary>
public class DamagedState : IVehicleState
{
    public VehicleState State => VehicleState.Damaged;
    
    public void Enter(Vehicle vehicle)
    {
        Console.WriteLine($"  ⚠️ 车辆严重损坏! 剩余耐久: {vehicle.Damage.HealthPercent:P0}");
    }
    
    public void Update(Vehicle vehicle, VehicleInput input, float deltaTime)
    {
        // 损坏状态下性能大幅下降
        if (vehicle.Damage.IsDestroyed)
        {
            vehicle.ChangeState(VehicleState.Exploded);
            return;
        }
        
        // 可以继续驾驶但有限制
        if (input.Throttle > 0.1f && vehicle.Physics.Speed < vehicle.Config.MaxSpeed * 0.5f)
        {
            float limitedAccel = vehicle.Config.Acceleration * 0.3f * input.Throttle;
            vehicle.Physics.Speed += limitedAccel * deltaTime * 3.6f;
        }
        
        // 随机引擎熄火
        if (Random.Shared.NextDouble() < 0.01)
        {
            Console.WriteLine("  💨 引擎熄火!");
            vehicle.Physics.Speed *= 0.8f;
        }
        
        // 冒烟效果
        if (DateTime.Now.Second % 2 == 0)
        {
            vehicle.Physics.EngineTemp += 5 * deltaTime;
        }
        
        // 如果修复到一定程度，恢复正常
        if (vehicle.Damage.HealthPercent > 0.5f)
        {
            vehicle.ChangeState(VehicleState.Idle);
        }
    }
    
    public void Exit(Vehicle vehicle)
    {
        Console.WriteLine("  🔧 车辆状况改善");
    }
}

/// <summary>
/// 爆炸状态
/// </summary>
public class ExplodedState : IVehicleState
{
    public VehicleState State => VehicleState.Exploded;
    
    public void Enter(Vehicle vehicle)
    {
        vehicle.Physics.Speed = 0;
        vehicle.Physics.RPM = 0;
        Console.WriteLine("  💥 车辆爆炸!");
        Console.WriteLine("  🔥🔥🔥🔥🔥");
    }
    
    public void Update(Vehicle vehicle, VehicleInput input, float deltaTime)
    {
        // 等待重生或恢复
    }
    
    public void Exit(Vehicle vehicle) { }
}

/// <summary>
/// 恢复状态
/// </summary>
public class RecoveringState : IVehicleState
{
    public VehicleState State => VehicleState.Recovering;
    private float _recoverTimer;
    
    public void Enter(Vehicle vehicle)
    {
        _recoverTimer = 0;
        Console.WriteLine("  🔄 车辆恢复中...");
    }
    
    public void Update(Vehicle vehicle, VehicleInput input, float deltaTime)
    {
        _recoverTimer += deltaTime;
        
        // 恢复动画/重置位置
        if (_recoverTimer > 2f)
        {
            vehicle.Damage.Repair(50);
            vehicle.Physics.Speed = 0;
            vehicle.Physics.Height = 0;
            vehicle.Physics.IsGrounded = true;
            vehicle.ChangeState(VehicleState.Idle);
        }
    }
    
    public void Exit(Vehicle vehicle)
    {
        Console.WriteLine("  ✅ 车辆已恢复");
    }
}
