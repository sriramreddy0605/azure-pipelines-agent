# Enhanced Logging Implementation Guide

## 🎯 Phase 1: Foundation Infrastructure (COMPLETED ✅)

### What We've Built
1. **Enhanced Tracing.cs** - Core logging infrastructure with structured format support
2. **LoggingConstants.cs** - Shared constants for components, phases, and correlation patterns  
3. **TracingExtensions.cs** - Convenient extension methods for common logging scenarios
4. **EnhancedLoggingExamples.cs** - Practical examples and expected log output

### Key Features Added
- **Correlation ID Management**: Hierarchical tracking (Job → Step → Task → Subtask)
- **Structured Log Format**: `[component] [phase] [correlationid][operation] message (duration: ) [metadata]`
- **Context Scoping**: Automatic context management with `using` statements
- **Performance Monitoring**: Built-in duration tracking and performance scopes
- **Backward Compatibility**: Existing code continues to work while supporting gradual migration

## 🚀 Phase 2: Parallel Implementation (NEXT STEPS)

### Rishabh's Tasks: Agent Lifecycle Logging
Focus on agent startup, configuration, job dispatch, and overall lifecycle events.

#### Key Files to Modify:
```
src/Agent.Listener/
├── Agent.cs                    # Main agent lifecycle
├── Configuration/
│   ├── ConfigurationManager.cs # Agent configuration
│   └── AgentSettings.cs        # Settings management
├── JobDispatcher.cs            # Job dispatch logic
└── MessageListener.cs          # Server communication
```

#### Implementation Strategy:
```csharp
// 1. Add context at agent startup
using (_trace.SetContext(LoggingConstants.Components.AgentListener, LoggingConstants.Phases.AgentStartup))
{
    _trace.LogAgentLifecycle(
        LoggingConstants.Phases.AgentStartup, 
        LoggingConstants.Operations.Initialize,
        "Agent starting up",
        correlationId: $"AGENT-{Environment.MachineName}-{DateTime.UtcNow:yyyyMMddHHmmss}"
    );
}

// 2. Use scoped contexts for job processing
using (_trace.CreateJobContext(jobId, workerId))
{
    _trace.LogJobLifecycle(
        LoggingConstants.Phases.JobReceived,
        LoggingConstants.Operations.Receive,
        "Job received from server",
        metadata: new { JobId = jobId, PoolId = poolId }
    );
}
```

### Sanju's Tasks: Task Lifecycle & Format Migration
Focus on task execution, handlers, and migrating existing log calls to enhanced format.

#### Key Files to Modify:
```
src/Agent.Worker/
├── JobRunner.cs                # Job execution coordination
├── StepsRunner.cs              # Step execution
├── TaskRunner.cs               # Task execution
└── Handlers/                   # Task handlers
    ├── NodeHandler.cs
    ├── PowerShellHandler.cs
    └── ProcessHandler.cs
```

#### Implementation Strategy:
```csharp
// 1. Add context at task level
using (_trace.CreateTaskContext(stepCorrelationId, taskId))
{
    _trace.LogTaskLifecycle(
        LoggingConstants.Phases.TaskExecution,
        LoggingConstants.Operations.TaskExecute,
        $"Executing task: {taskName}",
        metadata: new { TaskId = taskId, TaskVersion = version }
    );
}

// 2. Migrate existing log calls
// OLD: _trace.Info("Task completed successfully");
// NEW: _trace.LogWithEnhancedFormat(TraceEventType.Information, "Task completed successfully");

// 3. Add performance monitoring
using (_trace.MeasurePerformance("TaskDownload", LoggingConstants.Components.TaskRunner, taskCorrelationId))
{
    // Download task implementation
}
```

## 📋 Implementation Checklist

### Phase 2A: Rishabh (Agent Lifecycle)
- [ ] **Agent.cs**: Add startup/shutdown lifecycle logging
- [ ] **ConfigurationManager.cs**: Add configuration phase logging
- [ ] **JobDispatcher.cs**: Add job dispatch and worker coordination logging
- [ ] **MessageListener.cs**: Add server communication logging
- [ ] **Update correlation IDs**: Ensure job correlation IDs flow through entire pipeline

