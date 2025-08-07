# CallerMemberName and CallerFilePath Examples

## 🎯 **How They Work - Step by Step**

### **Example 1: JobRunner.cs calling Entering()**

**File: `c:\agent\src\Agent.Worker\JobRunner.cs`**
```csharp
public class JobRunner : AgentService, IJobRunner
{
    public async Task<TaskResult> RunAsync(...)  // <- This is the calling method
    {
        Trace.Entering();  // <- This calls our enhanced Entering() method
        
        // ... work happens here ...
        
        Trace.Leaving();
    }
    
    private async Task ProcessSteps()  // <- Another calling method
    {
        Trace.Entering();  // <- Another call to Entering()
        
        // ... work happens here ...
        
        Trace.Leaving();
    }
}
```

### **What Happens at Compile Time:**

When the compiler sees `Trace.Entering()`, it automatically transforms it to:

```csharp
// Original call:
Trace.Entering();

// Compiler transforms to:
Trace.Entering(
    name: "RunAsync",                                           // [CallerMemberName]
    filePath: "c:\\agent\\src\\Agent.Worker\\JobRunner.cs"    // [CallerFilePath]
);
```

### **In Our Enhanced Entering() Method:**

```csharp
public void Entering([CallerMemberName] string name = "", [CallerFilePath] string filePath = "")
{
    // name = "RunAsync" (automatically injected)
    // filePath = "c:\\agent\\src\\Agent.Worker\\JobRunner.cs" (automatically injected)
    
    var component = ExtractComponentFromFilePath(filePath);  // Returns "JOBRUNNER"
    var message = BuildStructuredMessage(component, _defaultPhase, _currentCorrelationId, name, $"Entering {name}");
    // message = "[JOBRUNNER] [General] [AGENT-DEFAULT][RunAsync] Entering RunAsync"
    
    Trace(TraceEventType.Verbose, message);
}
```

## 🔍 **Real Examples Across Different Files**

### **Example 2: TaskRunner.cs**
```csharp
// File: c:\agent\src\Agent.Worker\TaskRunner.cs
public class TaskRunner
{
    public async Task<TaskResult> ExecuteTask()
    {
        Trace.Entering();  // Compiler injects: name="ExecuteTask", filePath="...\TaskRunner.cs"
        // Output: [TASKRUNNER] [General] [AGENT-DEFAULT][ExecuteTask] Entering ExecuteTask
    }
    
    private void DownloadArtifacts() 
    {
        Trace.Entering();  // Compiler injects: name="DownloadArtifacts", filePath="...\TaskRunner.cs"
        // Output: [TASKRUNNER] [General] [AGENT-DEFAULT][DownloadArtifacts] Entering DownloadArtifacts
    }
}
```

### **Example 3: StepsRunner.cs**
```csharp
// File: c:\agent\src\Agent.Worker\StepsRunner.cs
public class StepsRunner
{
    public async Task RunAsync()
    {
        Trace.Entering();  // Compiler injects: name="RunAsync", filePath="...\StepsRunner.cs"
        // Output: [STEPSRUNNER] [General] [AGENT-DEFAULT][RunAsync] Entering RunAsync
    }
}
```

### **Example 4: Agent.Listener files**
```csharp
// File: c:\agent\src\Agent.Listener\MessageListener.cs
public class MessageListener
{
    public void StartListening()
    {
        Trace.Entering();  // Compiler injects: name="StartListening", filePath="...\MessageListener.cs"
        // Output: [LISTENER] [General] [AGENT-DEFAULT][StartListening] Entering StartListening
    }
}
```

## 🛠️ **How Our Component Detection Works**

### **ExtractComponentFromFilePath() Logic:**
```csharp
private string ExtractComponentFromFilePath(string filePath)
{
    // filePath = "c:\\agent\\src\\Agent.Worker\\JobRunner.cs"
    
    var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath).ToUpperInvariant();
    // fileName = "JOBRUNNER"
    
    if (fileName.Contains("JOBRUNNER")) return "JOBRUNNER";     // ✅ Matches!
    if (fileName.Contains("TASKRUNNER")) return "TASKRUNNER";   
    if (fileName.Contains("STEPSRUNNER")) return "STEPSRUNNER"; 
    // ... more patterns ...
    
    return fileName; // Fallback
}
```

## 📊 **Complete Flow Example**

**Step-by-step for a call from JobRunner.cs:**

1. **Source Code:**
   ```csharp
   // In JobRunner.cs, line 45
   public async Task<TaskResult> RunAsync()
   {
       Trace.Entering();  // <- Developer writes this
   }
   ```

2. **Compiler Magic:**
   ```csharp
   // Compiler automatically transforms to:
   Trace.Entering("RunAsync", "c:\\agent\\src\\Agent.Worker\\JobRunner.cs");
   ```

3. **Our Enhanced Method Processes:**
   ```csharp
   public void Entering(string name = "RunAsync", string filePath = "c:\\agent\\src\\Agent.Worker\\JobRunner.cs")
   {
       var component = ExtractComponentFromFilePath(filePath);  // "JOBRUNNER"
       var message = BuildStructuredMessage("JOBRUNNER", "General", "AGENT-DEFAULT", "RunAsync", "Entering RunAsync");
       // Result: "[JOBRUNNER] [General] [AGENT-DEFAULT][RunAsync] Entering RunAsync"
   }
   ```

4. **Final Log Output:**
   ```
   [JOBRUNNER] [General] [AGENT-DEFAULT][RunAsync] Entering RunAsync
   ```

## ✨ **Key Benefits**

1. **Zero Manual Work**: Developer just writes `Trace.Entering()`
2. **Automatic Context**: Component and method name detected automatically  
3. **Compile-Time**: No runtime performance cost for detection
4. **Accurate**: Always gets the actual calling method and file
5. **Maintenance-Free**: If method is renamed, logs update automatically

## 🚫 **What Happens Without These Attributes**

**Old way (manual):**
```csharp
Trace.Info("Entering RunAsync");  // Have to type method name manually
```

**Problems:**
- ❌ Easy to forget to update when method renamed
- ❌ Typos in method names  
- ❌ No component context
- ❌ Manual maintenance

**New way (automatic):**
```csharp
Trace.Entering();  // Automatic method name + component detection
```

**Benefits:**
- ✅ Always accurate method name
- ✅ Automatic component detection  
- ✅ Zero maintenance
- ✅ Rich structured format

---

**The compiler attributes make our enhanced logging completely automatic and maintenance-free! 🎉**
