# Azure DevOps Agent: Complete Execution Lifecycle Documentation

## Overview
This document provides a comprehensive walkthrough of the Azure DevOps Agent execution lifecycle from when the agent starts listening for jobs to when task execution completes. The agent operates with a multi-process architecture consisting of a persistent **Listener** process and ephemeral **Worker** processes.

## Architecture Components

### 1. Agent.Listener Process (Persistent)
- **Entry Point**: `src/Agent.Listener/Program.cs::Main()`
- **Purpose**: Long-running process that listens for job messages from Azure DevOps server
- **Lifespan**: Runs continuously until agent shutdown
- **Key Components**: `Agent`, `JobDispatcher`, `MessageListener`

### 2. Agent.Worker Process (Ephemeral)
- **Entry Point**: `src/Agent.Worker/Program.cs::Main()`
- **Purpose**: Executes individual jobs and their tasks
- **Lifespan**: Created for each job, terminated after job completion
- **Key Components**: `Worker`, `JobRunner`, `StepsRunner`, `TaskRunner`

## Complete Execution Flow

### Phase 1: Agent Initialization and Listening

#### 1.1 Agent Listener Startup
```
Agent.Listener/Program.cs::Main()
├── MainAsync()
│   ├── HostContext creation (Agent type)
│   ├── Command line parsing
│   ├── Environment validation
│   ├── Process priority elevation (Windows)
│   └── Agent.ExecuteCommand()
```

#### 1.2 Agent Service Initialization
```
Agent.cs::ExecuteCommand()
├── Configuration loading
├── Banner printing
├── Debug mode handling
├── Telemetry initialization
└── RunAsync() - Main listening loop
```

#### 1.3 Message Listener Setup
```
Agent.cs::RunAsync()
├── MessageListener.CreateSessionAsync()
│   ├── Server connection establishment
│   ├── Authentication with OAuth/PAT
│   └── Session creation with Azure DevOps
├── JobDispatcher initialization
├── JobNotification service start
└── Message polling loop initiation
```

### Phase 2: Job Request Reception and Processing

#### 2.1 Message Reception
```
Agent.cs::RunAsync() - Message Loop
├── MessageListener.GetNextMessageAsync()
│   ├── Long polling to Azure DevOps
│   ├── Message type detection:
│   │   ├── AgentJobRequest
│   │   ├── PipelineAgentJobRequest
│   │   ├── JobCancelMessage
│   │   ├── JobMetadataMessage
│   │   └── AgentRefreshMessage
│   └── Message deserialization
```

#### 2.2 Job Dispatch Decision
```
Agent.cs::RunAsync() - Job Message Processing
├── Message type validation
├── Auto-update conflict checking
├── Pipeline job message conversion
└── JobDispatcher.Run(pipelineJobMessage, runOnce)
```

#### 2.3 Job Dispatcher Initialization
```
JobDispatcher.cs::Run()
├── Previous job cleanup check
├── WorkerDispatcher creation
├── Job dispatch mode selection:
│   ├── RunAsync() - Normal mode
│   └── RunOnceAsync() - Single-use agent mode
└── Job queue management
```

### Phase 3: Worker Process Creation and Communication

#### 3.1 Worker Process Setup
```
JobDispatcher.cs::RunAsync()
├── Previous job completion wait
├── Job lock renewal initiation
├── Process channel establishment
├── Worker process creation:
│   ├── Worker binary: "Agent.Worker.exe"
│   ├── Arguments: "spawnclient {pipeOut} {pipeIn}"
│   ├── Environment variables setup
│   └── IPC pipe configuration
```

#### 3.2 Worker-Listener Communication
```
Communication Architecture:
┌─────────────────┐    IPC Pipes    ┌─────────────────┐
│  Agent.Listener │ ←──────────────→ │  Agent.Worker   │
│  (JobDispatcher)│                 │   (Worker)      │
└─────────────────┘                 └─────────────────┘

Message Types:
├── NewJobRequest - Job data transfer
├── CancelRequest - Job cancellation
├── AgentShutdown - Graceful shutdown
├── OperatingSystemShutdown - OS shutdown
└── JobMetadataUpdate - Runtime metadata
```

### Phase 4: Worker Process Execution

