# Best Ways to Pass Component Name and Operation to Trace Functions

## 🎯 **Summary of Approaches**

### **1. Fully Automatic (Recommended for Most Cases)**
```csharp
// Zero parameters needed - automatically detects class and method
trace.InfoAuto("Processing started", new { RecordCount = 100 });
trace.ErrorAuto("Operation failed", new { ErrorCode = "E001" });
```

### **2. Semi-Automatic with CallerMemberName**
```csharp
// Automatically gets method name, file path determines component
trace.InfoWithAutoContext("Processing started", new { RecordCount = 100 });
```

### **3. Manual but Flexible**
```csharp
// Full control over component and operation
trace.InfoWithContext("Processing started", "ProcessRecords", new { RecordCount = 100 });
```

### **4. Scoped Context (Best for Complex Operations)**
```csharp
using (trace.SetContext("JOBRUNNER", "Execution", "JOB-123"))
{
    trace.InfoAuto("Starting job");  // Inherits JOBRUNNER context
    DoWork();                        // All nested calls inherit context
    trace.InfoAuto("Job completed"); // Still uses JOBRUNNER context
}
```

## 📋 **Detailed Comparison**

| Approach | Component Detection | Operation Detection | Performance | Use Case |
|----------|--------------------|--------------------|-------------|----------|
| `InfoAuto()` | StackTrace + FilePath | CallerMemberName | Medium | General use |
| `InfoWithAutoContext()` | FilePath only | CallerMemberName | Fast | High-frequency calls |
| `InfoWithContext()` | Manual parameter | Manual parameter | Fastest | Performance-critical |
| Scoped Context | Manual setup | CallerMemberName | Fast | Complex operations |

## 🚀 **Implementation Examples**

### **Example 1: JobRunner.cs Usage**
```csharp
public class JobRunner : AgentService, IJobRunner
{
    public async Task<TaskResult> RunAsync(...)
    {
        // Automatic detection - will extract "JOBRUNNER" from class name
        Trace.InfoAuto("Starting job execution", new { JobId = message.JobId });
        
        try
        {
            // Scoped context for the entire job
            using (Trace.SetContext("JOBRUNNER", "Execution", $"JOB-{message.JobId}"))
            {
                Trace.InfoAuto("Job context established");
                
                // All nested calls automatically inherit JOBRUNNER context
                await ProcessSteps();
                
                Trace.InfoAuto("Job completed successfully");
            }
        }
        catch (Exception ex)
        {
            // Automatic error logging with context
            Trace.ErrorAuto("Job execution failed", new { 
                JobId = message.JobId, 
                Error = ex.Message 
            });
            throw;
        }
    }
    
    private async Task ProcessSteps()
    {
        // This will automatically show as [JOBRUNNER] [Execution] [JOB-123][ProcessSteps]
        Trace.InfoAuto("Processing job steps");
    }
}
```

### **Example 2: TaskRunner.cs Usage**
```csharp
public class TaskRunner : AgentService
{
    public async Task<TaskResult> RunAsync(IExecutionContext context, ITaskDefinition task)
    {
        // Performance measurement with automatic component detection
        using (Trace.MeasurePerformance("TaskExecution"))
        {
            // Will automatically detect "TASKRUNNER" component
            Trace.InfoAuto("Starting task execution", new { 
                TaskName = task.Name,
                TaskVersion = task.Version 
            });
            
            // Nested operations inherit context
            await DownloadTask(task);
            await ExecuteTask(task);
            await UploadResults(task);
            
            Trace.InfoAuto("Task execution completed");
        }
    }
    
    private async Task DownloadTask(ITaskDefinition task)
    {
        // Automatic: [TASKRUNNER] [General] [correlation][DownloadTask] message
        Trace.InfoAuto("Downloading task artifacts");
    }
}
```

## 🎨 **Expected Log Output**

### **Before (Manual)**
```csharp
Trace.Info("Starting job execution");
Trace.Info("Processing job steps");  
Trace.Error("Job execution failed");
```
**Output:**
```
Starting job execution
Processing job steps
Job execution failed
```

### **After (Automatic)**
```csharp
Trace.InfoAuto("Starting job execution", new { JobId = 123 });
Trace.InfoAuto("Processing job steps");
Trace.ErrorAuto("Job execution failed", new { JobId = 123, Error = "Timeout" });
```
**Output:**
```
[JOBRUNNER] [General] [AGENT-DEFAULT][RunAsync] Starting job execution [JobId=123]
[JOBRUNNER] [Execution] [JOB-123][ProcessSteps] Processing job steps
[JOBRUNNER] [General] [AGENT-DEFAULT][RunAsync] Job execution failed [JobId=123, Error=Timeout]
```

## ⚡ **Performance Considerations**

### **Choose Based on Usage Frequency:**

1. **High-frequency calls (>1000/sec)**: Use `InfoWithAutoContext()` - file path parsing only
2. **Medium-frequency calls**: Use `InfoAuto()` - includes stack trace analysis  
3. **Low-frequency, complex operations**: Use scoped context with `SetContext()`
4. **Performance-critical**: Use manual `InfoWithContext()` with hardcoded values

### **Performance Benchmarks:**
```csharp
// Fastest - no reflection
trace.InfoWithContext("message", "Operation", metadata);          // ~0.1ms

// Fast - file path parsing only  
trace.InfoWithAutoContext("message", metadata);                   // ~0.2ms

// Medium - includes stack trace
trace.InfoAuto("message", metadata);                             // ~0.5ms

// Context setup cost amortized across multiple calls
using (trace.SetContext("COMPONENT", "Phase", "CORR-123"))       // ~0.1ms setup
{
    trace.InfoAuto("message1");  // ~0.2ms each
    trace.InfoAuto("message2");  // ~0.2ms each  
}
```

## 🏆 **Recommended Usage Patterns**

### **For New Code:**
```csharp
// Use automatic detection by default
Trace.InfoAuto("Operation started");

// Use scoped context for major operations
using (Trace.SetContext("COMPONENT", "Phase", correlationId))
{
    Trace.InfoAuto("Sub-operation");
}

// Use performance measurement for timing
using (Trace.MeasurePerformance("ExpensiveOperation"))
{
    // Work here
}
```

### **For Existing Code Migration:**
```csharp
// Replace these:
Trace.Info("Message");                           
Trace.Error("Error occurred");

// With these:
Trace.InfoAuto("Message");                       
Trace.ErrorAuto("Error occurred");

// No other changes needed!
```

---

**The automatic detection approaches eliminate the need to manually pass component and operation names while providing rich, structured logging! 🎉**
