# StateMachinePatterns

State machine patterns implementation in C#/.NET demonstrating various approaches to managing state in object-oriented systems.

## Overview

This repository contains three different state machine pattern implementations, each suitable for different use cases:

1. **State Pattern** - Classic Gang of Four behavioral design pattern
2. **State Machine with Transitions** - Explicit state machine with configurable transitions
3. **Hierarchical State Machine** - Nested states with event bubbling

## Patterns Implemented

### 1. State Pattern

The classic State Pattern encapsulates state-specific behavior into separate state classes. The context delegates behavior to the current state object.

**When to Use:**
- Different states have significantly different behaviors
- State transitions are relatively simple
- You want to eliminate large conditional statements

**Example:** Document Workflow
```csharp
var document = new Document("Project Proposal");
document.Edit();      // Editing in Draft state
document.Submit();    // Transition to Moderation state
document.Approve();   // Transition to Approved state
document.Publish();   // Transition to Published state
```

### 2. State Machine with Transitions

Explicit state machine where you configure allowed transitions between states. Includes entry/exit actions and transition validation.

**When to Use:**
- Complex transition rules between states
- Need to validate transitions before they occur
- Want to log or track all state changes
- Entry/exit actions are important

**Example:** Traffic Light
```csharp
var stateMachine = new StateMachine<LightState, Trigger>(LightState.Red);

stateMachine.Configure(LightState.Red)
    .Permit(Trigger.TimerExpired, LightState.Green)
    .OnEntry(() => Console.WriteLine("Stop!"));

stateMachine.Fire(Trigger.TimerExpired); // Transition to Green
```

### 3. Hierarchical State Machine

States can contain sub-states, allowing for nested state machines. Events bubble up from child to parent if not handled.

**When to Use:**
- States have sub-states or nested behavior
- Need event bubbling from child to parent states
- Complex state hierarchies
- Want to reuse common behavior across related states

**Example:** Vending Machine
```csharp
var vendingMachine = new VendingMachine();
vendingMachine.Initialize();
vendingMachine.HandleEvent("InsertMoney");
vendingMachine.HandleEvent("SelectProduct");
vendingMachine.HandleEvent("Dispense");
```

## Project Structure

```
StateMachinePatterns/
├── StateMachinePatterns.Core/         # Core library with pattern implementations
│   ├── IState.cs                      # State Pattern interface
│   ├── StateContext.cs                # State Pattern context base class
│   ├── StateMachine.cs                # State Machine with transitions
│   └── HierarchicalState.cs           # Hierarchical state machine
├── StateMachinePatterns.Examples/     # Example implementations
│   ├── DocumentWorkflowExample.cs     # State Pattern example
│   ├── TrafficLightExample.cs         # State Machine example
│   └── VendingMachineExample.cs       # Hierarchical State Machine example
└── StateMachinePatterns.Tests/        # Unit tests
    ├── StatePatternTests.cs
    ├── StateMachineTests.cs
    └── HierarchicalStateMachineTests.cs
```

## Getting Started

### Prerequisites
- .NET 9.0 SDK or later

### Building the Project

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Running Examples

```bash
dotnet run --project StateMachinePatterns.Examples
```

## Usage Examples

### State Pattern

```csharp
// Define your context
public class MyContext : StateContext<MyContext>
{
    public void DoSomething()
    {
        Execute(); // Delegates to current state
    }
}

// Define your states
public class StateA : IState<MyContext>
{
    public void OnEnter(MyContext context) { /* ... */ }
    public void OnExit(MyContext context) { /* ... */ }
    public void Execute(MyContext context) { /* ... */ }
}

// Use it
var context = new MyContext();
context.TransitionTo(new StateA());
context.DoSomething();
```

### State Machine

```csharp
// Define your enums
public enum State { Idle, Running, Stopped }
public enum Trigger { Start, Stop }

// Configure the state machine
var sm = new StateMachine<State, Trigger>(State.Idle);

sm.Configure(State.Idle)
    .Permit(Trigger.Start, State.Running)
    .OnEntry(() => Console.WriteLine("Ready"));

sm.Configure(State.Running)
    .Permit(Trigger.Stop, State.Stopped)
    .OnEntry(() => Console.WriteLine("Started"));

// Use it
sm.Fire(Trigger.Start);  // Idle -> Running
sm.Fire(Trigger.Stop);   // Running -> Stopped
```

### Hierarchical State Machine

```csharp
// Define your states
public class RootState : HierarchicalState
{
    public override string Name => "Root";
    
    public override bool HandleEvent(string eventName)
    {
        // Handle event or bubble to parent
        return base.HandleEvent(eventName);
    }
}

// Use it
var hsm = new HierarchicalStateMachine();
hsm.Initialize(new RootState());
hsm.HandleEvent("SomeEvent");
```

## Key Features

- ✅ Three different state machine patterns
- ✅ Comprehensive unit tests (18 tests, 100% pass rate)
- ✅ Real-world examples
- ✅ Type-safe state transitions
- ✅ Entry/Exit actions
- ✅ Event handling and bubbling
- ✅ Transition validation
- ✅ Clean, documented code

## Design Principles

- **Single Responsibility:** Each state class handles only its own behavior
- **Open/Closed:** Easy to add new states without modifying existing code
- **Type Safety:** Strongly-typed state and trigger enums
- **Separation of Concerns:** State logic separated from context
- **Testability:** All patterns are easily unit testable

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Feel free to submit issues or pull requests.

## References

- [State Pattern - Gang of Four](https://en.wikipedia.org/wiki/State_pattern)
- [Stateless (C# State Machine Library)](https://github.com/dotnet-state-machine/stateless)
- [Hierarchical State Machines](https://en.wikipedia.org/wiki/UML_state_machine#Hierarchically_nested_states)
 
