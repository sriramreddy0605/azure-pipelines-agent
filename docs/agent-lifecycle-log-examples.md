# Azure DevOps Agent Lifecycle - Enhanced Log Format Examples

## Overview

This document provides concrete examples of how the enhanced log format would appear during real Azure DevOps Agent lifecycle scenarios, with direct references to the TaskRunner.cs code and actual agent execution patterns.

## Complete Agent Lifecycle Example

### 1. Agent Startup and Job Acquisition

```
[2025-01-15T14:30:00.000Z] [INFO] [AGENT] [*:*:START] Azure DevOps Agent starting {version=3.250.0, platform=ubuntu-20.04, dotnet=8.0.1}
[2025-01-15T14:30:00.001Z] [INFO] [AGENT] [*:*:CONFIG] Configuration: Pool=Default, Agent=MyAgent-001, Workspace=/home/agent/_work
[2025-01-15T14:30:00.100Z] [INFO] [AGENT] [*:*:READY] Agent ready and listening for jobs
[2025-01-15T14:30:45.000Z] [INFO] [AGENT] [*:*:JOB_ACQUIRED] Job acquired from queue {request_id=12345, plan_id=ABC-123, job_id=J456}
```

### 2. Job Start - Worker Process Creation

```
[2025-01-15T14:30:45.100Z] [INFO] [JOB] [J456:*:START] Job started: Build and Deploy Application {worker_pid=8901, agent_pid=7890}
[2025-01-15T14:30:45.101Z] [INFO] [JOB] [J456:*:CONFIG] Job environment: ubuntu-latest, Pool=Default, Repository=MyOrg/MyRepo
[2025-01-15T14:30:45.102Z] [INFO] [JOB] [J456:*:WORKSPACE] Workspace initialized: /home/agent/_work/1 {disk_space=18.5GB}
```

### 3. Task Execution Lifecycle (Based on TaskRunner.cs)

#### Task 1: Build Solution - Complete Success Flow

```
[2025-01-15T14:30:46.100Z] [INFO] [TASK] [J456:BuildSolution:START] Starting task: Build solution **/*.sln {version=2.1.0, timeout=0}
[2025-01-15T14:30:46.101Z] [INFO] [TASK] [J456:BuildSolution:LOAD] Loading task definition {task_id=497d490f-eea7-4f2b-ab94-48d9c1acdcb1}
[2025-01-15T14:30:46.102Z] [INFO] [TASK] [J456:BuildSolution:VERIFY] Task signature verification: Enabled {mode=Warning}
[2025-01-15T14:30:46.103Z] [INFO] [TASK] [J456:BuildSolution:HANDLER] Selected execution handler: Node {version=18.17.0, target=host, preferred=true}
[2025-01-15T14:30:46.104Z] [INFO] [TASK] [J456:BuildSolution:INPUT] Task inputs: solution='**/*.sln', configuration='Release', platform='Any CPU', msbuildArgs=''
[2025-01-15T14:30:46.105Z] [INFO] [TASK] [J456:BuildSolution:INPUT] Working directory: '/home/agent/_work/1/s'
[2025-01-15T14:30:46.106Z] [DEBUG] [TASK] [J456:BuildSolution:INPUT] Expanded inputs: solution='**/*.sln' -> '/home/agent/_work/1/s/**/*.sln'
[2025-01-15T14:30:46.107Z] [DEBUG] [TASK] [J456:BuildSolution:ENV] Environment variables: DOTNET_ROOT='/usr/share/dotnet', PATH='/usr/bin:/bin'
[2025-01-15T14:30:46.150Z] [INFO] [TASK] [J456:BuildSolution:EXEC] Executing: node /home/agent/_work/_tasks/VSBuild_71a9a2d3-a98a-4caa-96ab-affca411ecda/2.1.0/vsbuild.js
[2025-01-15T14:30:50.250Z] [INFO] [TASK] [J456:BuildSolution:COMPLETE] Task completed successfully {duration=4.15s, warnings=0, exit_code=0}
```

