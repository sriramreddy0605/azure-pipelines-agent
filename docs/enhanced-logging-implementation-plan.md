# Enhanced Logging Implementation Plan for Tracing.cs

## Overview
This document outlines the planned enhancements to the Azure DevOps Agent logging infrastructure, specifically focusing on updates to `Tracing.cs` to provide structured logging with automatic context detection.

## Current State
The existing `Tracing.cs` already has enhanced `Entering()` and `Leaving()` methods implemented with automatic component detection using `[CallerMemberName]` and `[CallerFilePath]` attributes.

## Proposed Changes

### 1. Enhanced Log Format
**Target Format:** `[component] [phase] [correlationid][operation] message [metadata]`

**With Duration (for Leaving methods):** `[component] [phase] [correlationid][operation] message (duration: Xms) [metadata]`

**Example:**
```
[JOBDISPATCHER] [GENERAL] [12345][ProcessJob] Starting job execution
[TASKRUNNER] [GENERAL] [12345][RunTask] Executing PowerShell task [taskId=456, retryCount=0]
[AGENTSERVICE] [GENERAL] [AGENT-DEFAULT][Initialize] Agent service initialized
[JOBDISPATCHER] [GENERAL] [12345][ProcessJob] Leaving ProcessJob (duration: 1250ms)
```

### 2. Method Enhancement Strategy
Instead of creating new methods, we will enhance existing logger methods by adding optional `CallerMemberName` and `CallerFilePath` parameters with feature flag control.

#### Methods to Enhance:
- `Info(string message, Dictionary<string, object> metadata = null)`
- `Info(string format, params object[] args)` with optional metadata
- `Info(object item, Dictionary<string, object> metadata = null)`
- `Error(string message, Dictionary<string, object> metadata = null)`
- `Error(string format, params object[] args)` with optional metadata
- `Error(Exception exception, Dictionary<string, object> metadata = null)`
- `Warning(string message, Dictionary<string, object> metadata = null)`
- `Warning(string format, params object[] args)` with optional metadata
- `Verbose(string message, Dictionary<string, object> metadata = null)`
- `Verbose(string format, params object[] args)` with optional metadata
- `Verbose(object item, Dictionary<string, object> metadata = null)`

#### Implementation Pattern:
```csharp
public void Info(string message, Dictionary<string, object> metadata = null, 
                [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
{
    if (ShouldUseEnhancedLogging())
    {
        var component = ExtractComponentFromFilePath(filePath);
        var enhancedMessage = BuildStructuredMessage(component, _defaultPhase, _currentCorrelationId, memberName, message, metadata);
        Trace(TraceEventType.Information, enhancedMessage);
    }
    else
    {
        Trace(TraceEventType.Information, message);
    }
}
```

### 3. Feature Flag Implementation
- **Feature Flag Name:** `Agent.Logging.EnhancedFormat`
- **Default Value:** `false` (maintains backward compatibility)
- **Implementation:** AgentKnobs-based configuration
- **Granularity:** Global agent-level control

#### Feature Flag Method:
```csharp
private bool ShouldUseEnhancedLogging()
{
    return AgentKnobs.EnhancedLogging.GetValue(ExecutionContext).AsBoolean();
}
```

### 4. Automatic Component Detection
- **Source:** `[CallerFilePath]` attribute provides compile-time file path
- **Algorithm:** Extract filename without extension, convert to uppercase
- **Fallback:** Use `_defaultComponent` when file path unavailable

#### Examples:
- `JobDispatcher.cs` → `JOBDISPATCHER`
- `TaskRunner.cs` → `TASKRUNNER`
- `AgentService.cs` → `AGENTSERVICE`

### 5. Context Fields
Already implemented in current `Tracing.cs`:
- `_currentCorrelationId`: Tracking request/job correlation
- `_defaultComponent`: Fallback component name from tracer name
- `_defaultPhase`: Default phase value ("General")

**Additional fields needed for duration tracking:**
- `_methodStartTimes`: Dictionary<string, DateTime> to track method entry times
- Key format: `{component}:{memberName}` for unique method identification

