using StateMachinePatterns.Core;

namespace StateMachinePatterns.Examples;

/// <summary>
/// Example: Document workflow using the State Pattern.
/// Demonstrates how different states can have different behaviors.
/// </summary>
public class DocumentWorkflowExample
{
    public static void Run()
    {
        Console.WriteLine("=== Document Workflow Example (State Pattern) ===\n");
        
        var document = new Document("Project Proposal");
        
        Console.WriteLine($"Initial state: {document.GetStateName()}");
        document.Render();
        
        Console.WriteLine("\n--- Editing the document ---");
        document.Edit();
        
        Console.WriteLine("\n--- Submitting for review ---");
        document.Submit();
        
        Console.WriteLine("\n--- Attempting to edit (should fail) ---");
        document.Edit();
        
        Console.WriteLine("\n--- Approving the document ---");
        document.Approve();
        
        Console.WriteLine("\n--- Publishing the document ---");
        document.Publish();
        
        Console.WriteLine($"\nFinal state: {document.GetStateName()}");
    }
}

public class Document : StateContext<Document>
{
    public string Title { get; }
    public string Content { get; set; } = string.Empty;
    
    public Document(string title)
    {
        Title = title;
        TransitionTo(new DraftState());
    }
    
    public void Edit()
    {
        Execute();
    }
    
    public void Submit()
    {
        if (CurrentState is DraftState)
        {
            TransitionTo(new ModerationState());
        }
        else
        {
            Console.WriteLine("Cannot submit from current state");
        }
    }
    
    public void Approve()
    {
        if (CurrentState is ModerationState)
        {
            TransitionTo(new ApprovedState());
        }
        else
        {
            Console.WriteLine("Cannot approve from current state");
        }
    }
    
    public void Reject()
    {
        if (CurrentState is ModerationState)
        {
            TransitionTo(new DraftState());
        }
        else
        {
            Console.WriteLine("Cannot reject from current state");
        }
    }
    
    public void Publish()
    {
        if (CurrentState is ApprovedState)
        {
            TransitionTo(new PublishedState());
        }
        else
        {
            Console.WriteLine("Cannot publish from current state");
        }
    }
    
    public void Render()
    {
        Console.WriteLine($"Rendering document: {Title}");
    }
    
    public string GetStateName()
    {
        return CurrentState?.GetType().Name.Replace("State", "") ?? "Unknown";
    }
}

public class DraftState : IState<Document>
{
    public void OnEnter(Document context)
    {
        Console.WriteLine("Document is now in Draft state - can be edited");
    }
    
    public void OnExit(Document context)
    {
        Console.WriteLine("Leaving Draft state");
    }
    
    public void Execute(Document context)
    {
        Console.WriteLine("Editing document content...");
        context.Content += " [Edited content] ";
    }
}

public class ModerationState : IState<Document>
{
    public void OnEnter(Document context)
    {
        Console.WriteLine("Document submitted for moderation - editing is locked");
    }
    
    public void OnExit(Document context)
    {
        Console.WriteLine("Leaving Moderation state");
    }
    
    public void Execute(Document context)
    {
        Console.WriteLine("Document is under review - cannot edit!");
    }
}

public class ApprovedState : IState<Document>
{
    public void OnEnter(Document context)
    {
        Console.WriteLine("Document has been approved");
    }
    
    public void OnExit(Document context)
    {
        Console.WriteLine("Leaving Approved state");
    }
    
    public void Execute(Document context)
    {
        Console.WriteLine("Document is approved - ready for publishing");
    }
}

public class PublishedState : IState<Document>
{
    public void OnEnter(Document context)
    {
        Console.WriteLine("Document is now published - immutable");
    }
    
    public void OnExit(Document context)
    {
        Console.WriteLine("Leaving Published state");
    }
    
    public void Execute(Document context)
    {
        Console.WriteLine("Document is published - no changes allowed");
    }
}
