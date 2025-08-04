# Quick Start Guide

Welcome to the Azure DevOps Agent team! This guide will get you up to speed quickly on the agent architecture and codebase.

## 🚀 5-Minute Overview

### What You Need to Know Immediately

1. **Two Processes**: Agent has Listener (persistent) + Worker (per-job)
2. **Sequential Jobs**: One job at a time per agent (by design)
3. **Process Isolation**: Each job runs in separate Worker process
4. **IPC Communication**: Pipes connect Listener and Worker
5. **Security First**: Comprehensive secret masking throughout

### Key File Locations

```
src/
├── Agent.Listener/          # Persistent process, job polling
│   ├── Program.cs           # Entry point
│   └── JobDispatcher.cs     # Creates worker processes
├── Agent.Worker/            # Per-job process, task execution
│   ├── Program.cs           # Entry point
│   ├── Worker.cs            # Main orchestration
│   ├── JobRunner.cs         # Job execution management
│   ├── StepsRunner.cs       # Step orchestration
│   └── TaskRunner.cs        # Individual task execution
└── Microsoft.VisualStudio.Services.Agent/  # Shared libraries
    ├── AgentKnobs.cs        # Feature flags and settings
    └── Constants.cs         # System constants
```

## 🔧 Development Environment Setup

### Prerequisites

- **Visual Studio 2022** or **VS Code** with C# extension
- **.NET 6.0 SDK** or later
- **Git** for version control
- **PowerShell** (for Windows development)

### Quick Build

```powershell
# Clone repository (if not already done)
git clone https://github.com/microsoft/azure-pipelines-agent.git
cd azure-pipelines-agent

# Build agent
src\dev.cmd layout

# Run tests
src\dev.cmd test
```

### Development Workflow

```powershell
# Make changes to code
# Build and test locally
src\dev.cmd build
src\dev.cmd test

# Debug specific component
# Set startup project in Visual Studio to:
# - Agent.Listener (for listener debugging)
# - Agent.Worker (for worker debugging)
```

## 🎯 Common Development Scenarios

### 1. Adding New Logging

**Where**: Usually in `TaskRunner.cs`, `JobRunner.cs`, or `Worker.cs`

```csharp
// Standard pattern
Trace.Info($"Your message here: {variable}");

// With feature flag
if (AgentKnobs.LogTaskInputParameters.GetValue(HostContext).AsBoolean())
{
    Trace.Info($"Task inputs: {inputData}");
}
```

### 2. Adding Configuration Options

**Where**: Add to `AgentKnobs.cs`

```csharp
public static readonly Knob YourNewSetting = new Knob(
    nameof(YourNewSetting),
    "Description of the setting",
    new RuntimeKnobSource("AGENT_YOUR_SETTING"),
    new BuiltInDefaultKnobSource("false"));
```

### 3. Modifying Task Execution

**Where**: `TaskRunner.cs::RunAsyncInternal()`

```csharp
// Common pattern around line 200-300
try
{
    // Your task execution logic
    executionContext.Output("Your output message");
}
catch (Exception ex)
{
    // Error handling
    executionContext.Error($"Error: {ex.Message}");
    throw;
}
```

## 🐛 Debugging Tips

### Setting Up Debug Environment

1. **Agent Configuration**:
   ```powershell
   # Configure agent for local debugging
   .\config.cmd --unattended --url https://dev.azure.com/yourorg --auth pat --token yourtoken --pool "Default" --agent "debug-agent"
   ```

2. **Visual Studio Setup**:
   - Set breakpoints in relevant components
   - Use "Attach to Process" for running agents
   - Set startup project based on what you're debugging

### Debug Agent Execution

```powershell
# Run listener in debug mode
cd _layout
.\run.cmd --once  # Run single job then exit

# Check logs
type _diag\Agent_*.log | Select-String "your search term"
```

### Common Debugging Scenarios

| Scenario | Component | Key Files |
|----------|-----------|-----------|
| Job not starting | JobDispatcher | JobDispatcher.cs, Worker.cs |
| Task failing | TaskRunner | TaskRunner.cs, specific task handlers |
| Communication issues | Worker/Listener | ProcessChannel.cs, Worker.cs |
| Secret masking | Worker | Worker.cs (InitializeSecretMasker) |

## 📝 Code Patterns to Follow

### 1. Error Handling

```csharp
// Always use this pattern
try
{
    // Your code here
}
catch (Exception ex) when (!(ex is OperationCanceledException))
{
    Trace.Error($"Error in {nameof(YourMethod)}: {ex}");
    // Handle appropriately
    throw;
}
```