#### 4.1 Worker Initialization
```
Agent.Worker/Program.cs::Main()
├── MainAsync()
│   ├── HostContext creation (Worker type)
│   ├── Command validation ("spawnclient")
│   ├── Pipe handle extraction
│   └── Worker.RunAsync(pipeIn, pipeOut)
```

#### 4.2 Job Message Reception
```
Worker.cs::RunAsync()
├── ProcessChannel.StartClient()
├── Channel.ReceiveAsync() - Wait for job message
├── Job message deserialization
├── VsoCommand deactivation (security)
├── Secret masker initialization
├── Culture/locale setup
└── JobRunner.RunAsync() initiation
```

### Phase 5: Job Execution Context Setup

#### 5.1 Job Context Initialization
```
JobRunner.cs::RunAsync()
├── Job validation and parameter setup
├── Server connection establishment:
│   ├── JobServer connection
│   ├── TaskServer connection (task definitions)
│   └── Certificate validation handling
├── ExecutionContext creation and initialization
├── Variable expansion and environment setup
├── Work directory creation and validation
├── Agent metadata population
└── Resource monitoring initialization
```

#### 5.2 Job Extension Initialization
```
JobRunner.cs::RunAsync() - Job Steps Setup
├── Job extension loading (Build/Release/etc.)
├── JobExtension.InitializeJob()
│   ├── Step definition parsing
│   ├── Task reference resolution
│   ├── Dependency analysis
│   └── IStep implementation creation
├── Task definition downloads
├── Container setup (if applicable)
└── Step list finalization
```

### Phase 6: Step Execution Pipeline

#### 6.1 Steps Runner Orchestration
```
StepsRunner.cs::RunAsync()
├── Async command completion wait
├── Step iteration loop:
│   ├── Step validation
│   ├── ExecutionContext.Start()
│   ├── Step target configuration
│   ├── Variable expansion
│   ├── Condition evaluation
│   └── RunStepAsync() execution
```

#### 6.2 Individual Step Execution
```
StepsRunner.cs::RunStepAsync()
├── Step timeout configuration
├── UTF-8 codepage switching (Windows)
├── Step.RunAsync() - Main execution
├── Exception handling:
│   ├── OperationCanceledException (timeout/cancellation)
│   ├── General exceptions (step failure)
│   └── Result determination
├── Async command completion wait
└── Step completion logging
```

### Phase 7: Task Execution (Core Implementation)

#### 7.1 Task Runner Entry Point
```
TaskRunner.cs::RunAsync()
├── Task validation
├── User agent logging (if enabled)
└── RunAsyncInternal() - Core execution
```

#### 7.2 Task Execution Pipeline
```
TaskRunner.cs::RunAsyncInternal()
├── Task definition loading
├── Handler selection (Node/PowerShell/Plugin)
├── Container target validation
├── Input processing pipeline:
│   ├── LoadDefaultInputs() - Definition defaults
│   ├── Instance input merging
│   ├── Variable expansion
│   ├── Environment variable expansion
│   └── File path translation
├── Environment setup
├── Endpoint and secure file processing
├── Handler creation and configuration
├── Resource monitoring startup
└── Handler.RunAsync() execution
```

#### 7.3 Task Input Processing Details
```
Task Input Pipeline:
├── LoadDefaultInputs()
│   ├── Task definition input defaults
│   └── Input trimming (if enabled)
├── Instance Input Merging
│   ├── Task.Inputs dictionary processing
│   ├── Key validation and trimming
│   └── Value assignment
├── Variable Expansion
│   ├── RuntimeVariables.ExpandValues()
│   ├── Pipeline variable substitution
│   └── Secret masking integration
├── Environment Variable Expansion
│   ├── VarUtil.ExpandEnvironmentVariables()
│   └── System environment resolution
└── File Path Translation
    ├── FilePath input type detection
    └── StepHost.ResolvePathForStepHost()
```

### Phase 8: Task Handler Execution