#### Task 2: NuGet Restore - Failure with Recovery Guidance

```
[2025-01-15T14:30:50.300Z] [INFO] [TASK] [J456:NuGetRestore:START] Starting task: NuGet restore {version=2.0.1, timeout=0}
[2025-01-15T14:30:50.301Z] [INFO] [TASK] [J456:NuGetRestore:INPUT] Task inputs: restoreSolution='**/*.sln', includeNuGetOrg='true', noCache='false'
[2025-01-15T14:30:50.350Z] [INFO] [TASK] [J456:NuGetRestore:EXEC] Executing: nuget restore MySolution.sln
[2025-01-15T14:30:52.100Z] [ERROR] [TASK] [J456:NuGetRestore:FAILED] Task failed: Package restore failed {duration=1.8s, exit_code=1}
[2025-01-15T14:30:52.101Z] [ERROR] [TASK] [J456:NuGetRestore:FAILED] Root cause: Unable to connect to NuGet.org (503 Service Unavailable)
[2025-01-15T14:30:52.102Z] [INFO] [TASK] [J456:NuGetRestore:FAILED] Troubleshooting steps:
  1. Check connectivity: curl -I https://api.nuget.org/v3/index.json
  2. Verify firewall allows HTTPS to *.nuget.org
  3. Consider using internal NuGet feed as fallback
[2025-01-15T14:30:52.103Z] [INFO] [TASK] [J456:NuGetRestore:FAILED] Documentation: https://docs.microsoft.com/nuget/consume-packages/package-restore-troubleshooting
[2025-01-15T14:30:52.104Z] [DEBUG] [TASK] [J456:NuGetRestore:ERROR] Exception: System.Net.Http.HttpRequestException at NuGet.Protocol.HttpSource.GetAsync()
```

#### Task 3: Container Task - Advanced Context

```
[2025-01-15T14:30:53.000Z] [INFO] [TASK] [J456:DockerBuild:START] Starting task: Docker build {version=1.0.0, timeout=10}
[2025-01-15T14:30:53.001Z] [INFO] [TASK] [J456:DockerBuild:CONTAINER] Running in container: ubuntu:20.04 {id=abc123def456, network=bridge}
[2025-01-15T14:30:53.002Z] [INFO] [TASK] [J456:DockerBuild:HANDLER] Selected execution handler: Node {version=18.17.0, target=container, preferred_for_container=true}
[2025-01-15T14:30:53.003Z] [INFO] [TASK] [J456:DockerBuild:INPUT] Task inputs: dockerFile='Dockerfile', buildContext='.', imageName='myapp:latest'
[2025-01-15T14:30:53.004Z] [DEBUG] [TASK] [J456:DockerBuild:CONTAINER] Container path translation: '/home/agent/_work/1/s' -> '/workspace'
[2025-01-15T14:30:58.500Z] [INFO] [TASK] [J456:DockerBuild:COMPLETE] Task completed successfully {duration=5.5s, warnings=1, exit_code=0}
```

### 4. Resource Monitoring During Execution

```
[2025-01-15T14:30:47.000Z] [INFO] [WORKER] [J456:*:MONITOR] Resource monitoring enabled {memory_threshold=85%, disk_threshold=90%}
[2025-01-15T14:30:47.500Z] [WARN] [WORKER] [J456:*:MONITOR] High memory usage detected: 87% (6.96GB/8GB) during BuildSolution
[2025-01-15T14:30:48.200Z] [WARN] [WORKER] [J456:*:MONITOR] Low disk space: 8% remaining (1.6GB/20GB) on build drive
[2025-01-15T14:30:48.201Z] [INFO] [WORKER] [J456:*:MONITOR] Recommendation: Consider using smaller build agents or cleanup workspace between jobs
```