### 2. Logging with Context

```csharp
// Include relevant context in logs
Trace.Info($"Processing task '{taskDisplayName}' (Id: {taskId}) for job {jobId}");
```

### 3. Resource Cleanup

```csharp
// Always use using statements for disposables
using (var resource = CreateResource())
{
    // Use resource
}
```

### 4. Secret Handling

```csharp
// Never log secrets directly
// Use secret masker for any potentially sensitive data
if (!string.IsNullOrEmpty(sensitiveValue))
{
    HostContext.SecretMasker.AddValue(sensitiveValue, "YourSecretType");
}
```

## 🧪 Testing Guidelines

### Unit Tests

```csharp
// Test structure pattern
[Fact]
public void YourMethod_WithValidInput_ReturnsExpectedResult()
{
    // Arrange
    var mockHostContext = new Mock<IHostContext>();
    // Setup mocks

    // Act
    var result = yourComponent.YourMethod(input);

    // Assert
    Assert.Equal(expectedResult, result);
}
```

### Integration Tests

- Located in `src/Test/`
- Use `TestHostContext` for agent services
- Mock external dependencies (Azure DevOps API calls)

### Running Tests

```powershell
# Run all tests
src\dev.cmd test

# Run specific test class
dotnet test --filter "ClassName=YourTestClass"

# Run with coverage
src\dev.cmd test --coverage
```

## 🔍 Understanding the Codebase

### Key Concepts

1. **Service Locator Pattern**: Heavy use of dependency injection
   ```csharp
   var service = HostContext.GetService<IYourService>();
   ```

2. **Execution Context**: Thread-safe logging and state management
   ```csharp
   executionContext.Output("Message to pipeline");
   executionContext.Debug("Debug information");
   ```

3. **Cancellation Tokens**: Proper cancellation handling throughout
   ```csharp
   public async Task YourMethod(CancellationToken cancellationToken)
   {
       // Use cancellationToken in async operations
   }
   ```

### Data Flow Overview

```
Azure DevOps → Agent.Listener → JobDispatcher → Agent.Worker → JobRunner → StepsRunner → TaskRunner
```

Each arrow represents:
- **HTTP/WebSocket**: Azure DevOps ↔ Agent.Listener
- **IPC Pipes**: Agent.Listener ↔ Agent.Worker  
- **In-Process**: Worker → JobRunner → StepsRunner → TaskRunner

## 📚 Essential Reading

### First Week
1. **[Agent Overview](./Agent-Overview.md)** - Architecture fundamentals
2. **[Agent Lifecycle Flow](./Agent-Lifecycle-Flow.md)** - Complete execution flow
3. **[Process Architecture](./Process-Architecture.md)** - Inter-process communication

### Second Week
4. **[Component Reference](./Component-Reference.md)** - Detailed component docs
5. **[Security Implementation](./Security-Implementation.md)** - Security features
6. **[Logging Architecture](./Logging-Architecture.md)** - Logging system

### Ongoing Reference
7. **[Troubleshooting Guide](./Troubleshooting-Guide.md)** - Common issues
8. **[Development Guidelines](./Development-Guidelines.md)** - Best practices

## 🆘 Getting Help

### Immediate Questions
- **Teams Channel**: `#agent-development`
- **Code Reviews**: Tag experienced team members
- **Architecture Questions**: Platform architecture team

### Documentation Issues
- **Missing Information**: Create wiki page or update existing
- **Outdated Content**: Submit documentation updates
- **Complex Scenarios**: Pair with experienced developer

## ✅ Checklist for First Contribution

### Before You Start
- [ ] Successfully build agent locally
- [ ] Run basic tests
- [ ] Read relevant wiki pages
- [ ] Understand component you're modifying

### Making Changes
- [ ] Follow established code patterns
- [ ] Add appropriate logging
- [ ] Include error handling
- [ ] Add or update tests

### Before Submitting PR
- [ ] All tests pass locally
- [ ] Code review checklist complete
- [ ] Documentation updated if needed
- [ ] Breaking changes documented

## 🎓 Learning Path by Role

### **Backend Developer**
Focus on: JobRunner, TaskRunner, execution pipeline

### **Infrastructure/DevOps**
Focus on: Configuration, deployment, monitoring

### **Security Engineer**
Focus on: Secret masking, process isolation, certificates

### **Platform Engineer**
Focus on: Process architecture, communication, performance

---

**Remember**: Start small, ask questions, and refer back to this guide as you learn the codebase!