#### 8.1 Handler Types and Execution
```
Handler Execution Flow:
├── NodeHandler (Node.js tasks)
│   ├── Node.js runtime setup
│   ├── Task script execution
│   └── Output/error capture
├── PowerShellHandler (PowerShell tasks)
│   ├── PowerShell runtime setup
│   ├── Script execution
│   └── Output/error capture
├── PluginHandler (Agent plugins)
│   ├── Plugin host process creation
│   ├── Plugin assembly loading
│   └── Plugin.RunAsync() execution
└── ProcessHandler (External executables)
    ├── Process configuration
    ├── Executable invocation
    └── Exit code handling
```

### Phase 9: Job Completion and Cleanup

#### 9.1 Step Completion Processing
```
StepsRunner.cs - Step Completion:
├── Result aggregation
├── Continue-on-error evaluation
├── Job status updates
├── Resource cleanup
└── Next step decision
```

#### 9.2 Job Finalization
```
JobRunner.cs - Job Completion:
├── Job extension finalization
├── Support log upload (if diagnostic mode)
├── Timeline record updates
├── Job result calculation
├── Server communication completion
└── Resource disposal
```

#### 9.3 Worker Process Termination
```
Worker.cs - Process Cleanup:
├── JobRunner completion wait
├── Final result code determination
├── Channel cleanup
├── Process exit with result code
```

### Phase 10: Listener Cleanup and Next Job

#### 10.1 Job Dispatcher Cleanup
```
JobDispatcher.cs - Job Completion:
├── Worker process exit wait
├── Process result code validation
├── Job request completion
├── Lock renewal cancellation
├── Timeline record updates
├── Unhandled exception logging
└── Next job readiness
```

#### 10.2 Agent Listener Continuation
```
Agent.cs - Listener Loop:
├── Message deletion from queue
├── Job completion notification
├── Performance counter updates
├── Auto-update handling
└── Next message wait (back to Phase 2)
```

## Process Relationships and Communication

### 1:1 vs 1:N Relationship Analysis

#### Listener to Dispatcher (1:1)
- **Agent** process contains one **JobDispatcher** instance
- Single dispatcher handles all job scheduling
- Sequential job processing (no parallel jobs per agent)

#### Dispatcher to Worker (1:1 per Job)
- **JobDispatcher** creates one **Worker** process per job
- Each job gets isolated worker process
- Process isolation ensures job independence

#### Worker to JobRunner (1:1)
- **Worker** process contains one **JobRunner** instance
- Single job runner per worker lifetime
- Direct job message processing

#### JobRunner to StepsRunner (1:1)
- **JobRunner** uses one **StepsRunner** instance
- Single steps runner orchestrates all job steps
- Sequential step execution within job

#### StepsRunner to TaskRunner (1:N)
- **StepsRunner** creates multiple **TaskRunner** instances
- One task runner per task step
- Sequential task execution per job

## Key Configuration Points

### Agent Configuration
- **Location**: `src/Agent.Listener/AgentKnobs.cs`
- **Environment Variables**: `AGENT_*` prefixed variables
- **Key Settings**:
  - `AGENT_CHANNEL_TIMEOUT`: Worker communication timeout
  - `AGENT_LOG_TASK_INPUT_PARAMETERS`: Task input logging (future)
  - `AGENT_ENABLE_RESOURCE_UTILIZATION_WARNINGS`: Resource monitoring

### Performance Monitoring
- **Performance Counters**: Throughout lifecycle for timing analysis
- **Resource Monitoring**: CPU, memory, disk usage tracking
- **Telemetry**: Feature usage and error reporting

### Security Considerations
- **Secret Masking**: Automatic detection and masking of sensitive data
- **Certificate Validation**: Server certificate verification
- **Process Isolation**: Worker processes run isolated from listener
- **Secure Communication**: Encrypted channels between processes

## Error Handling and Recovery

### Graceful Degradation
- **Task Failure**: Continue-on-error logic allows job continuation
- **Worker Crash**: Listener detects and reports worker failures
- **Communication Loss**: Timeout handling with process termination
- **Agent Shutdown**: Graceful shutdown with job completion wait

### Retry Mechanisms
- **Connection Retry**: Automatic server reconnection
- **Task Retry**: Built-in task retry logic with exponential backoff
- **Lock Renewal**: Continuous job lock maintenance
- **Queue Processing**: Robust message queue handling

This documentation provides the complete execution flow from agent startup through task completion, showing the detailed interaction between all components in the Azure DevOps Agent architecture.
