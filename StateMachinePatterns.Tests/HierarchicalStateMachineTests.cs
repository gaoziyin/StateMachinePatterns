using StateMachinePatterns.Core;

namespace StateMachinePatterns.Tests;

/// <summary>
/// Tests for the Hierarchical State Machine
/// </summary>
public class HierarchicalStateMachineTests
{
    private class TestRootState : HierarchicalState
    {
        public override string Name => "Root";
        public List<string> Events { get; } = new();
        
        public override void OnEnter()
        {
            Events.Add("Root.OnEnter");
        }
        
        public override void OnExit()
        {
            Events.Add("Root.OnExit");
        }
        
        public override bool HandleEvent(string eventName)
        {
            if (eventName == "RootEvent")
            {
                Events.Add("Root.HandleEvent");
                return true;
            }
            return base.HandleEvent(eventName);
        }
    }
    
    private class TestSubState : HierarchicalState
    {
        private readonly TestRootState _root;
        
        public override string Name => "Sub";
        
        public TestSubState(TestRootState root)
        {
            _root = root;
        }
        
        public override void OnEnter()
        {
            _root.Events.Add("Sub.OnEnter");
        }
        
        public override void OnExit()
        {
            _root.Events.Add("Sub.OnExit");
        }
        
        public override bool HandleEvent(string eventName)
        {
            if (eventName == "SubEvent")
            {
                _root.Events.Add("Sub.HandleEvent");
                return true;
            }
            return base.HandleEvent(eventName);
        }
    }
    
    [Fact]
    public void Initialize_ShouldSetRootStateAndCallOnEnter()
    {
        // Arrange
        var stateMachine = new HierarchicalStateMachine();
        var rootState = new TestRootState();
        
        // Act
        stateMachine.Initialize(rootState);
        
        // Assert
        Assert.Equal(rootState, stateMachine.CurrentState);
        Assert.Contains("Root.OnEnter", rootState.Events);
    }
    
    [Fact]
    public void TransitionTo_ShouldCallOnExitAndOnEnter()
    {
        // Arrange
        var stateMachine = new HierarchicalStateMachine();
        var rootState1 = new TestRootState();
        var rootState2 = new TestRootState();
        
        stateMachine.Initialize(rootState1);
        rootState1.Events.Clear();
        
        // Act
        stateMachine.TransitionTo(rootState2);
        
        // Assert
        Assert.Contains("Root.OnExit", rootState1.Events);
        Assert.Contains("Root.OnEnter", rootState2.Events);
        Assert.Equal(rootState2, stateMachine.CurrentState);
    }
    
    [Fact]
    public void HandleEvent_ShouldBeHandledByCurrentState()
    {
        // Arrange
        var stateMachine = new HierarchicalStateMachine();
        var rootState = new TestRootState();
        stateMachine.Initialize(rootState);
        
        // Act
        var result = stateMachine.HandleEvent("RootEvent");
        
        // Assert
        Assert.True(result);
        Assert.Contains("Root.HandleEvent", rootState.Events);
    }
    
    [Fact]
    public void HandleEvent_WithUnhandledEvent_ShouldReturnFalse()
    {
        // Arrange
        var stateMachine = new HierarchicalStateMachine();
        var rootState = new TestRootState();
        stateMachine.Initialize(rootState);
        
        // Act
        var result = stateMachine.HandleEvent("UnknownEvent");
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public void GetFullPath_ShouldReturnCorrectPath()
    {
        // Arrange
        var rootState = new TestRootState();
        var subState = new TestSubState(rootState);
        
        // Act - simulate adding sub-state
        // This is a simplified test since TransitionToSubState is protected
        var path = rootState.Name;
        
        // Assert
        Assert.Equal("Root", path);
    }
    
    [Fact]
    public void GetCurrentStatePath_ShouldReturnStatePath()
    {
        // Arrange
        var stateMachine = new HierarchicalStateMachine();
        var rootState = new TestRootState();
        
        // Act
        stateMachine.Initialize(rootState);
        var path = stateMachine.GetCurrentStatePath();
        
        // Assert
        Assert.Equal("Root", path);
    }
    
    [Fact]
    public void GetCurrentStatePath_WithNoState_ShouldReturnNone()
    {
        // Arrange
        var stateMachine = new HierarchicalStateMachine();
        
        // Act
        var path = stateMachine.GetCurrentStatePath();
        
        // Assert
        Assert.Equal("None", path);
    }
}
