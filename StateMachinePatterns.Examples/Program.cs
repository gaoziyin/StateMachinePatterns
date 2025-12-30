using StateMachinePatterns.Examples;

Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
Console.WriteLine("║       State Machine Patterns in C#/.NET Examples         ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Run Document Workflow Example (State Pattern)
DocumentWorkflowExample.Run();
Console.WriteLine("\n" + new string('=', 60) + "\n");

// Run Traffic Light Example (State Machine with Transitions)
TrafficLightExample.Run();
Console.WriteLine("\n" + new string('=', 60) + "\n");

// Run Vending Machine Example (Hierarchical State Machine)
VendingMachineExample.Run();

Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
Console.WriteLine("║                    Examples Complete                      ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
