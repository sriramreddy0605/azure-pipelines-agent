# Azure DevOps Agent - Enhanced Log Format Specification

## Overview

This document defines a standardized, human-readable log format for the Azure DevOps Agent that provides clear information for pipeline users while including sufficient technical detail for developers to diagnose issues effectively.

## Core Design Principles

1. **Human-First**: Messages should be immediately understandable by pipeline users
2. **Contextual**: Include relevant context without overwhelming detail
3. **Actionable**: Provide clear guidance on what went wrong and how to fix it
4. **Structured**: Consistent format across all logging levels
5. **Searchable**: Easy to grep and filter for specific information
6. **Progressive Detail**: More detail available at higher verbosity levels

## Log Format Structure

### Base Format Pattern
```
[TIMESTAMP] [LEVEL] [COMPONENT] [CONTEXT] MESSAGE [METADATA]
```

### Component Breakdown

#### 1. Timestamp
```
Format: ISO 8601 with milliseconds
Example: 2025-01-15T14:30:45.123Z
```

#### 2. Level
```
INFO    - Normal operational information
WARN    - Warning conditions that don't stop execution
ERROR   - Error conditions that may cause task/job failure
DEBUG   - Detailed diagnostic information (System.Debug=true)
TRACE   - Very detailed internal information (Agent debug flags)
```

#### 3. Component
```
AGENT    - Agent listener operations
WORKER   - Worker process operations
TASK     - Task execution
JOB      - Job-level operations
HTTP     - Network communications
AUTH     - Authentication operations
SETUP    - Environment setup
CLEANUP  - Cleanup operations
```

#### 4. Context
```
Format: [JobId:TaskName:Phase]
Example: [J123:BuildSolution:SETUP] or [J123:*:STARTUP] or [*:*:CONFIG]
```

#### 5. Metadata (Optional)
```
Format: {key1=value1, key2=value2}
Example: {duration=1.2s, handler=Node, platform=windows}
```

## Log Format Examples

### 1. Job and Task Lifecycle Logs

#### Job Start
```
[2025-01-15T14:30:45.123Z] [INFO] [JOB] [J123:*:START] Job started: Build and Deploy Application
[2025-01-15T14:30:45.124Z] [INFO] [JOB] [J123:*:CONFIG] Agent: MyAgent-001, Pool: Default, Platform: ubuntu-latest
```

#### Task Execution Start
```
[2025-01-15T14:30:46.100Z] [INFO] [TASK] [J123:BuildSolution:START] Starting task: Build solution {version=2.1.0, handler=Node}
[2025-01-15T14:30:46.101Z] [INFO] [TASK] [J123:BuildSolution:SETUP] Configuring build environment
[2025-01-15T14:30:46.102Z] [INFO] [TASK] [J123:BuildSolution:INPUT] Task inputs: solution='**/*.sln', configuration='Release', platform='Any CPU'
```

#### Task Success
```
[2025-01-15T14:30:50.250Z] [INFO] [TASK] [J123:BuildSolution:COMPLETE] Task completed successfully {duration=4.15s, warnings=0, exit_code=0}
```

#### Task Failure
```
[2025-01-15T14:30:50.250Z] [ERROR] [TASK] [J123:BuildSolution:FAILED] Task failed: Build solution could not find project files {duration=4.15s, exit_code=1}
[2025-01-15T14:30:50.251Z] [ERROR] [TASK] [J123:BuildSolution:FAILED] Issue: No solution files found matching pattern '**/*.sln'
[2025-01-15T14:30:50.252Z] [INFO] [TASK] [J123:BuildSolution:FAILED] Suggestion: Verify the solution file path or check if files exist in the working directory '/home/agent/_work/1/s'
```

### 2. Input Parameter Logging (P1 Requirement)

#### Standard Level (Always Visible)
```
[2025-01-15T14:30:46.102Z] [INFO] [TASK] [J123:BuildSolution:INPUT] Task inputs: solution='**/*.sln', configuration='Release', platform='Any CPU'
[2025-01-15T14:30:46.103Z] [INFO] [TASK] [J123:BuildSolution:INPUT] Working directory: '/home/agent/_work/1/s'
```