### 5. HTTP Communications and Network

```
[2025-01-15T14:30:45.200Z] [INFO] [HTTP] [J456:*:COMM] Connecting to Azure DevOps: https://dev.azure.com/myorg
[2025-01-15T14:30:45.350Z] [INFO] [HTTP] [J456:*:COMM] Connection established {status=200, duration=150ms, server=Azure-DevOps/1.0}
[2025-01-15T14:30:51.000Z] [WARN] [HTTP] [J456:*:COMM] Slow response detected {duration=1.2s, endpoint='/apis/distributedtask/hubs/build/plans/ABC-123/events'}
[2025-01-15T14:30:51.001Z] [DEBUG] [HTTP] [J456:*:COMM] Request: POST /apis/distributedtask/hubs/build/plans/ABC-123/events {headers=8, body_size=1.2KB}
```

### 6. Task Retry Scenario (from TaskRunner.cs retry logic)

```
[2025-01-15T14:30:55.000Z] [INFO] [TASK] [J456:DeployToStaging:START] Starting task: Deploy to staging {version=1.2.0, timeout=30, retry_count=3}
[2025-01-15T14:30:58.000Z] [ERROR] [TASK] [J456:DeployToStaging:RETRY] Task failed, retrying: Connection timeout {attempt=1/3, next_retry=2s}
[2025-01-15T14:31:00.000Z] [INFO] [TASK] [J456:DeployToStaging:RETRY] Retry attempt 2 starting {delay=2s, backoff=exponential}
[2025-01-15T14:31:03.000Z] [ERROR] [TASK] [J456:DeployToStaging:RETRY] Task failed, retrying: Connection timeout {attempt=2/3, next_retry=4s}
[2025-01-15T14:31:07.000Z] [INFO] [TASK] [J456:DeployToStaging:RETRY] Retry attempt 3 starting {delay=4s, backoff=exponential}
[2025-01-15T14:31:12.000Z] [INFO] [TASK] [J456:DeployToStaging:COMPLETE] Task completed successfully {duration=17s, attempts=3, warnings=1}
```

### 7. Job Completion and Cleanup

```
[2025-01-15T14:31:15.000Z] [INFO] [JOB] [J456:*:CLEANUP] Starting job cleanup {tasks_completed=5, tasks_failed=1, tasks_skipped=0}
[2025-01-15T14:31:15.100Z] [INFO] [JOB] [J456:*:ARTIFACTS] Uploading job artifacts {files=15, total_size=125MB}
[2025-01-15T14:31:18.500Z] [INFO] [JOB] [J456:*:COMPLETE] Job completed with warnings {duration=33.4s, result=PartiallySucceeded}
[2025-01-15T14:31:18.600Z] [INFO] [WORKER] [J456:*:SHUTDOWN] Worker process shutting down {pid=8901, exit_code=0}
[2025-01-15T14:31:18.700Z] [INFO] [AGENT] [*:*:READY] Agent ready and listening for jobs
```

## TaskRunner.cs Specific Examples

### Input Processing (Lines 210-232 in TaskRunner.cs)

```
[2025-01-15T14:30:46.200Z] [DEBUG] [TASK] [J456:MSBuild:INPUT] Loading default inputs from task definition
[2025-01-15T14:30:46.201Z] [DEBUG] [TASK] [J456:MSBuild:INPUT] Default input: msbuildArgs='' (from task.json)
[2025-01-15T14:30:46.202Z] [DEBUG] [TASK] [J456:MSBuild:INPUT] Loading instance inputs from pipeline
[2025-01-15T14:30:46.203Z] [DEBUG] [TASK] [J456:MSBuild:INPUT] Instance input: msbuildArgs='/p:Configuration=Release /p:Platform="Any CPU"'
[2025-01-15T14:30:46.204Z] [INFO] [TASK] [J456:MSBuild:INPUT] Final inputs: solution='**/*.sln', msbuildArgs='/p:Configuration=Release /p:Platform="Any CPU"'
[2025-01-15T14:30:46.205Z] [DEBUG] [TASK] [J456:MSBuild:INPUT] Input expansion: $(Build.SourcesDirectory) -> '/home/agent/_work/1/s'
```

