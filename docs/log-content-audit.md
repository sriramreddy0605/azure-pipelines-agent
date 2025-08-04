# Azure DevOps Agent - Log Content Audit

## Executive Summary

This document provides a comprehensive audit of the Azure DevOps Agent logging infrastructure, analyzing what data is normally present versus what's hidden behind flags, and mapping the complete landscape of logging controls available in the agent.

## Current Logging Architecture

### Core Logging Components

1. **Tracing System** (`src/Microsoft.VisualStudio.Services.Agent/Tracing.cs`)
   - Standard log levels: Off, Critical, Error, Warning, Info, Verbose
   - Configurable through `TraceSetting.cs` with default level `Info` (Production) or `Verbose` (Debug builds)

2. **AgentKnobs Configuration** (`src/Agent.Sdk/Knob/AgentKnobs.cs`)
   - Central configuration system with 80+ logging-related flags
   - Environment variables and runtime knobs for controlling logging behavior

3. **Diagnostic Log Manager** 
   - Handles diagnostic log upload and management
   - Located in `_diag` folder by default

## Logging Flag Categories

### 1. Core Agent Diagnostic Flags

#### Always Enabled Logs (No Flags Required)
- **Basic operational logs**: Info, Warning, Error, Critical levels
- **Job lifecycle events**: Job start/stop, task execution status
- **Configuration loading**: Agent startup, capability detection
- **Connection status**: Server communication, authentication
- **Standard error handling**: Exception logging, failure reporting

#### Flag-Controlled Core Diagnostics
- **`VSTS_AGENT_TRACE=true`** or **`ADO_AGENT_DEBUG=true`**
  - **Purpose**: Enables verbose agent diagnostic tracing to `_diag` folder
  - **Impact**: Significantly increases log verbosity for agent internals
  - **Current Usage**: Behind flag
  - **Content**: Detailed execution flow, internal state changes, component interactions

### 2. HTTP and Network Logging

#### Flag-Controlled HTTP Debugging
- **`VSTS_AGENT_HTTPTRACE=true`**
  - **Purpose**: HTTP request/response logging, network diagnostics
  - **Impact**: Logs all HTTP communications with Azure DevOps
  - **Current Usage**: Behind flag
  - **Content**: HTTP headers, request/response bodies, network timing

### 3. Performance and Resource Monitoring

#### Flag-Controlled Performance Logging
- **`VSTS_AGENT_PERFLOG=<path>`**
  - **Purpose**: Performance counter logging and metrics
  - **Impact**: System resource usage, timing metrics
  - **Current Usage**: Behind flag
  - **Content**: Memory usage, CPU utilization, I/O metrics

#### Resource Utilization Debugging
- **`AZP_ENABLE_RESOURCE_MONITOR_DEBUG_OUTPUT=true`**
  - **Purpose**: Resource utilization debugging for debug runs
  - **Impact**: Detailed resource monitoring during job execution
  - **Current Usage**: Behind flag, disabled by default even in debug mode
  - **Content**: Process resource consumption, system performance metrics

- **`AZP_ENABLE_RESOURCE_UTILIZATION_WARNINGS=true`**
  - **Purpose**: Resource utilization warnings
  - **Impact**: Alerts when resource thresholds are exceeded
  - **Current Usage**: Behind flag
  - **Content**: Resource threshold violations, performance warnings

### 4. Platform-Specific Diagnostic Flags

#### Windows Event Logging
- **`VSTSAGENT_DUMP_JOB_EVENT_LOGS=true`**
  - **Purpose**: Windows event log capture during job execution
  - **Impact**: Captures system event logs for debugging
  - **Current Usage**: Behind flag
  - **Content**: Windows event log entries, system events during job execution

#### Linux Package Verification
- **`VSTSAGENT_DUMP_PACKAGES_VERIFICATION_RESULTS=true`**
  - **Purpose**: Package verification debugging on Linux
  - **Impact**: Detailed package integrity checking
  - **Current Usage**: Behind flag
  - **Content**: Package verification results, dependency analysis

### 5. Pipeline and Task Debugging

#### Pipeline Debug Mode
- **`System.Debug=true`** (Pipeline Variable)
  - **Purpose**: Enables verbose output for tasks and pipeline steps
  - **Impact**: Increases verbosity of task execution logs
  - **Current Usage**: User-controlled pipeline variable
  - **Content**: Task input parameters, detailed task execution steps, debug output from tasks

#### Task-Specific Debugging
- **`VSTSAGENT_DEBUG_TASK=true`**
  - **Purpose**: Task-level debugging and diagnostics
  - **Impact**: Detailed task execution tracing
  - **Current Usage**: Behind flag
  - **Content**: Task lifecycle, input processing, handler execution details

### 6. Security and Secret Management

#### Secret Masking Telemetry
- **`AZP_SEND_SECRET_MASKER_TELEMETRY=true`**
  - **Purpose**: Secret masker telemetry and debugging
  - **Impact**: Logs secret masking operations (without revealing secrets)
  - **Current Usage**: Behind flag
  - **Content**: Secret masking statistics, pattern matching metrics

#### New Masker Framework
- **`AZP_ENABLE_NEW_MASKER_AND_REGEXES=true`**
  - **Purpose**: Enables new secret masking framework
  - **Impact**: Uses enhanced secret detection patterns
  - **Current Usage**: Behind flag
  - **Content**: Enhanced secret pattern detection

### 7. Custom Log Paths

#### Agent Log Path Override
- **`AGENT_DIAGLOGPATH=<path>`**
  - **Purpose**: Custom location for agent listener logs
  - **Impact**: Redirects `Agent_*.log` files to specified path
  - **Current Usage**: Optional override
  - **Default**: `_diag` folder

