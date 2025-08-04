# Component Reference

This page provides detailed technical documentation for each major component in the Azure DevOps Agent architecture.

## 🏗️ Component Overview

### High-Level Component Map

```
Agent.Listener Process (Persistent)
├── MessageListener          # Azure DevOps communication
├── JobDispatcher            # Job routing and worker management
├── AgentServer             # Agent capabilities and status
├── Configuration           # Agent settings and credentials
└── Telemetry              # Metrics and diagnostics

Agent.Worker Process (Per Job)
├── Worker                  # Main orchestration
├── JobRunner              # Job execution management
├── StepsRunner            # Step orchestration
├── TaskRunner             # Individual task execution
├── ExecutionContext       # Logging and state management
└── ProcessChannel         # IPC communication
```

## 📡 Agent.Listener Components

### MessageListener

**Purpose**: Maintains connection to Azure DevOps Services and polls for job messages.

**Location**: `src/Agent.Listener/MessageListener.cs`

**Key Responsibilities**:
- WebSocket/HTTP long polling to Azure DevOps
- Agent capability advertisement
- Authentication token management
- Job message routing to JobDispatcher

**Configuration**:
```csharp
public class MessageListener : AgentService, IMessageListener
{
    private readonly TimeSpan _getNextMessageTimeout = TimeSpan.FromSeconds(60);
    private readonly TimeSpan _keepAliveTimeout = TimeSpan.FromSeconds(30);
    
    // Authentication and connection management
    public async Task CreateSessionAsync(CancellationToken token)
    {
        // Establish authenticated session with Azure DevOps
    }
    
    public async Task<TaskAgentMessage> GetNextMessageAsync(CancellationToken token)
    {
        // Poll for next job message
    }
}
```

**Key Methods**:
- `CreateSessionAsync()` - Establishes authenticated session
- `GetNextMessageAsync()` - Polls for job messages
- `DeleteMessageAsync()` - Acknowledges message processing

**Error Handling**:
- Connection failures: Automatic reconnection with exponential backoff
- Authentication failures: Token refresh and retry
- Rate limiting: Adaptive polling intervals

---

### JobDispatcher

**Purpose**: Routes job messages and manages worker process lifecycle.

**Location**: `src/Agent.Listener/JobDispatcher.cs`

**Key Responsibilities**:
- Worker process creation and management
- Job message routing
- Worker health monitoring
- Resource cleanup after job completion

**Architecture**:
```csharp
public sealed class JobDispatcher : AgentService, IJobDispatcher
{
    // Worker process management
    private readonly ConcurrentDictionary<Guid, WorkerInfo> _workers;
    
    public async Task<TaskResult> DispatchAsync(Pipelines.AgentJobRequestMessage jobMessage)
    {
        // 1. Validate job requirements
        // 2. Create worker process
        // 3. Establish IPC communication
        // 4. Transfer job message
        // 5. Monitor execution
        // 6. Cleanup resources
    }
}
```

**Worker Creation Process**:
```csharp
private async Task<Process> CreateWorkerProcess(Guid jobId)
{
    var processInfo = new ProcessStartInfo
    {
        FileName = GetWorkerExecutablePath(),
        Arguments = $"--pipeIn {pipeIn} --pipeOut {pipeOut}",
        CreateNoWindow = true,
        UseShellExecute = false
    };
    
    var process = Process.Start(processInfo);
    await EstablishCommunication(process.Id, pipeIn, pipeOut);
    return process;
}
```

**Performance Monitoring**:
- Worker creation time tracking
- Memory usage monitoring
- Process health checks
- Communication latency measurement

---

### AgentServer

**Purpose**: Manages agent capabilities, status reporting, and maintenance tasks.

**Location**: `src/Agent.Listener/Agent.cs`

**Key Responsibilities**:
- Agent capability detection and reporting
- Agent status updates to Azure DevOps
- Maintenance task execution (updates, cleanup)
- Agent configuration management

**Capability Management**:
```csharp
public class AgentServer : AgentService, IAgent
{
    public async Task<List<AgentCapability>> GetAgentCapabilitiesAsync()
    {
        var capabilities = new List<AgentCapability>();
        
        // System capabilities
        capabilities.AddRange(await GetSystemCapabilitiesAsync());
        
        // Tool capabilities  
        capabilities.AddRange(await GetToolCapabilitiesAsync());
        
        // User-defined capabilities
        capabilities.AddRange(await GetUserCapabilitiesAsync());
        
        return capabilities;
    }
}
```

## 🔧 Agent.Worker Components

### Worker

**Purpose**: Main orchestration component for job execution in worker process.