### Handler Selection (Lines 530-560 in TaskRunner.cs)

```
[2025-01-15T14:30:46.300Z] [DEBUG] [TASK] [J456:NodeTask:HANDLER] Available handlers: Node16, PowerShell3 {platform=ubuntu-20.04}
[2025-01-15T14:30:46.301Z] [DEBUG] [TASK] [J456:NodeTask:HANDLER] Platform preference: Node16 preferred for ubuntu-20.04
[2025-01-15T14:30:46.302Z] [DEBUG] [TASK] [J456:NodeTask:HANDLER] Container context: targeting host environment
[2025-01-15T14:30:46.303Z] [INFO] [TASK] [J456:NodeTask:HANDLER] Selected handler: Node16 {script=task.js, target=host, preferred=true}
```

### Container Task Execution (Lines 155-200 in TaskRunner.cs)

```
[2025-01-15T14:30:47.000Z] [INFO] [TASK] [J456:ContainerTask:CONTAINER] Container target detected: ubuntu:20.04 {id=abc123}
[2025-01-15T14:30:47.001Z] [DEBUG] [TASK] [J456:ContainerTask:CONTAINER] Checking container status before execution
[2025-01-15T14:30:47.002Z] [INFO] [TASK] [J456:ContainerTask:CONTAINER] Container is running: ubuntu:20.04 {status=running, uptime=5m}
[2025-01-15T14:30:47.003Z] [DEBUG] [TASK] [J456:ContainerTask:CONTAINER] Setting up container step host
[2025-01-15T14:30:47.004Z] [DEBUG] [TASK] [J456:ContainerTask:CONTAINER] Variable translation for container context
[2025-01-15T14:30:47.005Z] [INFO] [TASK] [J456:ContainerTask:HANDLER] Container-compatible handler: Node16 {target=container}
```

### File Path Translation (Lines 250-270 in TaskRunner.cs)

```
[2025-01-15T14:30:46.400Z] [DEBUG] [TASK] [J456:PublishResults:FILEPATH] Translating file path inputs
[2025-01-15T14:30:46.401Z] [DEBUG] [TASK] [J456:PublishResults:FILEPATH] Input 'testResultsFiles': '**/*.trx' (FilePath type)
[2025-01-15T14:30:46.402Z] [DEBUG] [TASK] [J456:PublishResults:FILEPATH] Resolved path: '/home/agent/_work/1/s/**/*.trx'
[2025-01-15T14:30:46.403Z] [DEBUG] [TASK] [J456:PublishResults:FILEPATH] Path validation: pattern is valid, directory exists
[2025-01-15T14:30:46.404Z] [INFO] [TASK] [J456:PublishResults:INPUT] File path inputs: testResultsFiles='/home/agent/_work/1/s/**/*.trx'
```

### Resource Monitoring (Lines 408-430 in TaskRunner.cs)

```
[2025-01-15T14:30:46.500Z] [INFO] [TASK] [J456:BuildTask:MONITOR] Resource utilization monitoring: Enabled
[2025-01-15T14:30:46.501Z] [DEBUG] [TASK] [J456:BuildTask:MONITOR] Starting memory utilization monitor {threshold=85%}
[2025-01-15T14:30:46.502Z] [DEBUG] [TASK] [J456:BuildTask:MONITOR] Starting disk space utilization monitor {threshold=90%}
[2025-01-15T14:30:46.503Z] [DEBUG] [TASK] [J456:BuildTask:MONITOR] Starting CPU utilization monitor {task_id=497d490f}
[2025-01-15T14:30:48.000Z] [WARN] [TASK] [J456:BuildTask:MONITOR] Memory threshold exceeded: 87% (6.96GB/8GB) during MSBuild execution
```

