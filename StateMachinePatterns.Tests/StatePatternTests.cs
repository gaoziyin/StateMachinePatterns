using StateMachinePatterns.Core;

namespace StateMachinePatterns.Tests;

/// <summary>
/// Tests for the State Pattern implementation
/// </summary>
public class StatePatternTests
{
    private class TestContext : StateContext<TestContext>
    {
        public List<string> Events { get; } = new();
        
        public void AddEvent(string eventName)
        {
            Events.Add(eventName);
        }
    }
    
    private class StateA : IState<TestContext>
    {
        public void OnEnter(TestContext context)
        {
            context.AddEvent("StateA.OnEnter");
        }
        
        public void OnExit(TestContext context)
        {
            context.AddEvent("StateA.OnExit");
        }
        
        public void Execute(TestContext context)
        {
            context.AddEvent("StateA.Execute");
        }
    }
    
    private class StateB : IState<TestContext>
    {
        public void OnEnter(TestContext context)
        {
            context.AddEvent("StateB.OnEnter");
        }
        
        public void OnExit(TestContext context)
        {
            context.AddEvent("StateB.OnExit");
        }
        
        public void Execute(TestContext context)
        {
            context.AddEvent("StateB.Execute");
        }
    }
    
    [Fact]
    public void TransitionTo_ShouldCallOnExitAndOnEnter()
    {
        // Arrange
        var context = new TestContext();
        var stateA = new StateA();
        var stateB = new StateB();
        
        // Act
        context.TransitionTo(stateA);
        context.Events.Clear(); // Clear the OnEnter from initial transition
        context.TransitionTo(stateB);
        
        // Assert
        Assert.Contains("StateA.OnExit", context.Events);
        Assert.Contains("StateB.OnEnter", context.Events);
        Assert.Equal(stateB, context.CurrentState);
    }
    
    [Fact]
    public void Execute_ShouldCallCurrentStateExecute()
    {
        // Arrange
        var context = new TestContext();
        var stateA = new StateA();
        
        // Act
        context.TransitionTo(stateA);
        context.Events.Clear();
        context.Execute();
        
        // Assert
        Assert.Contains("StateA.Execute", context.Events);
    }
    
    [Fact]
    public void CurrentState_ShouldReturnCorrectState()
    {
        // Arrange
        var context = new TestContext();
        var stateA = new StateA();
        var stateB = new StateB();
        
        // Act & Assert
        Assert.Null(context.CurrentState);
        
        context.TransitionTo(stateA);
        Assert.Equal(stateA, context.CurrentState);
        
        context.TransitionTo(stateB);
        Assert.Equal(stateB, context.CurrentState);
    }
}