#### Debug Level (System.Debug=true)
```
[2025-01-15T14:30:46.104Z] [DEBUG] [TASK] [J123:BuildSolution:INPUT] Expanded inputs: solution='**/*.sln' -> '/home/agent/_work/1/s/**/*.sln'
[2025-01-15T14:30:46.105Z] [DEBUG] [TASK] [J123:BuildSolution:INPUT] Environment variables: MSBuildArgs='-p:Configuration=Release', DOTNET_ROOT='/usr/share/dotnet'
[2025-01-15T14:30:46.106Z] [DEBUG] [TASK] [J123:BuildSolution:INPUT] Handler inputs: script='build.js', workingDirectory='/home/agent/_work/1/s'
```

### 3. Resource and Performance Logging

#### Resource Warnings (Always Visible)
```
[2025-01-15T14:30:47.500Z] [WARN] [WORKER] [J123:*:MONITOR] High memory usage detected: 85% (6.8GB/8GB) - Consider optimizing build process
[2025-01-15T14:30:48.200Z] [WARN] [WORKER] [J123:*:MONITOR] Low disk space: 12% remaining (2.4GB/20GB) on build drive
```

#### Performance Metrics (Info Level)
```
[2025-01-15T14:30:50.250Z] [INFO] [TASK] [J123:BuildSolution:PERF] Performance: CPU=45%, Memory=2.1GB, Duration=4.15s
```

### 4. Network and HTTP Logging

#### Basic HTTP Status (Info Level)
```
[2025-01-15T14:30:45.200Z] [INFO] [HTTP] [J123:*:COMM] Connecting to Azure DevOps: https://dev.azure.com/myorg
[2025-01-15T14:30:45.350Z] [INFO] [HTTP] [J123:*:COMM] Connection established {status=200, duration=150ms}
[2025-01-15T14:30:45.351Z] [WARN] [HTTP] [J123:*:COMM] Slow response detected {duration=1.2s, endpoint='/apis/build/builds'}
```

#### Detailed HTTP (Debug Level)
```
[2025-01-15T14:30:45.200Z] [DEBUG] [HTTP] [J123:*:COMM] Request: POST /apis/build/builds {headers=8, body_size=1.2KB}
[2025-01-15T14:30:45.350Z] [DEBUG] [HTTP] [J123:*:COMM] Response: 200 OK {headers=12, body_size=4.5KB, server=nginx/1.20}
```

### 5. Error and Exception Logging

#### User-Friendly Errors
```
[2025-01-15T14:30:48.100Z] [ERROR] [TASK] [J123:NuGetRestore:FAILED] Package restore failed: Unable to connect to NuGet.org
[2025-01-15T14:30:48.101Z] [ERROR] [TASK] [J123:NuGetRestore:FAILED] Network error: The remote server returned an error: (503) Service Unavailable
[2025-01-15T14:30:48.102Z] [INFO] [TASK] [J123:NuGetRestore:FAILED] Troubleshooting: 
  1. Check internet connectivity to https://api.nuget.org/v3/index.json
  2. Verify firewall and proxy settings
  3. Try using a different NuGet package source
[2025-01-15T14:30:48.103Z] [INFO] [TASK] [J123:NuGetRestore:FAILED] Documentation: https://docs.microsoft.com/azure/devops/pipelines/troubleshooting/nuget
```

#### Developer Debug Info
```
[2025-01-15T14:30:48.104Z] [DEBUG] [TASK] [J123:NuGetRestore:ERROR] Exception details: System.Net.Http.HttpRequestException
[2025-01-15T14:30:48.105Z] [DEBUG] [TASK] [J123:NuGetRestore:ERROR] Stack trace: at TaskRunner.ExecuteAsync() line 245
[2025-01-15T14:30:48.106Z] [DEBUG] [TASK] [J123:NuGetRestore:ERROR] Request context: {url='https://api.nuget.org/v3/index.json', timeout=30s, retry_count=3}
```

