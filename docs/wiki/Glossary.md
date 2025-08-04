# Glossary

This glossary provides definitions for key terms, concepts, and components used throughout the Azure DevOps Agent system.

## 🔤 A-D

### **Agent**
The complete Azure DevOps Agent system consisting of both Listener and Worker processes. Executes Azure DevOps pipelines on behalf of Azure DevOps Services.

### **Agent.Listener**
The persistent process that maintains connection to Azure DevOps Services, polls for jobs, and manages worker processes. Runs continuously until explicitly stopped.

### **Agent.Worker**  
The per-job process that executes individual pipeline jobs. Created fresh for each job and terminated after completion to ensure isolation.

### **AgentKnobs**
Centralized configuration system using feature flags and environment variables. Located in `AgentKnobs.cs`, provides runtime configuration control.

### **Capability**
A feature or tool that an agent advertises to Azure DevOps Services. Used for job routing to compatible agents. Examples: `Node.js`, `Docker`, `Python`.

### **Cancellation Token**
.NET mechanism for cooperative cancellation. Used throughout the agent to enable graceful shutdown and job cancellation.

### **Channel** (IPC)
Communication pathway between Agent.Listener and Agent.Worker processes using named pipes (Windows) or Unix domain sockets (Linux/macOS).

### **Context** (Execution)
Hierarchical logging and state management system. Provides scoped logging, variable management, and result tracking for jobs, steps, and tasks.

### **Dispatcher** (Job)
Component in Agent.Listener responsible for routing job messages to worker processes. Creates and manages worker process lifecycle.

## 🔤 E-J

### **Execution Context**
See **Context (Execution)**

### **Handler** (Task)
Execution engine for specific task types. Examples: `NodeHandler` for Node.js tasks, `PowerShellHandler` for PowerShell tasks.

### **HostContext**
Service locator and dependency injection container. Provides access to agent services throughout the system.

### **IPC** (Inter-Process Communication)
Communication mechanism between Listener and Worker processes using named pipes or Unix domain sockets with JSON message serialization.

### **Job**
A unit of work in Azure DevOps pipelines. Contains one or more steps to be executed. Maps 1:1 to a worker process.

### **JobRunner**
Core orchestration component in Agent.Worker. Manages complete job execution including setup, step processing, and cleanup.

## 🔤 K-P

### **Knob**
See **AgentKnobs**

### **Listener**
See **Agent.Listener**

### **Masker** (Secret)
Security component that detects and masks sensitive information in logs and outputs. Operates at multiple layers throughout execution.

### **Message** (Worker)
IPC communication unit between Listener and Worker. Types include: `NewJobRequest`, `CancelRequest`, `JobCompleted`.

### **Pipeline**
Azure DevOps CI/CD workflow definition. Executed by the agent as a series of jobs and steps.

### **Process Channel**
See **Channel (IPC)**

## 🔤 Q-T

### **Runner** (Job/Steps/Task)
Family of orchestration components:
- **JobRunner**: Orchestrates complete job execution
- **StepsRunner**: Manages step sequencing within jobs  
- **TaskRunner**: Executes individual tasks with input processing

### **Secret Masking**
Security feature that automatically detects and replaces sensitive information with `***` in logs and outputs.

### **Service Locator**
Design pattern used for dependency injection. Implemented via `HostContext` to provide access to agent services.

### **Step**
Individual unit within a job. Can be a task, script, or action. Multiple steps can exist per job.

### **StepsRunner**
Component that orchestrates execution of steps within a job. Handles step sequencing, conditions, and error continuation.

### **Task**
Reusable execution unit in Azure DevOps. Examples: `PublishTestResults`, `DotNetCoreCLI`. Executed by TaskRunner.

### **TaskRunner**
Component responsible for executing individual tasks. Handles input processing, handler creation, and output capture.

### **Timeline**
Real-time progress reporting system that updates Azure DevOps Services with job/step/task status and output.

## 🔤 U-Z

### **Variable**
Key-value pair used for configuration and data passing. Can be secret (masked) or regular. Supports expansion syntax `$(variableName)`.

### **Variable Expansion**
Process of replacing `$(variableName)` syntax with actual variable values. Performed during input processing.

### **Worker**
See **Agent.Worker**

### **Working Directory**
Isolated file system location where job execution occurs. Unique per job, cleaned up after completion.

## 🏗️ Architecture Terms

### **1:1 Relationship**
One-to-one mapping between components. Example: Each job gets exactly one worker process for complete isolation.

### **1:N Relationship**  
One-to-many mapping. Example: One StepsRunner can execute multiple TaskRunners within a job.