### Phase 2B: Sanju (Task Lifecycle & Migration)  
- [ ] **TaskRunner.cs**: Add task lifecycle logging (init, prep, exec, cleanup)
- [ ] **StepsRunner.cs**: Add step coordination logging
- [ ] **Handler Classes**: Migrate existing log calls to enhanced format
- [ ] **Performance Monitoring**: Add duration tracking for key operations
- [ ] **Error Handling**: Enhance error logging with structured format

### Phase 2C: Integration & Validation
- [ ] **End-to-End Testing**: Verify correlation IDs flow from agent to task completion
- [ ] **Performance Impact**: Measure logging overhead and optimize if needed
- [ ] **Documentation**: Update team wiki with implementation examples
- [ ] **Flag Integration**: Ensure enhanced logging respects existing AgentKnobs flags

## 🛠️ Usage Patterns

### 1. Agent Lifecycle Events
```csharp
// Agent startup
_trace.LogAgentLifecycle(LoggingConstants.Phases.AgentStartup, 
    LoggingConstants.Operations.Initialize, "message");

// Job processing  
using (_trace.CreateJobContext(jobId, workerId)) { /* job work */ }
```

### 2. Task Lifecycle Events  
```csharp
// Task execution
_trace.LogTaskLifecycle(LoggingConstants.Phases.TaskExecution,
    LoggingConstants.Operations.TaskExecute, "message");

// With performance monitoring
using (_trace.MeasurePerformance("operation")) { /* work */ }
```

### 3. Format Migration
```csharp
// Simple migration (automatic component detection)
_trace.LogWithEnhancedFormat(TraceEventType.Information, "message");

// Full structured logging (preferred)
_trace.LogStructured(level, component, phase, correlationId, operation, message);
```

### 4. Error Handling
```csharp
// Enhanced error logging
_trace.LogError(exception, operation, component, correlationId, additionalContext);
```

## 🔄 Coordination Strategy

### Daily Sync Points
1. **Morning Standup**: Review progress and coordinate correlation ID flow
2. **Integration Testing**: Test end-to-end correlation every 2-3 days
3. **Code Reviews**: Cross-review each other's correlation ID implementations

### Merge Strategy
1. **Rishabh commits first**: Agent lifecycle changes (foundation for correlation IDs)
2. **Sanju follows**: Task lifecycle changes (builds on job correlation IDs)
3. **Joint integration**: End-to-end testing and refinements

### Shared Dependencies
- **LoggingConstants.cs**: Both team members update as needed
- **TracingExtensions.cs**: Add methods as needed for specific scenarios
- **Correlation ID Flow**: Must be consistent across all components

## 📊 Expected Log Output

After implementation, logs will look like:
```
[LISTENER] [Startup] [AGENT-MACHINE-20241201123045][Initialize] Agent starting up [Version=3.0.0]
[LISTENER] [Listen] [AGENT-MACHINE-20241201123045][Connect] Connected to Azure DevOps [ServerUrl=https://dev.azure.com/org]
[JOBRUNNER] [JobReceived] [JOB-12345|WKR-67890][Receive] Job received from server [JobId=12345, PoolId=123]
[STEPSRUNNER] [Process] [JOB-12345|WKR-67890|STEP-step1][Execute] Processing step: Build Solution
[TASKRUNNER] [TaskExec] [JOB-12345|WKR-67890|STEP-step1|TASK-task1][TaskExecute] Executing MSBuild task (duration: 1234ms)
[TASKRUNNER] [TaskComplete] [JOB-12345|WKR-67890|STEP-step1|TASK-task1][TaskExecute] Task completed successfully [ExitCode=0]
```

## 🚨 Important Notes

1. **Maintain Backward Compatibility**: All existing trace calls must continue working
2. **Respect Existing Flags**: Enhanced logging should honor AgentKnobs configuration
3. **Performance**: Monitor impact and use async logging where appropriate
4. **Secret Masking**: All enhanced logs still go through existing secret masking
5. **Testing**: Add unit tests for new logging methods and correlation ID generation

## 🤝 Support & Questions

- **Implementation Questions**: Ask in team chat or during daily standup
- **Correlation ID Issues**: Review EnhancedLoggingExamples.cs for patterns
- **Performance Concerns**: Use performance scopes and monitor impact
- **Migration Help**: Reference TracingExtensions.LogWithEnhancedFormat method

---

**Ready to start Phase 2? Let's build amazing logging! 🎉**