**Location**: `src/Agent.Worker/Worker.cs`

**Key Responsibilities**:
- IPC communication with listener
- Job message processing
- Secret masker initialization
- JobRunner coordination
- Result reporting

**Execution Flow**:
```csharp
public async Task<int> RunAsync(string pipeIn, string pipeOut)
{
    // 1. Establish IPC communication
    using (var channel = HostContext.CreateService<IProcessChannel>())
    {
        channel.StartClient(pipeIn, pipeOut);
        
        // 2. Receive job message
        var channelMessage = await channel.ReceiveAsync(cancellationToken);
        var jobMessage = JsonUtility.FromString<AgentJobRequestMessage>(channelMessage.Body);
        
        // 3. Initialize security
        InitializeSecretMasker(jobMessage);
        SetCulture(jobMessage);
        
        // 4. Start job execution
        var jobRunner = HostContext.CreateService<IJobRunner>();
        return await jobRunner.RunAsync(jobMessage, cancellationToken);
    }
}
```

**Secret Masking Initialization**:
```csharp
private void InitializeSecretMasker(AgentJobRequestMessage message)
{
    // Variable secrets
    foreach (var variable in message.Variables ?? new Dictionary<string, VariableValue>())
    {
        if (variable.Value.IsSecret && !string.IsNullOrWhiteSpace(variable.Value.Value))
        {
            AddUserSuppliedSecret(variable.Value.Value);
        }
    }
    
    // Endpoint secrets
    foreach (var endpoint in message.Resources.Endpoints ?? new List<ServiceEndpoint>())
    {
        foreach (var auth in endpoint.Authorization?.Parameters ?? new Dictionary<string, string>())
        {
            if (MaskingUtil.IsEndpointAuthorizationParametersSecret(auth.Key))
            {
                HostContext.SecretMasker.AddValue(auth.Value, $"Endpoint_{auth.Key}");
            }
        }
    }
}
```

---

### JobRunner

**Purpose**: Orchestrates complete job execution including setup, step processing, and cleanup.

**Location**: `src/Agent.Worker/JobRunner.cs`

**Key Responsibilities**:
- Job context initialization
- Environment variable setup
- Working directory management
- Step orchestration via StepsRunner
- Result aggregation and reporting

**Job Execution Phases**:
```csharp
public async Task<TaskResult> RunAsync(AgentJobRequestMessage message, CancellationToken token)
{
    try
    {
        // Phase 1: Initialize job context
        await InitializeJobAsync(message, token);
        
        // Phase 2: Setup environment
        await SetupEnvironmentAsync(message, token);
        
        // Phase 3: Execute steps
        var stepsRunner = HostContext.CreateService<IStepsRunner>();
        await stepsRunner.RunAsync(executionContext, message.Steps);
        
        // Phase 4: Finalize and cleanup
        return await FinalizeJobAsync(token);
    }
    catch (Exception ex)
    {
        // Comprehensive error handling
        return TaskResult.Failed;
    }
}
```

**Environment Setup**:
```csharp
private async Task SetupEnvironmentAsync(AgentJobRequestMessage message, CancellationToken token)
{
    // Working directory
    var workingDirectory = HostContext.GetDirectory(WellKnownDirectory.Work);
    executionContext.Variables.Set(Constants.Variables.Agent.WorkFolder, workingDirectory);
    
    // System variables
    executionContext.Variables.Set(Constants.Variables.System.Culture, GetSystemCulture());
    executionContext.Variables.Set(Constants.Variables.Agent.OS, PlatformUtil.GetOS());
    
    // Job-specific variables
    foreach (var variable in message.Variables)
    {
        executionContext.Variables.Set(variable.Key, variable.Value.Value, variable.Value.IsSecret);
    }
}
```

---

### StepsRunner

**Purpose**: Orchestrates execution of individual steps within a job.

**Location**: `src/Agent.Worker/StepsRunner.cs`

**Key Responsibilities**:
- Step sequencing and dependency management
- Conditional step execution
- Error handling and continuation logic
- Timeline integration for progress reporting

**Step Execution Logic**:
```csharp
public async Task RunAsync(IExecutionContext jobContext, IList<IStep> steps)
{
    foreach (var step in steps)
    {
        // Check conditions
        if (!await EvaluateStepCondition(step, jobContext))
        {
            jobContext.Debug($"Skipping step '{step.DisplayName}' due to condition");
            continue;
        }
        
        // Create step context
        using (var stepContext = jobContext.CreateChild(step.Id, step.DisplayName))
        {
            try
            {
                // Execute step
                await RunStepAsync(stepContext, step);
            }
            catch (Exception ex) when (step.ContinueOnError)
            {
                stepContext.Warning($"Step failed but continuing: {ex.Message}");
                stepContext.Result = TaskResult.SucceededWithIssues;
            }
        }
    }
}
```