### **Dual-Process Architecture**
Design pattern using separate Listener (persistent) and Worker (per-job) processes for reliability and security.

### **Process Isolation**
Security and reliability feature where each job runs in completely separate process with isolated memory, file system, and resources.

## 🔐 Security Terms

### **Certificate Validation**
SSL/TLS certificate verification for secure communication with Azure DevOps Services. Configurable via agent settings.

### **PAT** (Personal Access Token)
Authentication mechanism for agent-to-Azure DevOps communication. Stored securely and used for all API calls.

### **Process Boundaries**
Security isolation between different processes. Prevents jobs from interfering with each other or the agent itself.

### **Secret**
Sensitive information (passwords, tokens, keys) that must be masked in logs and outputs. Handled by SecretMasker component.

## 📊 Performance Terms

### **Performance Counter**
Timing measurement points throughout agent execution. Used for monitoring and optimization. Examples: `WorkerProcessCreated`, `JobCompleted`.

### **Baseline Memory**
Minimum memory usage for agent components:
- Agent.Listener: ~50-100MB
- Agent.Worker: ~100-200MB

### **Process Overhead**
Additional resource cost of multi-process architecture:
- Process creation: 50-200ms
- IPC setup: 10-50ms
- Memory overhead: ~100MB per worker

## 🐛 Troubleshooting Terms

### **Diagnostic Log**
Detailed trace information collected during agent execution. Automatically uploaded to Azure DevOps for support scenarios.

### **Exit Code**
Numeric value returned by processes to indicate success (0) or failure (non-zero). Used for error detection and recovery.

### **Health Check**
Monitoring mechanism to verify agent and worker process status. Includes responsiveness, memory usage, and communication verification.

### **Retry Logic**
Automatic recovery mechanism for transient failures. Uses exponential backoff to avoid overwhelming failing services.

## 🔧 Configuration Terms

### **Agent Pool**
Group of agents that can execute jobs. Jobs are routed to available agents within the specified pool.

### **Capability Matching**
Process of routing jobs to agents that have required capabilities. Prevents jobs from running on incompatible agents.

### **Environment Variable**
OS-level configuration that affects agent behavior. Many agent settings can be controlled via environment variables.

### **Feature Flag**
Runtime configuration switch that enables/disables functionality. Implemented via AgentKnobs system.

## 📝 Development Terms

### **Service Registration**
Dependency injection pattern where services are registered in HostContext and resolved as needed throughout the system.

### **Trace Logging**
Development-time logging for debugging and diagnostics. Different from pipeline output logging.

### **Unit of Work**
Design pattern where each major operation (job, step, task) is treated as discrete unit with clear boundaries.

### **Cancellation Cooperative**
Pattern where long-running operations regularly check cancellation tokens and gracefully exit when requested.

## 🔄 Integration Terms

### **Artifact**
File or package produced by pipeline execution. Uploaded to Azure DevOps storage after job completion.

### **Endpoint**
External service connection (Git, Docker, cloud services) configured in Azure DevOps and used by tasks.

### **Repository**
Source code location that agent checks out and uses for pipeline execution. Can be Git, TFVC, or other SCM systems.

### **Service Connection**
Authenticated connection to external services. Credentials are securely provided to tasks that need them.

## 📚 File System Terms

### **Layout Directory**
Agent installation directory structure after build. Contains all necessary binaries and dependencies.

### **Temp Directory**
Temporary file location used during job execution. Cleaned up after job completion.

### **Work Directory**
Root directory for all agent job execution. Contains subdirectories for each job's working space.

---

## 🎯 Quick Reference

### Most Important Terms for New Team Members
1. **Agent.Listener** / **Agent.Worker** - The two main processes
2. **JobRunner** / **StepsRunner** / **TaskRunner** - Execution orchestration
3. **ExecutionContext** - Logging and state management
4. **SecretMasker** - Security component for sensitive data
5. **AgentKnobs** - Configuration and feature flags

### Most Important for Debugging
1. **Process Isolation** - Understanding separate processes
2. **IPC Channel** - Communication between processes
3. **Cancellation Token** - Graceful shutdown mechanisms
4. **Timeline** - Progress reporting system
5. **Diagnostic Log** - Troubleshooting information

### Most Important for Security
1. **Secret Masking** - Protecting sensitive information
2. **Process Boundaries** - Isolation mechanisms
3. **Certificate Validation** - Secure communication
4. **Variable Expansion** - Data handling security
5. **PAT** - Authentication mechanism

---

**Usage Tip**: Use Ctrl+F to quickly find specific terms in this glossary. Each term links to relevant documentation pages for deeper understanding.
