using StateMachinePatterns.Core;

namespace StateMachinePatterns.Tests;

/// <summary>
/// Tests for the State Machine with explicit transitions
/// </summary>
public class StateMachineTests
{
    private enum TestState
    {
        Off,
        Starting,
        Running,
        Stopping
    }
    
    private enum TestTrigger
    {
        TurnOn,
        Start,
        Stop,
        TurnOff
    }
    
    [Fact]
    public void Constructor_ShouldSetInitialState()
    {
        // Arrange & Act
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Off);
        
        // Assert
        Assert.Equal(TestState.Off, sm.CurrentState);
    }
    
    [Fact]
    public void Fire_WithValidTransition_ShouldTransitionToNewState()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Off);
        sm.Configure(TestState.Off)
            .Permit(TestTrigger.TurnOn, TestState.Starting);
        
        // Act
        var result = sm.Fire(TestTrigger.TurnOn);
        
        // Assert
        Assert.True(result);
        Assert.Equal(TestState.Starting, sm.CurrentState);
    }
    
    [Fact]
    public void Fire_WithInvalidTransition_ShouldReturnFalse()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Off);
        sm.Configure(TestState.Off)
            .Permit(TestTrigger.TurnOn, TestState.Starting);
        
        // Act
        var result = sm.Fire(TestTrigger.Start); // Not configured
        
        // Assert
        Assert.False(result);
        Assert.Equal(TestState.Off, sm.CurrentState);
    }
    
    [Fact]
    public void CanFire_WithValidTrigger_ShouldReturnTrue()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Off);
        sm.Configure(TestState.Off)
            .Permit(TestTrigger.TurnOn, TestState.Starting);
        
        // Act & Assert
        Assert.True(sm.CanFire(TestTrigger.TurnOn));
        Assert.False(sm.CanFire(TestTrigger.Start));
    }
    
    [Fact]
    public void OnEntry_ShouldBeCalledWhenEnteringState()
    {
        // Arrange
        var entryCallCount = 0;
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Off);
        sm.Configure(TestState.Off)
            .Permit(TestTrigger.TurnOn, TestState.Starting);
        sm.Configure(TestState.Starting)
            .OnEntry(() => entryCallCount++);
        
        // Act
        sm.Fire(TestTrigger.TurnOn);
        
        // Assert
        Assert.Equal(1, entryCallCount);
    }
    
    [Fact]
    public void OnExit_ShouldBeCalledWhenExitingState()
    {
        // Arrange
        var exitCallCount = 0;
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Off);
        sm.Configure(TestState.Off)
            .Permit(TestTrigger.TurnOn, TestState.Starting)
            .OnExit(() => exitCallCount++);
        
        // Act
        sm.Fire(TestTrigger.TurnOn);
        
        // Assert
        Assert.Equal(1, exitCallCount);
    }
    
    [Fact]
    public void OnTransition_ShouldBeRaisedWhenTransitionOccurs()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Off);
        sm.Configure(TestState.Off)
            .Permit(TestTrigger.TurnOn, TestState.Starting);
        
        TestState? fromState = null;
        TestState? toState = null;
        TestTrigger? trigger = null;
        
        sm.OnTransition += (sender, args) =>
        {
            fromState = args.FromState;
            toState = args.ToState;
            trigger = args.Trigger;
        };
        
        // Act
        sm.Fire(TestTrigger.TurnOn);
        
        // Assert
        Assert.Equal(TestState.Off, fromState);
        Assert.Equal(TestState.Starting, toState);
        Assert.Equal(TestTrigger.TurnOn, trigger);
    }
    
    [Fact]
    public void ComplexStateMachine_ShouldHandleMultipleTransitions()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Off);
        
        sm.Configure(TestState.Off)
            .Permit(TestTrigger.TurnOn, TestState.Starting);
        
        sm.Configure(TestState.Starting)
            .Permit(TestTrigger.Start, TestState.Running)
            .Permit(TestTrigger.TurnOff, TestState.Off);
        
        sm.Configure(TestState.Running)
            .Permit(TestTrigger.Stop, TestState.Stopping);
        
        sm.Configure(TestState.Stopping)
            .Permit(TestTrigger.TurnOff, TestState.Off)
            .Permit(TestTrigger.Start, TestState.Running);
        
        // Act & Assert
        Assert.Equal(TestState.Off, sm.CurrentState);
        
        sm.Fire(TestTrigger.TurnOn);
        Assert.Equal(TestState.Starting, sm.CurrentState);
        
        sm.Fire(TestTrigger.Start);
        Assert.Equal(TestState.Running, sm.CurrentState);
        
        sm.Fire(TestTrigger.Stop);
        Assert.Equal(TestState.Stopping, sm.CurrentState);
        
        sm.Fire(TestTrigger.TurnOff);
        Assert.Equal(TestState.Off, sm.CurrentState);
    }
}