**Step Types**:
- **Task Steps**: Execute Azure DevOps tasks
- **Script Steps**: Run inline scripts
- **Action Steps**: Execute GitHub Actions
- **Container Steps**: Run in Docker containers

---

### TaskRunner

**Purpose**: Executes individual Azure DevOps tasks with comprehensive input processing and handler management.

**Location**: `src/Agent.Worker/TaskRunner.cs`

**Key Responsibilities**:
- Task definition loading and validation
- Input parameter processing and variable expansion
- Handler creation and execution
- Output capture and result reporting

**Task Execution Flow**:
```csharp
public async Task RunAsync(IExecutionContext executionContext, ITaskStep taskStep)
{
    try
    {
        // 1. Load task definition
        var taskDefinition = await LoadTaskDefinitionAsync(taskStep.Task);
        
        // 2. Process inputs
        var inputs = await ProcessTaskInputsAsync(executionContext, taskStep, taskDefinition);
        
        // 3. Create and configure handler
        var handler = CreateTaskHandler(executionContext, taskDefinition, inputs);
        
        // 4. Execute task
        await handler.RunAsync(executionContext);
        
        // 5. Process outputs
        await ProcessTaskOutputsAsync(executionContext, taskDefinition);
    }
    catch (Exception ex)
    {
        executionContext.Error($"Task '{taskStep.DisplayName}' failed: {ex.Message}");
        executionContext.Result = TaskResult.Failed;
        throw;
    }
}
```

**Input Processing (P1 Feature)**:
```csharp
private async Task<Dictionary<string, string>> ProcessTaskInputsAsync(
    IExecutionContext executionContext,
    ITaskStep taskStep,
    TaskDefinition taskDefinition)
{
    var inputs = new Dictionary<string, string>();
    
    foreach (var input in taskDefinition.Inputs)
    {
        var inputValue = taskStep.Inputs.GetValueOrDefault(input.Name, input.DefaultValue);
        
        // Variable expansion
        inputValue = executionContext.Variables.ExpandValue(inputValue);
        
        // Secret masking for sensitive inputs
        if (input.IsSecret)
        {
            HostContext.SecretMasker.AddValue(inputValue, $"TaskInput_{input.Name}");
        }
        
        inputs[input.Name] = inputValue;
        
        // P1 REQUIREMENT: Log task input parameters (when flag enabled)
        if (AgentKnobs.LogTaskInputParameters.GetValue(HostContext).AsBoolean())
        {
            var logValue = input.IsSecret ? "***" : inputValue;
            executionContext.Output($"Input '{input.Name}': {logValue}");
        }
    }
    
    return inputs;
}
```

**Handler Types**:
```csharp
public enum TaskHandlerType
{
    Node,           // Node.js execution
    PowerShell,     // PowerShell script execution
    Process,        // External process execution
    Container       // Container-based execution
}
```

---

### ExecutionContext

**Purpose**: Provides logging, variable management, and state tracking for execution scopes.

**Location**: `src/Agent.Worker/ExecutionContext.cs`

**Key Responsibilities**:
- Hierarchical logging with scope management
- Variable storage and expansion
- Timeline integration
- Result tracking and aggregation

**Logging Integration**:
```csharp
public class ExecutionContext : IExecutionContext
{
    public void Output(string message)
    {
        // Timeline integration
        _timelineManager.UpdateRecord(TimelineRecordType.Output, message);
        
        // Console output
        Console.WriteLine($"##[section]{message}");
        
        // Trace logging
        Trace.Info($"[{_scopeName}] {message}");
    }
    
    public void Error(string message)
    {
        _errorCount++;
        _timelineManager.UpdateRecord(TimelineRecordType.Error, message);
        Console.WriteLine($"##[error]{message}");
        Trace.Error($"[{_scopeName}] {message}");
    }
}
```

**Variable Management**:
```csharp
public class Variables
{
    private readonly Dictionary<string, Variable> _variables;
    
    public string ExpandValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
            
        // Variable expansion: $(variableName) format
        return Regex.Replace(value, @"\$\(([^)]+)\)", match =>
        {
            var variableName = match.Groups[1].Value;
            return GetValueOrDefault(variableName, match.Value);
        });
    }
}
```

---

### ProcessChannel (IPC Communication)

**Purpose**: Manages inter-process communication between listener and worker.

