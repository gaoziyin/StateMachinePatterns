namespace VehicleStateMachine;

/// <summary>
/// 载具状态
/// </summary>
public enum VehicleState
{
    Parked,         // 停放
    Idle,           // 怠速
    Accelerating,   // 加速
    Cruising,       // 巡航
    Braking,        // 刹车
    Reversing,      // 倒车
    Drifting,       // 漂移
    Airborne,       // 腾空
    Damaged,        // 损坏
    Exploded,       // 爆炸
    Recovering      // 恢复中
}

/// <summary>
/// 地面类型
/// </summary>
public enum SurfaceType
{
    Asphalt,        // 沥青 - 最佳抓地力
    Concrete,       // 混凝土 - 良好抓地力
    Dirt,           // 泥土 - 中等抓地力
    Grass,          // 草地 - 较低抓地力
    Sand,           // 沙地 - 低抓地力
    Ice,            // 冰面 - 极低抓地力
    Water           // 水面 - 减速
}

/// <summary>
/// 轮胎类型
/// </summary>
public enum TireType
{
    Slick,          // 光头胎 - 干燥最佳
    AllSeason,      // 全季节 - 平衡
    Rain,           // 雨胎 - 湿滑最佳
    OffRoad,        // 越野胎 - 非铺装路面
    Drift           // 漂移胎 - 易于侧滑
}

/// <summary>
/// 档位
/// </summary>
public enum Gear
{
    Reverse = -1,   // 倒车
    Neutral = 0,    // 空挡
    First = 1,
    Second = 2,
    Third = 3,
    Fourth = 4,
    Fifth = 5,
    Sixth = 6
}

/// <summary>
/// 输入指令
/// </summary>
public class VehicleInput
{
    public float Throttle { get; set; }     // 0-1 油门
    public float Brake { get; set; }        // 0-1 刹车
    public float Steering { get; set; }     // -1到1 方向盘
    public bool Handbrake { get; set; }     // 手刹
    public bool NitroBoost { get; set; }    // 氮气加速
    public Gear? GearRequest { get; set; }  // 换挡请求
}

/// <summary>
/// 碰撞信息
/// </summary>
public record CollisionInfo(
    float ImpactForce,
    float ImpactAngle,
    string CollidedWith
);

/// <summary>
/// 载具配置
/// </summary>
public class VehicleConfig
{
    public string Name { get; set; } = "默认车辆";
    public VehicleClass Class { get; set; } = VehicleClass.Sport;
    
    // 性能参数
    public float MaxSpeed { get; set; } = 200f;             // km/h
    public float Acceleration { get; set; } = 8f;           // m/s²
    public float BrakeForce { get; set; } = 15f;            // m/s²
    public float MaxReverseSpeed { get; set; } = 40f;       // km/h
    
    // 操控参数
    public float SteeringSpeed { get; set; } = 2f;          // 转向速度
    public float MaxSteeringAngle { get; set; } = 35f;      // 最大转向角
    public float TractionControl { get; set; } = 0.8f;      // 牵引力控制 0-1
    public float DriftThreshold { get; set; } = 0.7f;       // 漂移触发阈值
    
    // 耐久参数
    public float MaxHealth { get; set; } = 100f;
    public float ArmorRating { get; set; } = 1f;            // 护甲系数
    
    // 氮气
    public float NitroCapacity { get; set; } = 100f;        // 氮气容量
    public float NitroBoostPower { get; set; } = 1.5f;      // 氮气加速倍数
    public float NitroConsumption { get; set; } = 25f;      // 每秒消耗
    
    // 变速箱
    public int GearCount { get; set; } = 6;
    public float[] GearRatios { get; set; } = { 3.5f, 2.5f, 1.8f, 1.4f, 1.1f, 0.9f };
}

/// <summary>
/// 载具类型
/// </summary>
public enum VehicleClass
{
    Compact,        // 紧凑型
    Sedan,          // 轿车
    Sport,          // 跑车
    Super,          // 超跑
    Muscle,         // 肌肉车
    Offroad,        // 越野车
    Truck           // 卡车
}

/// <summary>
/// 物理参数
/// </summary>
public class PhysicsState
{
    public float Speed { get; set; }            // 当前速度 km/h
    public float RPM { get; set; }              // 发动机转速
    public float AngularVelocity { get; set; }  // 角速度
    public float SlipAngle { get; set; }        // 滑移角
    public float TractionLevel { get; set; }    // 抓地力等级 0-1
    public float Height { get; set; }           // 离地高度
    public Gear CurrentGear { get; set; }       // 当前档位
    public float EngineTemp { get; set; }       // 发动机温度
    public bool IsGrounded { get; set; }        // 是否着地
}

/// <summary>
/// 损坏系统
/// </summary>
public class DamageSystem
{
    public float CurrentHealth { get; set; }
    public float MaxHealth { get; set; }
    
    public float EngineHealth { get; set; } = 100f;     // 发动机
    public float WheelHealth { get; set; } = 100f;      // 轮胎
    public float BodyHealth { get; set; } = 100f;       // 车身
    
    public float HealthPercent => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0;
    
    public bool IsDestroyed => CurrentHealth <= 0;
    public bool HasMajorDamage => HealthPercent < 0.3f;
    public bool HasMinorDamage => HealthPercent < 0.7f;
    
    /// <summary>
    /// 发动机效率 (受损时降低)
    /// </summary>
    public float EngineEfficiency => EngineHealth / 100f;
    
    /// <summary>
    /// 最高速度修正
    /// </summary>
    public float SpeedModifier
    {
        get
        {
            if (EngineHealth < 30) return 0.5f;
            if (EngineHealth < 60) return 0.7f;
            if (EngineHealth < 80) return 0.9f;
            return 1f;
        }
    }
    
    /// <summary>
    /// 操控修正
    /// </summary>
    public float HandlingModifier
    {
        get
        {
            if (WheelHealth < 30) return 0.5f;
            if (WheelHealth < 60) return 0.75f;
            return 1f;
        }
    }
    
    public void ApplyDamage(float damage, float armorRating)
    {
        float actualDamage = damage / armorRating;
        CurrentHealth = Math.Max(0, CurrentHealth - actualDamage);
        
        // 随机分配部件损伤
        var rand = Random.Shared;
        EngineHealth = Math.Max(0, EngineHealth - actualDamage * (0.5f + (float)rand.NextDouble() * 0.5f));
        WheelHealth = Math.Max(0, WheelHealth - actualDamage * (0.3f + (float)rand.NextDouble() * 0.4f));
        BodyHealth = Math.Max(0, BodyHealth - actualDamage * (0.4f + (float)rand.NextDouble() * 0.6f));
    }
    
    public void Repair(float amount)
    {
        CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        EngineHealth = Math.Min(100, EngineHealth + amount * 0.8f);
        WheelHealth = Math.Min(100, WheelHealth + amount * 0.9f);
        BodyHealth = Math.Min(100, BodyHealth + amount);
    }
}
