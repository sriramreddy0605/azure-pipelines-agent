# Agent Overview

## What is the Azure DevOps Agent?

The Azure DevOps Agent is a sophisticated execution engine that runs Azure DevOps pipelines on-premises or in private cloud environments. It provides a secure, isolated environment for executing CI/CD workflows while maintaining connectivity to Azure DevOps Services.

## 🏗️ High-Level Architecture

### Two-Process Design

The agent uses a **dual-process architecture** for reliability and security:

```
┌─────────────────────────────────────────────────────────────┐
│                        Host Machine                         │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐                ┌─────────────────┐     │
│  │ Agent.Listener  │   IPC Pipes    │ Agent.Worker    │     │
│  │   (Persistent)  │ ◄──────────── │   (Per Job)     │     │
│  │                 │                │                 │     │
│  │ • Job Polling   │                │ • Job Execution │     │
│  │ • Communication │                │ • Task Running  │     │
│  │ • Lifecycle Mgmt│                │ • Result Report │     │
│  └─────────────────┘                └─────────────────┘     │
└─────────────────────────────────────────────────────────────┘
```

#### Agent.Listener (Persistent Process)
- **Purpose**: Maintains connection to Azure DevOps, polls for jobs
- **Lifecycle**: Runs continuously until explicitly stopped
- **Responsibilities**:
  - Authentication with Azure DevOps Services
  - Job message polling and queueing
  - Worker process creation and management
  - Agent capability advertisement

#### Agent.Worker (Per-Job Process)
- **Purpose**: Executes individual jobs in isolation
- **Lifecycle**: Created per job, terminated after completion
- **Responsibilities**:
  - Job context setup and configuration
  - Step execution and task orchestration
  - Result collection and reporting
  - Resource cleanup

## 🔄 Process Relationships

| Component | Relationship | Description |
|-----------|--------------|-------------|
| **Agent ↔ JobDispatcher** | 1:1 | Single dispatcher manages all incoming jobs |
| **JobDispatcher ↔ Worker** | 1:1 | Each job gets its own isolated worker process |
| **Worker ↔ JobRunner** | 1:1 | Single job runner orchestrates job execution |
| **JobRunner ↔ StepsRunner** | 1:1 | Single step orchestrator per job |
| **StepsRunner ↔ TaskRunner** | 1:N | Multiple tasks can run within a job |

## 🛡️ Security Model

### Process Isolation
- **Separate Memory Space**: Each job runs in isolated process
- **Resource Boundaries**: CPU, memory, and file system isolation
- **Clean Environment**: Fresh process state for each job

### Secret Management
- **Multi-Layer Masking**: Secrets masked at multiple levels
- **Dynamic Detection**: Runtime secret detection and masking
- **Secure Communication**: Encrypted channels for sensitive data

### Authentication & Authorization
- **Personal Access Tokens**: Secure authentication to Azure DevOps
- **Certificate Validation**: Configurable SSL/TLS validation
- **Capability-Based Access**: Jobs only run on compatible agents

## 📊 Key Capabilities

### Platform Support
- **Windows**: Full support with native Windows features
- **Linux**: Complete Linux distribution support
- **macOS**: Apple ecosystem integration
- **Containers**: Docker and container-based execution

### Execution Features
- **Multiple Languages**: .NET, Java, Python, Node.js, and more
- **Tool Integration**: Git, Docker, Kubernetes, cloud CLIs
- **Custom Tasks**: Extensible task framework
- **Parallel Execution**: Multi-task and multi-job support

### Monitoring & Diagnostics
- **Performance Counters**: Built-in timing and resource monitoring
- **Comprehensive Logging**: Detailed execution tracing
- **Health Monitoring**: Agent and job health reporting
- **Diagnostic Collection**: Automated log collection and upload

## 🔧 Configuration & Management

### Agent Registration
```bash
# Interactive configuration
./config.cmd

# Unattended configuration
./config.cmd --unattended --url <url> --auth pat --token <token>
```

### Service Management
```bash
# Windows Service
./config.cmd --configure-as-service

# Linux Systemd
sudo ./svc.sh install
sudo ./svc.sh start
```

### Capability Management
- **System Capabilities**: Automatically detected (OS, tools, etc.)
- **User Capabilities**: Manually configured custom capabilities
- **Dynamic Discovery**: Runtime capability detection

## 🎯 Common Use Cases

### CI/CD Pipelines
- **Build Automation**: Compile, test, and package applications
- **Deployment Orchestration**: Multi-environment deployment workflows
- **Quality Gates**: Automated testing and approval processes

### Hybrid Scenarios
- **On-Premises Resources**: Access to internal systems and databases
- **Private Networks**: Execution within secure network boundaries
- **Compliance Requirements**: Meet regulatory and security requirements

### Custom Automation
- **Infrastructure Management**: Terraform, ARM templates, scripting
- **Data Processing**: ETL jobs, data migration, reporting
- **Integration Workflows**: API integration, file processing

## 📈 Performance Characteristics

### Throughput
- **Job Concurrency**: Single job per agent (by design)
- **Task Parallelism**: Multiple tasks within jobs
- **Resource Efficiency**: Optimized for resource utilization

### Scalability
- **Horizontal Scaling**: Multiple agents per pool
- **Pool Management**: Dynamic agent allocation
- **Load Distribution**: Automatic job distribution

### Reliability
- **Fault Tolerance**: Graceful failure handling
- **Retry Mechanisms**: Automatic retry for transient failures
- **Health Monitoring**: Continuous agent health checks

## 🔍 Monitoring & Observability

### Built-in Telemetry
- **Performance Metrics**: Execution time, resource usage
- **Error Tracking**: Exception logging and reporting
- **Audit Trails**: Complete execution history

### Integration Points
- **Azure Monitor**: Native Azure monitoring integration
- **Custom Logging**: Extensible logging framework
- **Third-Party Tools**: Integration with monitoring solutions

## 🚀 Next Steps

To dive deeper into specific aspects:

1. **[Agent Lifecycle Flow](./Agent-Lifecycle-Flow.md)** - Understand the complete execution process
2. **[Process Architecture](./Process-Architecture.md)** - Learn about inter-process communication
3. **[Component Reference](./Component-Reference.md)** - Detailed component documentation
4. **[Security Implementation](./Security-Implementation.md)** - Security features and best practices

---

**Key Takeaways:**
- Dual-process architecture ensures reliability and security
- 1:1 job-to-worker relationship provides complete isolation
- Comprehensive security model protects sensitive data
- Extensible design supports diverse automation scenarios