### 6. Duration Tracking Implementation
```csharp
private readonly Dictionary<string, DateTime> _methodStartTimes = new Dictionary<string, DateTime>();

public void Entering([CallerMemberName] string name = "", [CallerFilePath] string filePath = "")
{
    var component = ExtractComponentFromFilePath(filePath);
    var methodKey = $"{component}:{name}";
    _methodStartTimes[methodKey] = DateTime.UtcNow;
    
    if (ShouldUseEnhancedLogging())
    {
        var message = BuildStructuredMessage(component, _defaultPhase, _currentCorrelationId, name, $"Entering {name}");
        Trace(TraceEventType.Verbose, message);
    }
    else
    {
        Trace(TraceEventType.Verbose, $"Entering {name}");
    }
}

public void Leaving([CallerMemberName] string name = "", [CallerFilePath] string filePath = "")
{
    var component = ExtractComponentFromFilePath(filePath);
    var methodKey = $"{component}:{name}";
    
    long durationMs = 0;
    if (_methodStartTimes.TryGetValue(methodKey, out DateTime startTime))
    {
        durationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
        _methodStartTimes.Remove(methodKey);
    }
    
    if (ShouldUseEnhancedLogging())
    {
        var message = BuildStructuredMessageWithDuration(component, _defaultPhase, _currentCorrelationId, name, $"Leaving {name}", durationMs);
        Trace(TraceEventType.Verbose, message);
    }
    else
    {
        Trace(TraceEventType.Verbose, $"Leaving {name}");
    }
}
```

### 7. Metadata Support Implementation
```csharp
private string BuildStructuredMessage(string component, string phase, string correlationId, string operation, string message, Dictionary<string, object> metadata = null)
{
    var metadataStr = FormatMetadata(metadata);
    return $"[{component}] [{phase}] [{correlationId}][{operation}] {message}{metadataStr}";
}

private string BuildStructuredMessageWithDuration(string component, string phase, string correlationId, string operation, string message, long durationMs, Dictionary<string, object> metadata = null)
{
    var metadataStr = FormatMetadata(metadata);
    return $"[{component}] [{phase}] [{correlationId}][{operation}] {message} (duration: {durationMs}ms){metadataStr}";
}

private string FormatMetadata(Dictionary<string, object> metadata)
{
    if (metadata == null || metadata.Count == 0)
        return string.Empty;
    
    var pairs = metadata.Select(kvp => $"{kvp.Key}={kvp.Value}");
    return $" [{string.Join(", ", pairs)}]";
}
```

## Benefits

### 1. Zero Breaking Changes
- Existing method signatures remain unchanged
- All current calling code continues to work
- No method resolution ambiguity

### 2. Automatic Context Injection
- No manual parameters required from calling code
- Compile-time injection of context information
- Consistent component naming across codebase

### 3. Rollback Capability
- Feature flag OFF → Original log format
- Feature flag ON → Enhanced structured format
- No code restructuring required for rollback

### 4. Gradual Rollout
- Can enable enhanced logging per environment
- Easy A/B testing capability
- Risk mitigation through controlled deployment

## Implementation Phases

### Phase 1: Core Infrastructure (Current PR)
- [x] Enhanced `Entering()` and `Leaving()` methods
- [x] Component detection utilities
- [x] Structured message building
- [ ] Feature flag integration
- [ ] Duration tracking for Entering/Leaving
- [ ] Metadata support infrastructure
- [ ] Enhanced logging methods

### Phase 2: Method Enhancement
- [ ] Add `CallerMemberName` and `CallerFilePath` to all logging methods
- [ ] Add optional metadata parameter to all logging methods
- [ ] Implement feature flag checks
- [ ] Update method signatures

### Phase 3: Duration & Metadata Features
- [ ] Implement method start time tracking
- [ ] Enhanced Leaving method with duration calculation
- [ ] Metadata formatting utilities
- [ ] Thread-safe duration tracking

### Phase 4: Testing & Validation
- [ ] Unit tests for enhanced methods
- [ ] Integration testing with feature flag
- [ ] Performance impact analysis
- [ ] Backward compatibility verification
- [ ] Duration accuracy testing
- [ ] Metadata serialization testing