### Error Scenarios with Context

#### Task Definition Loading Error
```
[2025-01-15T14:30:46.100Z] [ERROR] [TASK] [J456:CustomTask:LOAD] Task definition loading failed: Task not found
[2025-01-15T14:30:46.101Z] [ERROR] [TASK] [J456:CustomTask:LOAD] Root cause: Task ID 'unknown-task-id' does not exist in task cache
[2025-01-15T14:30:46.102Z] [INFO] [TASK] [J456:CustomTask:LOAD] Troubleshooting:
  1. Verify task name and version in pipeline YAML
  2. Check if task is available in Azure DevOps marketplace
  3. Ensure task is installed in your organization
[2025-01-15T14:30:46.103Z] [DEBUG] [TASK] [J456:CustomTask:LOAD] Task cache directory: /home/agent/_work/_tasks (152 tasks cached)
```

#### Handler Compatibility Error
```
[2025-01-15T14:30:46.200Z] [ERROR] [TASK] [J456:LegacyTask:HANDLER] No compatible handler found: PowerShell not supported in container
[2025-01-15T14:30:46.201Z] [ERROR] [TASK] [J456:LegacyTask:HANDLER] Root cause: Task requires PowerShell handler but running in Linux container
[2025-01-15T14:30:46.202Z] [INFO] [TASK] [J456:LegacyTask:HANDLER] Solutions:
  1. Use 'windows-latest' agent for PowerShell tasks
  2. Update task to support Node.js handler
  3. Run task outside container context
[2025-01-15T14:30:46.203Z] [DEBUG] [TASK] [J456:LegacyTask:HANDLER] Available handlers: PowerShell {platforms=[windows]}, unavailable in container
```

#### Timeout and Cancellation
```
[2025-01-15T14:30:46.000Z] [INFO] [TASK] [J456:LongTask:START] Starting task: Long running task {timeout=5m}
[2025-01-15T14:35:46.000Z] [WARN] [TASK] [J456:LongTask:TIMEOUT] Task timeout approaching: 30s remaining {elapsed=4m30s}
[2025-01-15T14:36:16.000Z] [ERROR] [TASK] [J456:LongTask:TIMEOUT] Task timed out: Exceeded 5 minute limit {duration=5m30s}
[2025-01-15T14:36:16.001Z] [INFO] [TASK] [J456:LongTask:TIMEOUT] Task process terminated: SIGTERM sent to handler process {pid=9001}
[2025-01-15T14:36:16.002Z] [INFO] [TASK] [J456:LongTask:TIMEOUT] Recommendation: Increase timeoutInMinutes or optimize task performance
```

## Performance Analysis Examples

### Build Performance Breakdown
```
[2025-01-15T14:30:50.250Z] [INFO] [TASK] [J456:BuildSolution:PERF] Performance summary:
  Setup: 0.15s (4%)
  Compilation: 3.50s (84%) 
  Linking: 0.40s (10%)
  Cleanup: 0.10s (2%)
  Total: 4.15s
[2025-01-15T14:30:50.251Z] [INFO] [TASK] [J456:BuildSolution:PERF] Resource usage: Peak Memory=2.1GB, Peak CPU=78%, I/O=250MB
```

### Network Performance Analysis
```
[2025-01-15T14:30:52.000Z] [INFO] [HTTP] [J456:*:PERF] Network performance summary:
  Total requests: 15
  Average response time: 245ms
  Slowest request: 1.2s (GET /apis/build/artifacts)
  Failed requests: 0
  Data transferred: 4.5MB down, 1.2MB up
```

This enhanced format provides clear visibility into the agent lifecycle while maintaining the technical detail needed for effective debugging. Each log entry follows the structured format and includes relevant context for both pipeline users and developers.