### 6. Agent and System Logging

#### Agent Startup
```
[2025-01-15T14:30:00.000Z] [INFO] [AGENT] [*:*:START] Azure DevOps Agent starting {version=3.250.0, platform=ubuntu-20.04, dotnet=8.0.1}
[2025-01-15T14:30:00.001Z] [INFO] [AGENT] [*:*:CONFIG] Configuration: Pool=Default, Agent=MyAgent-001, Workspace=/home/agent/_work
[2025-01-15T14:30:00.100Z] [INFO] [AGENT] [*:*:READY] Agent ready and listening for jobs
```

#### Platform and Environment
```
[2025-01-15T14:30:45.110Z] [INFO] [SETUP] [J123:*:ENV] Build environment: ubuntu-20.04, dotnet 8.0.1, node 18.17.0
[2025-01-15T14:30:45.111Z] [INFO] [SETUP] [J123:*:ENV] Working directory: /home/agent/_work/1/s (18.5GB available)
[2025-01-15T14:30:45.112Z] [DEBUG] [SETUP] [J123:*:ENV] Environment variables: PATH, HOME, USER, CI=true, SYSTEM_DEFAULTWORKINGDIRECTORY=/home/agent/_work/1/s
```

### 7. Handler and Execution Context

#### Handler Selection
```
[2025-01-15T14:30:46.050Z] [INFO] [TASK] [J123:BuildSolution:HANDLER] Selected execution handler: Node {version=18.17.0, target=host}
[2025-01-15T14:30:46.051Z] [DEBUG] [TASK] [J123:BuildSolution:HANDLER] Available handlers: Node (preferred), PowerShell3, deprecated PowerShell
[2025-01-15T14:30:46.052Z] [DEBUG] [TASK] [J123:BuildSolution:HANDLER] Handler script: /opt/hostedtoolcache/node/18.17.0/x64/bin/node task.js
```

#### Container Context
```
[2025-01-15T14:30:46.060Z] [INFO] [TASK] [J123:BuildSolution:CONTAINER] Running in container: ubuntu:20.04 {id=abc123def456}
[2025-01-15T14:30:46.061Z] [DEBUG] [TASK] [J123:BuildSolution:CONTAINER] Container details: image=ubuntu:20.04, network=bridge, volumes=3
```

## Verbosity Levels and Content Guidelines

### INFO Level (Default - Always Visible)
**Target Audience**: Pipeline users, DevOps engineers
**Content**:
- Job and task start/complete status
- Task input parameters (P1 requirement)
- Basic performance metrics
- Resource warnings
- Network connection status
- Clear error messages with user guidance
- Success/failure summaries

### DEBUG Level (System.Debug=true)
**Target Audience**: Advanced users, task developers
**Content**:
- Detailed input expansion and processing
- Environment variable details
- Handler selection logic
- File path translations
- Basic exception information
- Performance breakdown

### TRACE Level (Agent Debug Flags)
**Target Audience**: Agent developers, Microsoft support
**Content**:
- Internal state changes
- Detailed HTTP request/response
- Memory and resource usage details
- Full exception stack traces
- Inter-process communication
- Security and authentication details

## Error Message Format Guidelines

### Structure for User-Facing Errors
```
[ERROR] [COMPONENT] [CONTEXT] Primary error message: Brief description
[ERROR] [COMPONENT] [CONTEXT] Root cause: Technical reason (if known)
[INFO]  [COMPONENT] [CONTEXT] Suggestion: Actionable remediation steps
[INFO]  [COMPONENT] [CONTEXT] Documentation: Link to relevant docs
```

### Example Error Scenarios

#### Authentication Error
```
[ERROR] [AUTH] [J123:*:SETUP] Authentication failed: Invalid or expired credentials
[ERROR] [AUTH] [J123:*:SETUP] Root cause: Personal access token has expired
[INFO]  [AUTH] [J123:*:SETUP] Suggestion: Update your service connection with a new personal access token
[INFO]  [AUTH] [J123:*:SETUP] Documentation: https://docs.microsoft.com/azure/devops/pipelines/library/service-endpoints
```