### Phase 5: Deployment
- [ ] Feature flag configuration
- [ ] Documentation updates
- [ ] Team training
- [ ] Gradual rollout plan

## Technical Considerations

### 1. Performance Impact
- Minimal overhead when feature flag is OFF
- String operations only when enhanced logging enabled
- File path processing cached per component

### 2. Memory Usage
- Additional context fields per Tracing instance
- String allocations for enhanced format
- Method start time tracking dictionary (`_methodStartTimes`)
- Metadata serialization overhead
- Negligible impact on overall agent memory

### 3. Backward Compatibility
- 100% backward compatible
- No changes to existing method contracts
- Optional parameters maintain compatibility
- Metadata parameter is optional and defaults to null

### 4. Thread Safety Considerations
- `_methodStartTimes` dictionary requires thread-safe operations
- Consider using `ConcurrentDictionary<string, DateTime>` for multi-threaded scenarios
- Duration tracking isolated per method call using unique keys

## Example Usage Scenarios

### Before Enhancement:
```csharp
_trace.Info("Starting job processing");
_trace.Error("Failed to connect to server");
```

**Output:**
```
Starting job processing
Failed to connect to server
```

### After Enhancement (Feature Flag ON):
```csharp
_trace.Info("Starting job processing");  // Same calling code
_trace.Error("Failed to connect to server");  // Same calling code

// With metadata support:
var metadata = new Dictionary<string, object> { {"jobId", 12345}, {"retryCount", 0} };
_trace.Info("Processing job", metadata);
```

**Output:**
```
[JOBDISPATCHER] [GENERAL] [12345][ProcessJobs] Starting job processing
[AGENTSERVICE] [GENERAL] [12345][ConnectToServer] Failed to connect to server
[JOBDISPATCHER] [GENERAL] [12345][ProcessJobs] Processing job [jobId=12345, retryCount=0]
```

**Duration Tracking Example:**
```csharp
public void ProcessJob()
{
    _trace.Entering();  // Automatically tracks start time
    // ... job processing logic ...
    _trace.Leaving();   // Automatically calculates and logs duration
}
```

**Output:**
```
[JOBDISPATCHER] [GENERAL] [12345][ProcessJob] Entering ProcessJob
[JOBDISPATCHER] [GENERAL] [12345][ProcessJob] Leaving ProcessJob (duration: 1250ms)
```

## Risk Mitigation

### 1. Rollback Strategy
- Immediate rollback via feature flag toggle
- No code deployment required for rollback
- Monitoring alerts for log format changes

### 2. Testing Strategy
- Comprehensive unit test coverage
- Integration tests with both flag states
- Performance benchmarking
- Memory leak detection

### 3. Deployment Strategy
- Internal environment first
- Gradual percentage rollout
- Monitoring and alerting
- Quick rollback procedures

## Team Tasks Distribution

### Backend Team
- Feature flag implementation in AgentKnobs
- Method signature updates
- Unit test development

### DevOps Team
- Feature flag configuration
- Deployment pipeline updates
- Monitoring setup

### QA Team
- Integration testing
- Performance validation
- Backward compatibility testing

## Success Criteria
1. All existing tests pass without modification
2. Enhanced logging provides structured format when enabled
3. Feature flag toggles format correctly
4. No performance degradation when feature disabled
5. Component detection works accurately across all agent files

## Questions for Team Discussion
1. Should we implement per-component feature flags for more granular control?
2. Should metadata be limited to simple key-value pairs or support complex objects?
3. Should correlation ID be configurable or auto-generated?
4. What monitoring/alerting do we need for the new log format?
5. Timeline preferences for gradual rollout phases?
6. Should we use `ConcurrentDictionary` for thread-safe duration tracking?
7. How should we handle nested method calls for duration tracking (stack-based approach)?
8. Should metadata have size limits to prevent log bloat?
9. Do we need duration tracking for all log methods or just Entering/Leaving?

---
**Document Version:** 1.0  
**Last Updated:** July 30, 2025  
**Author:** Development Team  
**Reviewers:** [To be filled during team discussion]
