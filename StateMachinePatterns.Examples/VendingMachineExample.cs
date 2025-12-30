using StateMachinePatterns.Core;

namespace StateMachinePatterns.Examples;

/// <summary>
/// Example: Vending Machine using hierarchical state machine.
/// Demonstrates nested states and event bubbling.
/// </summary>
public class VendingMachineExample
{
    public static void Run()
    {
        Console.WriteLine("=== Vending Machine Example (Hierarchical State Machine) ===\n");
        
        var vendingMachine = new VendingMachine();
        vendingMachine.Initialize();
        
        Console.WriteLine($"Initial state: {vendingMachine.GetCurrentState()}\n");
        
        // Try to dispense without money
        Console.WriteLine("--- Attempting to dispense without inserting money ---");
        vendingMachine.HandleEvent("Dispense");
        
        // Insert money
        Console.WriteLine("\n--- Inserting money ---");
        vendingMachine.HandleEvent("InsertMoney");
        
        Console.WriteLine($"\nCurrent state: {vendingMachine.GetCurrentState()}");
        
        // Select a product
        Console.WriteLine("\n--- Selecting product ---");
        vendingMachine.HandleEvent("SelectProduct");
        
        // Dispense
        Console.WriteLine("\n--- Dispensing product ---");
        vendingMachine.HandleEvent("Dispense");
        
        Console.WriteLine($"\nFinal state: {vendingMachine.GetCurrentState()}");
    }
}

public class VendingMachine
{
    private readonly HierarchicalStateMachine _stateMachine = new();
    
    public void Initialize()
    {
        _stateMachine.Initialize(new IdleState(this));
    }
    
    public bool HandleEvent(string eventName)
    {
        return _stateMachine.HandleEvent(eventName);
    }
    
    public string GetCurrentState()
    {
        return _stateMachine.GetCurrentStatePath();
    }
    
    // Internal method for states to transition
    internal void TransitionTo(HierarchicalState newState)
    {
        _stateMachine.TransitionTo(newState);
    }
    
    // State classes
    private class IdleState : HierarchicalState
    {
        private readonly VendingMachine _machine;
        
        public override string Name => "Idle";
        
        public IdleState(VendingMachine machine)
        {
            _machine = machine;
        }
        
        public override void OnEnter()
        {
            Console.WriteLine("Machine is idle - waiting for money");
        }
        
        public override bool HandleEvent(string eventName)
        {
            if (eventName == "InsertMoney")
            {
                Console.WriteLine("Money inserted!");
                _machine.TransitionTo(new HasMoneyState(_machine));
                return true;
            }
            else if (eventName == "Dispense")
            {
                Console.WriteLine("Please insert money first!");
                return true;
            }
            
            return base.HandleEvent(eventName);
        }
    }
    
    private class HasMoneyState : HierarchicalState
    {
        private readonly VendingMachine _machine;
        
        public override string Name => "HasMoney";
        
        public HasMoneyState(VendingMachine machine)
        {
            _machine = machine;
            TransitionToSubState(new WaitingForSelectionState(_machine));
        }
        
        public override void OnEnter()
        {
            Console.WriteLine("Machine has money - ready for selection");
        }
        
        public override bool HandleEvent(string eventName)
        {
            if (eventName == "Cancel")
            {
                Console.WriteLine("Transaction cancelled - returning money");
                _machine.TransitionTo(new IdleState(_machine));
                return true;
            }
            
            return base.HandleEvent(eventName);
        }
    }
    
    private class WaitingForSelectionState : HierarchicalState
    {
        private readonly VendingMachine _machine;
        
        public override string Name => "WaitingForSelection";
        
        public WaitingForSelectionState(VendingMachine machine)
        {
            _machine = machine;
        }
        
        public override void OnEnter()
        {
            Console.WriteLine("Waiting for product selection...");
        }
        
        public override bool HandleEvent(string eventName)
        {
            if (eventName == "SelectProduct")
            {
                Console.WriteLine("Product selected!");
                TransitionToSibling(new DispensingState(_machine));
                return true;
            }
            
            return base.HandleEvent(eventName);
        }
    }
    
    private class DispensingState : HierarchicalState
    {
        private readonly VendingMachine _machine;
        
        public override string Name => "Dispensing";
        
        public DispensingState(VendingMachine machine)
        {
            _machine = machine;
        }
        
        public override void OnEnter()
        {
            Console.WriteLine("Preparing to dispense...");
        }
        
        public override bool HandleEvent(string eventName)
        {
            if (eventName == "Dispense")
            {
                Console.WriteLine("Dispensing product... Enjoy!");
                _machine.TransitionTo(new IdleState(_machine));
                return true;
            }
            
            return base.HandleEvent(eventName);
        }
    }
}
