using VehicleStateMachine;

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                   🚗 赛车载具系统演示 🚗                       ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");

// 创建载具
Console.WriteLine("\n【选择载具】");
Console.WriteLine("  1. Sport GT - 平衡型跑车");
Console.WriteLine("  2. Muscle V8 - 大马力肌肉车");
Console.WriteLine("  3. Offroad Beast - 越野卡车");
Console.WriteLine("  4. Hyper X - 极速超跑");

var vehicle = VehicleFactory.CreateMuscleCar("Muscle V8");

// 订阅事件
vehicle.OnStateChanged += (oldState, newState) =>
{
    Console.WriteLine($"\n  [状态] {oldState} → {newState}");
};

vehicle.OnCollision += collision =>
{
    Console.WriteLine($"  [碰撞] 与 {collision.CollidedWith} 发生碰撞!");
};

vehicle.OnDestroyed += () =>
{
    Console.WriteLine("  [事件] 🔥 车辆报废!");
};


Console.WriteLine("\n\n========== 场景1: 起步加速 ==========\n");

vehicle.PrintDashboard();

// 模拟输入
var input = new VehicleInput();

// 启动
Console.WriteLine("\n--- 踩油门启动 ---");
input.Throttle = 0.3f;
for (int i = 0; i < 10; i++)
{
    vehicle.Update(input, 0.1f);
}
vehicle.PrintDashboard();

// 全油门加速
Console.WriteLine("\n--- 全油门加速 ---");
input.Throttle = 1.0f;
for (int i = 0; i < 30; i++)
{
    vehicle.Update(input, 0.1f);
    if (i % 10 == 9)
    {
        Console.WriteLine($"  速度: {vehicle.Physics.Speed:F1} km/h, 档位: {vehicle.Physics.CurrentGear}");
    }
}
vehicle.PrintDashboard();


Console.WriteLine("\n\n========== 场景2: 氮气加速 ==========\n");

Console.WriteLine("--- 启动氮气! ---");
input.Throttle = 1.0f;
input.NitroBoost = true;
for (int i = 0; i < 20; i++)
{
    vehicle.Update(input, 0.1f);
    if (i % 5 == 4)
    {
        Console.WriteLine($"  🔥 速度: {vehicle.Physics.Speed:F1} km/h, 氮气: {vehicle.NitroAmount:F0}%");
    }
}
input.NitroBoost = false;
vehicle.PrintDashboard();


Console.WriteLine("\n\n========== 场景3: 漂移 ==========\n");

Console.WriteLine("--- 准备漂移 ---");
// 减速到漂移速度
input.Throttle = 0.5f;
input.Brake = 0.3f;
for (int i = 0; i < 20; i++)
{
    vehicle.Update(input, 0.1f);
}
Console.WriteLine($"  当前速度: {vehicle.Physics.Speed:F1} km/h");

Console.WriteLine("\n--- 拉手刹+转向，开始漂移! ---");
input.Brake = 0f;
input.Throttle = 0.8f;
input.Handbrake = true;
input.Steering = 0.9f;

for (int i = 0; i < 30; i++)
{
    vehicle.Update(input, 0.1f);
    if (vehicle.CurrentState == VehicleState.Drifting && i % 5 == 0)
    {
        Console.WriteLine($"  🌀 漂移中! 滑移角: {vehicle.Physics.SlipAngle:F1}°");
    }
}

input.Handbrake = false;
input.Steering = 0f;
vehicle.PrintDashboard();


Console.WriteLine("\n\n========== 场景4: 跳跃 ==========\n");

Console.WriteLine("--- 冲向跳台! ---");
input.Throttle = 1.0f;
for (int i = 0; i < 20; i++)
{
    vehicle.Update(input, 0.1f);
}

Console.WriteLine("--- 腾空! ---");
vehicle.TriggerAirborne(5f); // 5米高

for (int i = 0; i < 30; i++)
{
    vehicle.Update(input, 0.1f);
    if (vehicle.CurrentState == VehicleState.Airborne)
    {
        Console.WriteLine($"  🛫 高度: {vehicle.Physics.Height:F1}m");
    }
    if (vehicle.CurrentState != VehicleState.Airborne) break;
}
vehicle.PrintDashboard();


Console.WriteLine("\n\n========== 场景5: 碰撞与损坏 ==========\n");

Console.WriteLine("--- 高速碰撞! ---");
input.Throttle = 1.0f;
for (int i = 0; i < 15; i++)
{
    vehicle.Update(input, 0.1f);
}

// 模拟碰撞
vehicle.ApplyCollision(new CollisionInfo(80, 45, "墙壁"));
vehicle.PrintDashboard();

Console.WriteLine("\n--- 再次碰撞! ---");
vehicle.ApplyCollision(new CollisionInfo(60, 30, "护栏"));
vehicle.PrintDashboard();


Console.WriteLine("\n\n========== 场景6: 维修与恢复 ==========\n");

if (vehicle.CurrentState == VehicleState.Damaged || vehicle.CurrentState == VehicleState.Exploded)
{
    Console.WriteLine("--- 请求恢复 ---");
    vehicle.Respawn();
    
    for (int i = 0; i < 25; i++)
    {
        vehicle.Update(new VehicleInput(), 0.1f);
    }
}

Console.WriteLine("\n--- 进站维修 ---");
vehicle.Repair(50);
vehicle.RefillNitro();
vehicle.PrintDashboard();


Console.WriteLine("\n\n========== 场景7: 不同路面 ==========\n");

Console.WriteLine("--- 切换到泥土路面 ---");
vehicle.CurrentSurface = SurfaceType.Dirt;
vehicle.CurrentTires = TireType.OffRoad;

input.Throttle = 1.0f;
input.Brake = 0;
for (int i = 0; i < 20; i++)
{
    vehicle.Update(input, 0.1f);
}
Console.WriteLine($"  泥土路面加速较慢: {vehicle.Physics.Speed:F1} km/h");

Console.WriteLine("\n--- 切换到冰面! ---");
vehicle.CurrentSurface = SurfaceType.Ice;
for (int i = 0; i < 20; i++)
{
    vehicle.Update(input, 0.1f);
}
Console.WriteLine($"  冰面抓地力极低: {vehicle.Physics.Speed:F1} km/h");

vehicle.PrintDashboard();


Console.WriteLine("\n\n════════════════════════════════════════════════════════════════");
Console.WriteLine("                         系统说明");
Console.WriteLine("════════════════════════════════════════════════════════════════");
Console.WriteLine(@"
【载具状态】
  Parked → Idle → Accelerating ↔ Cruising ↔ Braking
                       ↓
                   Drifting
                       
  任意状态 → Airborne → 着陆后恢复
  任意状态 → Damaged → Exploded → Recovering

【物理系统】
  - 速度: 受加速度、路面、损坏影响
  - 转速: 根据速度和档位计算
  - 抓地力: 取决于轮胎和路面组合

【路面抓地力】
  沥青: 100%  混凝土: 95%  泥土: 70%
  草地: 60%   沙地: 50%    冰面: 20%

【损坏系统】
  - 碰撞造成伤害
  - 发动机损坏降低加速和极速
  - 轮胎损坏影响操控
  - 耐久归零则爆炸
");

Console.WriteLine("演示完成!");