**Location**: `src/Agent.Worker/ProcessChannel.cs`

**Key Responsibilities**:
- Named pipe/socket management
- Message serialization/deserialization
- Timeout and error handling
- Secure communication protocols

**Channel Implementation**:
```csharp
public class ProcessChannel : IProcessChannel
{
    public async Task<WorkerMessage> ReceiveAsync(CancellationToken cancellationToken)
    {
        try
        {
            var messageLength = await ReadInt32Async(_inputStream, cancellationToken);
            var messageBytes = await ReadBytesAsync(_inputStream, messageLength, cancellationToken);
            var messageJson = Encoding.UTF8.GetString(messageBytes);
            return JsonUtility.FromString<WorkerMessage>(messageJson);
        }
        catch (Exception ex)
        {
            Trace.Error($"Failed to receive message: {ex}");
            throw;
        }
    }
    
    public async Task SendAsync(MessageType messageType, string body, CancellationToken cancellationToken)
    {
        var message = new WorkerMessage(messageType, body);
        var messageJson = JsonUtility.ToString(message);
        var messageBytes = Encoding.UTF8.GetBytes(messageJson);
        
        await WriteInt32Async(_outputStream, messageBytes.Length, cancellationToken);
        await WriteBytesAsync(_outputStream, messageBytes, cancellationToken);
        await _outputStream.FlushAsync(cancellationToken);
    }
}
```

## 🔧 Shared Components

### AgentKnobs (Configuration)

**Purpose**: Centralized configuration management with feature flags.

**Location**: `src/Microsoft.VisualStudio.Services.Agent/AgentKnobs.cs`

**Key Features**:
- Runtime configuration changes
- Environment variable integration
- Default value management
- Type-safe configuration access

**Usage Pattern**:
```csharp
public static readonly Knob LogTaskInputParameters = new Knob(
    nameof(LogTaskInputParameters),
    "Log task input parameters for debugging",
    new RuntimeKnobSource("AGENT_LOG_TASK_INPUTS"),
    new BuiltInDefaultKnobSource("false"));

// Usage in code
if (AgentKnobs.LogTaskInputParameters.GetValue(HostContext).AsBoolean())
{
    // Feature implementation
}
```

### SecretMasker

**Purpose**: Comprehensive secret detection and masking across all outputs.

**Location**: `src/Microsoft.VisualStudio.Services.Agent/SecretMasker.cs`

**Key Features**:
- Multi-pattern secret detection
- Regex-based masking
- Cross-process secret protection
- Dynamic secret registration

## 📊 Component Interaction Patterns

### Typical Call Chain

```
JobDispatcher.DispatchAsync()
├── Create Worker Process
├── Worker.RunAsync()
│   ├── Initialize SecretMasker
│   ├── JobRunner.RunAsync()
│   │   ├── Setup Environment
│   │   ├── StepsRunner.RunAsync()
│   │   │   ├── For each step:
│   │   │   ├── TaskRunner.RunAsync()
│   │   │   │   ├── Load Task Definition
│   │   │   │   ├── Process Inputs
│   │   │   │   ├── Create Handler
│   │   │   │   ├── Execute Task
│   │   │   │   └── Process Outputs
│   │   │   └── Update Timeline
│   │   └── Aggregate Results
│   └── Report Completion
└── Cleanup Worker Process
```

### Error Propagation

```
TaskRunner (Task fails)
├── Set ExecutionContext.Result = Failed
├── StepsRunner (continues or stops based on continueOnError)
├── JobRunner (aggregates step results)
├── Worker (reports job result)
└── JobDispatcher (cleanup and next job preparation)
```

## 🎯 Key Integration Points

### Timeline Integration
- Real-time progress updates to Azure DevOps
- Hierarchical record structure (Job → Steps → Tasks)
- Output capture and streaming

### Artifact Management
- File upload coordination
- Artifact metadata management
- Storage integration

### Resource Management
- Working directory cleanup
- Temporary file management
- Process resource monitoring

## 🔄 Next Steps

For deeper understanding of specific areas:

1. **[Security Implementation](./Security-Implementation.md)** - Security features and protocols
2. **[Performance Monitoring](./Performance-Monitoring.md)** - Metrics and optimization
3. **[Error Handling](./Error-Handling.md)** - Exception management and recovery
4. **[Logging Architecture](./Logging-Architecture.md)** - Logging system and best practices

---

**Component Summary:**
- Well-defined separation of concerns across components
- Comprehensive error handling and logging throughout
- Strong security integration at all levels
- Scalable architecture supporting diverse execution scenarios
