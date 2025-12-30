using StateMachinePatterns.Core;

namespace StateMachinePatterns.Examples;

/// <summary>
/// Example: Traffic Light using explicit state machine with transitions.
/// Demonstrates how to configure allowed transitions and handle events.
/// </summary>
public class TrafficLightExample
{
    public enum LightState
    {
        Red,
        Yellow,
        Green
    }
    
    public enum Trigger
    {
        TimerExpired
    }
    
    public static void Run()
    {
        Console.WriteLine("=== Traffic Light Example (State Machine with Transitions) ===\n");
        
        var trafficLight = new TrafficLight();
        
        for (int i = 0; i < 7; i++)
        {
            Console.WriteLine($"Current Light: {trafficLight.CurrentLight}");
            Thread.Sleep(1000); // Simulate time passing
            trafficLight.Tick();
            Console.WriteLine();
        }
    }
}

public class TrafficLight
{
    private readonly StateMachine<TrafficLightExample.LightState, TrafficLightExample.Trigger> _stateMachine;
    
    public TrafficLightExample.LightState CurrentLight => _stateMachine.CurrentState;
    
    public TrafficLight()
    {
        _stateMachine = new StateMachine<TrafficLightExample.LightState, TrafficLightExample.Trigger>(
            TrafficLightExample.LightState.Red);
        
        // Configure state transitions
        _stateMachine.Configure(TrafficLightExample.LightState.Red)
            .Permit(TrafficLightExample.Trigger.TimerExpired, TrafficLightExample.LightState.Green)
            .OnEntry(() => Console.WriteLine("🔴 Red light - STOP!"))
            .OnExit(() => Console.WriteLine("Red light timer expired"));
        
        _stateMachine.Configure(TrafficLightExample.LightState.Green)
            .Permit(TrafficLightExample.Trigger.TimerExpired, TrafficLightExample.LightState.Yellow)
            .OnEntry(() => Console.WriteLine("🟢 Green light - GO!"))
            .OnExit(() => Console.WriteLine("Green light timer expired"));
        
        _stateMachine.Configure(TrafficLightExample.LightState.Yellow)
            .Permit(TrafficLightExample.Trigger.TimerExpired, TrafficLightExample.LightState.Red)
            .OnEntry(() => Console.WriteLine("🟡 Yellow light - CAUTION!"))
            .OnExit(() => Console.WriteLine("Yellow light timer expired"));
        
        // Subscribe to transition events
        _stateMachine.OnTransition += (sender, args) =>
        {
            Console.WriteLine($"Transition: {args.FromState} -> {args.ToState} (Trigger: {args.Trigger})");
        };
    }
    
    public void Tick()
    {
        if (_stateMachine.CanFire(TrafficLightExample.Trigger.TimerExpired))
        {
            _stateMachine.Fire(TrafficLightExample.Trigger.TimerExpired);
        }
    }
}