#### File Not Found Error
```
[ERROR] [TASK] [J123:PublishArtifacts:FAILED] Artifact publishing failed: Cannot find file or directory
[ERROR] [TASK] [J123:PublishArtifacts:FAILED] Root cause: Path '$(Build.ArtifactStagingDirectory)/drop' does not exist
[INFO]  [TASK] [J123:PublishArtifacts:FAILED] Suggestion: Ensure previous build tasks create artifacts in the staging directory
[INFO]  [TASK] [J123:PublishArtifacts:FAILED] Working directory: /home/agent/_work/1/a (0 files)
```

## Search and Filter Patterns

### Common Grep Patterns
```bash
# All errors for a specific job
grep "\[ERROR\].*\[J123:" agent.log

# Task input parameters across all tasks
grep "\[INFO\].*INPUT\]" agent.log

# Performance and timing information
grep "duration=" agent.log

# All HTTP communication
grep "\[HTTP\]" agent.log

# Resource warnings
grep "\[WARN\].*MONITOR\]" agent.log
```

### Log Analysis Examples
```bash
# Task execution timeline
grep "\[J123:BuildSolution:" agent.log | grep -E "(START|COMPLETE|FAILED)"

# Network connectivity issues
grep "\[HTTP\].*\[WARN\|ERROR\]" agent.log

# Input parameter debugging
grep "\[J123:.*:INPUT\]" agent.log
```

## Implementation Guidelines

### Consistency Rules
1. **Timestamps**: Always UTC, ISO 8601 format with milliseconds
2. **Context**: Always include job ID when available
3. **Duration**: Always in human-readable format (1.2s, 150ms, 2.5m)
4. **File Paths**: Always use forward slashes, show full paths when relevant
5. **Sizes**: Use human-readable format (1.2KB, 4.5MB, 2.1GB)

### Performance Considerations
1. **Metadata**: Only include relevant metadata, avoid JSON dumps
2. **Truncation**: Truncate very long paths or values (>100 chars)
3. **Buffering**: Batch logs for better performance
4. **Filtering**: Respect verbosity levels to avoid overwhelming output

### Localization
1. **Error Messages**: Use StringUtil.Loc() for user-facing messages
2. **Technical Details**: Keep technical metadata in English for consistency
3. **Context**: Provide both localized and technical context where needed

## Sample Log Flow for Complete Task Execution

```
[2025-01-15T14:30:45.123Z] [INFO] [JOB] [J123:*:START] Job started: Build and Deploy Application
[2025-01-15T14:30:45.200Z] [INFO] [HTTP] [J123:*:COMM] Connection established {status=200, duration=150ms}
[2025-01-15T14:30:46.100Z] [INFO] [TASK] [J123:BuildSolution:START] Starting task: Build solution {version=2.1.0, handler=Node}
[2025-01-15T14:30:46.102Z] [INFO] [TASK] [J123:BuildSolution:INPUT] Task inputs: solution='**/*.sln', configuration='Release', platform='Any CPU'
[2025-01-15T14:30:46.103Z] [INFO] [TASK] [J123:BuildSolution:INPUT] Working directory: '/home/agent/_work/1/s'
[2025-01-15T14:30:46.150Z] [INFO] [TASK] [J123:BuildSolution:EXEC] Executing: dotnet build MySolution.sln -c Release
[2025-01-15T14:30:50.250Z] [INFO] [TASK] [J123:BuildSolution:COMPLETE] Task completed successfully {duration=4.15s, warnings=0, exit_code=0}
[2025-01-15T14:30:50.300Z] [INFO] [JOB] [J123:*:COMPLETE] Job completed successfully {duration=5.2s, tasks=3, warnings=0}
```

This log format provides clear, actionable information for pipeline users while maintaining the technical detail needed for effective debugging and troubleshooting.