#### Worker Log Path Override
- **`WORKER_DIAGLOGPATH=<path>`**
  - **Purpose**: Custom location for worker logs
  - **Impact**: Redirects `Worker_*.log` files to specified path
  - **Current Usage**: Optional override
  - **Default**: `_diag` folder

## Log Content Analysis

### Normal Operational Logs (Always Present)

**Purpose**: Provide essential information for basic troubleshooting and monitoring
**Content Includes**:
- Job status changes (started, completed, failed)
- Task execution status and results
- Configuration loading and validation
- Connection and authentication status
- Standard error conditions and exceptions
- Basic performance metrics (job duration, task timing)

**Examples**:
```
[INFO] Job request 12345 for plan ABC123 job DEF456 received.
[INFO] Task 'Build Solution' started
[WARNING] Unable to generate workspace ID: Access denied
[ERROR] Worker Dispatch failed with an exception for job request 12345
```

### Flag-Controlled Diagnostic Logs

**Purpose**: Detailed debugging information for specific scenarios
**Content Includes**:
- Internal component state changes
- Detailed execution flow tracing
- HTTP request/response details
- Resource utilization metrics
- Platform-specific system events
- Secret masking operations
- Task input parameter processing

**Examples** (when flags enabled):
```
[VERBOSE] Retrieve previous WorkerDispather for job 12345
[VERBOSE] Setting env 'PATH' to '/usr/bin:/bin'
[VERBOSE] Loading default inputs for task
[HTTP] POST https://dev.azure.com/org/_apis/build/builds/123 (Headers: {...})
```

## Recommendations for Better Debugging

### Logs That Should Be Moved Outside Flags

Based on customer feedback and debugging scenarios, the following logs should be elevated from flag-controlled to always-enabled:

#### 1. Task Input Parameter Logging (P1 - Customer Required)
**Current State**: Behind `System.Debug` flag
**Recommendation**: Move to Info level (always enabled)
**Justification**: 
- Critical for debugging task failures
- Helps identify configuration issues quickly
- Low volume, high value information
- Commonly needed in production debugging

#### 2. Basic HTTP Communication Status
**Current State**: Behind `VSTS_AGENT_HTTPTRACE` flag
**Recommendation**: Move basic HTTP status (not full content) to Info level
**Justification**:
- Network connectivity issues are common
- Basic status doesn't expose sensitive data
- Helps identify infrastructure problems quickly

#### 3. Resource Threshold Warnings
**Current State**: Behind `AZP_ENABLE_RESOURCE_UTILIZATION_WARNINGS` flag
**Recommendation**: Move to Warning level (always enabled)
**Justification**:
- Helps prevent job failures due to resource constraints
- Early warning system for capacity issues
- No sensitive data exposure

#### 4. Task Handler Resolution
**Current State**: Behind verbose tracing flags
**Recommendation**: Move to Info level
**Justification**:
- Helps debug task loading and execution issues
- Critical for understanding task execution flow
- Minimal performance impact

### Logs That Should Remain Behind Flags

#### 1. Detailed HTTP Content
- Contains potentially sensitive information
- High volume, detailed debugging only
- Performance impact considerations

#### 2. Internal Component State Changes
- Very high volume
- Only needed for deep debugging
- Minimal value for standard troubleshooting

#### 3. Platform-Specific Event Dumps
- High volume system logs
- Platform-specific debugging scenarios
- Potential performance impact

## Implementation Strategy

### Phase 1: Critical Customer Needs (Immediate)
1. Move task input parameter logging to Info level
2. Add basic HTTP status logging (without content)
3. Enable resource threshold warnings by default

### Phase 2: Enhanced Debugging (Short-term)
1. Add task handler resolution logging
2. Improve error context in standard logs
3. Add connection status indicators

### Phase 3: Advanced Diagnostics (Medium-term)
1. Implement intelligent verbosity scaling
2. Add context-aware log filtering
3. Enhance diagnostic log management

## Flag Usage Patterns

### Customer Scenarios and Recommended Flags

| Scenario | Recommended Flags | Purpose |
|----------|------------------|---------|
| Task failing | `System.Debug=true` | Task execution details |
| Agent won't start | `VSTS_AGENT_TRACE=true` | Agent startup diagnostics |
| Network issues | `VSTS_AGENT_HTTPTRACE=true` | HTTP communication details |
| Performance problems | `VSTS_AGENT_PERFLOG=<path>` | Resource utilization metrics |
| Windows events | `VSTSAGENT_DUMP_JOB_EVENT_LOGS=true` | System event correlation |

### Flag Interaction Matrix

| Flag Combination | Effect | Use Case |
|------------------|--------|----------|
| `System.Debug` + `VSTS_AGENT_TRACE` | Full pipeline and agent verbosity | Complete troubleshooting |
| `VSTS_AGENT_HTTPTRACE` + `VSTS_AGENT_PERFLOG` | Network and performance analysis | Infrastructure debugging |
| `AZP_ENABLE_RESOURCE_MONITOR_DEBUG_OUTPUT` + `AZP_ENABLE_RESOURCE_UTILIZATION_WARNINGS` | Complete resource monitoring | Capacity planning |

## Conclusion

The Azure DevOps Agent has a sophisticated, multi-layered logging system that provides comprehensive diagnostic capabilities. While the current flag-based approach offers fine-grained control, moving certain high-value, low-risk logs to the default Info level would significantly improve the debugging experience for customers without compromising security or performance.

The recommended changes focus on providing essential debugging information by default while maintaining the advanced diagnostic capabilities behind appropriate flags for specialized troubleshooting scenarios.
