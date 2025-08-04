# Troubleshooting Guide

This guide provides systematic approaches to diagnosing and resolving common issues with the Azure DevOps Agent.

## 🚨 Quick Triage

### Agent Won't Start
1. **Check Configuration**: `./config.cmd --check`
2. **Verify Permissions**: Agent service account permissions
3. **Check Logs**: `_diag/Agent_*.log` files
4. **Network Connectivity**: Test connection to Azure DevOps

### Jobs Failing to Start
1. **Capability Matching**: Verify agent has required capabilities  
2. **Resource Availability**: Check disk space and memory
3. **Pool Configuration**: Confirm agent is in correct pool
4. **Authentication**: Verify PAT token validity

### Tasks Failing
1. **Input Validation**: Check task input parameters
2. **Tool Availability**: Verify required tools are installed
3. **Permissions**: Check file system and network permissions
4. **Environment**: Validate environment variables and paths

## 🔍 Diagnostic Information Collection

### Essential Log Files

| Log Type | Location | Purpose |
|----------|----------|---------|
| **Agent Diagnostic** | `_diag/Agent_*.log` | Complete agent trace information |
| **Worker Diagnostic** | `_diag/Worker_*.log` | Job execution details |
| **Timeline Records** | Via Azure DevOps UI | Real-time job progress |
| **System Event Log** | Windows Event Viewer / syslog | OS-level events |

### Collecting Diagnostics

```powershell
# Enable verbose logging
$env:VSTS_AGENT_HTTPTRACE = "true"
$env:AGENT_DIAGNOSTIC = "true"

# Run agent with detailed logging
.\run.cmd --once

# Collect logs
Get-ChildItem _diag\*.log | Sort-Object LastWriteTime -Descending | Select-Object -First 5
```

### Log Analysis Commands

```powershell
# Find errors in recent logs
Get-Content _diag\Agent_*.log | Select-String "ERROR|FATAL" | Select-Object -Last 20

# Search for specific issues
Get-Content _diag\Worker_*.log | Select-String "TaskRunner|JobRunner" | Select-Object -Last 50

# Check performance counters
Get-Content _diag\*.log | Select-String "WritePerfCounter" | Select-Object -Last 30
```

## 🔧 Common Issues and Solutions

### Agent Registration Issues

#### Problem: "Agent already exists"
```
ERROR: An agent with the same name already exists
```

**Solution**:
```powershell
# Remove existing agent
.\config.cmd remove --auth pat --token <token>

# Re-register with new name or replace existing
.\config.cmd --url <url> --auth pat --token <token> --pool <pool> --agent <name> --replace
```

#### Problem: Authentication failures
```
ERROR: Failed to authenticate using the supplied credentials
```

**Solution**:
1. **Verify PAT scope**: Ensure token has Agent Pools (read, manage) permissions
2. **Check token expiration**: Generate new PAT if expired
3. **Validate URL**: Ensure correct Azure DevOps organization URL

### Job Execution Issues

#### Problem: Worker process crashes
```
ERROR: Worker process exited unexpectedly with exit code -1
```

**Diagnostic Steps**:
```powershell
# Check worker logs
Get-Content _diag\Worker_*.log | Select-String "Exception|Error" -Context 5

# Check system resources
Get-Process | Where-Object {$_.ProcessName -like "*Agent*"}
Get-WmiObject -Class Win32_PhysicalMemory | Measure-Object -Property Capacity -Sum
```

**Common Causes**:
- **Out of Memory**: Increase available memory or optimize tasks
- **Disk Space**: Clean up working directory or increase disk space
- **Permissions**: Verify agent service account permissions
- **Antivirus**: Add agent directories to exclusion list

#### Problem: Tasks timing out
```
ERROR: The task has timed out. Canceling the task execution.
```

**Solution**:
```yaml
# In pipeline YAML, increase timeout
steps:
- task: YourTask@1
  timeoutInMinutes: 60  # Increase from default
  inputs:
    # task inputs
```

### Communication Issues

#### Problem: IPC communication failures
```
ERROR: Failed to receive message from worker process
```

**Diagnostic Steps**:
```powershell
# Check named pipes (Windows)
Get-ChildItem \\.\pipe\ | Where-Object {$_.Name -like "*vsts*"}

# Check process communication
Get-Process | Where-Object {$_.ProcessName -like "*Agent*"} | Select-Object Id, ProcessName, StartTime
```

**Solutions**:
- **Restart Agent**: Stop and start agent service
- **Check Permissions**: Verify pipe permissions
- **Resource Limits**: Check for resource exhaustion

### Performance Issues

#### Problem: Slow job execution
```
INFO: Job execution taking longer than expected
```

**Analysis Commands**:
```powershell
# Analyze performance counters
Get-Content _diag\*.log | Select-String "WritePerfCounter" | 
    ForEach-Object { 
        if ($_ -match "WritePerfCounter\(`"([^`"]+)`"") { 
            $matches[1] 
        } 
    } | Group-Object | Sort-Object Count -Descending

# Check resource usage patterns
Get-Content _diag\*.log | Select-String "Memory|CPU|Disk" -Context 2
```

**Optimization Strategies**:
1. **Parallel Execution**: Use job/step parallelism where possible
2. **Resource Allocation**: Increase agent machine resources
3. **Tool Caching**: Enable tool installer caching
4. **Artifact Optimization**: Reduce artifact sizes

## 🛠️ Advanced Troubleshooting

### Network Connectivity Issues

#### Proxy Configuration
```powershell
# Check current proxy settings
.\config.cmd --check

# Configure proxy
.\config.cmd --proxyurl http://proxy:8080 --proxyusername domain\user --proxypassword password
```

#### SSL/Certificate Issues
```powershell
# Skip certificate validation (testing only)
$env:AGENT_SKIP_CERT_VALIDATION = "true"

# Check certificate store
Get-ChildItem Cert:\LocalMachine\Root | Where-Object {$_.Subject -like "*Azure*"}
```

### Secret Masking Issues

#### Problem: Secrets appearing in logs
```
WARNING: Potential secret detected in output
```

**Diagnostic Steps**:
```csharp
// Check secret masker configuration
Trace.Info($"Secret masker patterns: {HostContext.SecretMasker.GetPatternCount()}");

// Verify secret registration
if (AgentKnobs.LogSecretMasking.GetValue(HostContext).AsBoolean())
{
    Trace.Info($"Secret registered: {secretType}");
}
```

**Solutions**:
1. **Manual Registration**: Add secrets to masker manually
2. **Pattern Updates**: Update regex patterns for better detection
3. **Scope Verification**: Ensure secrets are registered before use

### Performance Analysis

#### Memory Usage Investigation
```powershell
# Monitor memory usage during job execution
while ($true) {
    $processes = Get-Process | Where-Object {$_.ProcessName -like "*Agent*"}
    $processes | Select-Object ProcessName, Id, WorkingSet, VirtualMemorySize | Format-Table
    Start-Sleep 30
}
```

#### CPU Usage Analysis
```powershell
# Get CPU usage for agent processes
Get-Counter "\Process(Agent.Listener)\% Processor Time" -SampleInterval 5 -MaxSamples 10
Get-Counter "\Process(Agent.Worker)\% Processor Time" -SampleInterval 5 -MaxSamples 10
```

## 🔬 Debug Mode Operations

### Running Agent in Debug Mode

```powershell
# Stop agent service
Stop-Service vstsagent.*

# Run interactively with debug output
.\run.cmd --once

# Enable additional debugging
$env:AGENT_DIAGNOSTIC = "true"
$env:VSTS_AGENT_HTTPTRACE = "true"
.\run.cmd --once
```

### Attaching Debugger

```csharp
// Add to code for debugging breakpoints
#if DEBUG
if (AgentKnobs.DebugMode.GetValue(HostContext).AsBoolean())
{
    System.Diagnostics.Debugger.Launch();
}
#endif
```

### Step-by-Step Debugging

1. **Set Breakpoints**: In Visual Studio, set breakpoints in relevant components
2. **Attach to Process**: Attach to Agent.Listener or Agent.Worker process
3. **Queue Job**: Trigger job execution
4. **Step Through**: Debug step-by-step execution

## 📊 Monitoring and Alerting

### Key Metrics to Monitor

| Metric | Threshold | Action |
|---------|-----------|---------|
| **Job Success Rate** | < 95% | Investigate failing jobs |
| **Average Job Duration** | > 2x baseline | Performance analysis |
| **Worker Process Crashes** | > 1 per day | System health check |
| **Disk Space** | < 10% free | Cleanup or expansion |
| **Memory Usage** | > 80% sustained | Resource optimization |

### Health Check Script

```powershell
function Test-AgentHealth {
    $health = @{
        AgentRunning = (Get-Service vstsagent.* -ErrorAction SilentlyContinue).Status -eq 'Running'
        DiskSpace = (Get-WmiObject -Class Win32_LogicalDisk -Filter "DeviceID='C:'").FreeSpace / 1GB
        RecentErrors = (Get-Content _diag\Agent_*.log | Select-String "ERROR" | Measure-Object).Count
        LastJobTime = (Get-ChildItem _diag\Worker_*.log | Sort-Object LastWriteTime -Descending | Select-Object -First 1).LastWriteTime
    }
    
    return $health
}

Test-AgentHealth
```

## 🔄 Recovery Procedures

### Automatic Recovery

#### Service Restart Script
```powershell
# Automatic agent service restart on failure
$serviceName = (Get-Service vstsagent.*).Name
$service = Get-Service $serviceName

if ($service.Status -ne 'Running') {
    Write-Host "Agent service not running, attempting restart..."
    Restart-Service $serviceName
    Start-Sleep 30
    
    if ((Get-Service $serviceName).Status -eq 'Running') {
        Write-Host "Agent service restarted successfully"
    } else {
        Write-Error "Failed to restart agent service"
    }
}
```

### Manual Recovery

#### Complete Agent Reset
```powershell
# Stop agent
Stop-Service vstsagent.*

# Clean working directory
Remove-Item _work\* -Recurse -Force

# Clean diagnostic logs (optional)
Remove-Item _diag\*.log

# Restart agent
Start-Service vstsagent.*
```

#### Configuration Recovery
```powershell
# Backup current configuration
Copy-Item .agent -Destination .agent.backup

# Remove and reconfigure
.\config.cmd remove --auth pat --token <token>
.\config.cmd --url <url> --auth pat --token <token> --pool <pool> --agent <name>
```

## 📞 Escalation Procedures

### When to Escalate

1. **Agent Infrastructure Issues**: Multiple agents affected
2. **Azure DevOps Service Issues**: Widespread connectivity problems  
3. **Security Incidents**: Potential security breach or data exposure
4. **Critical Performance**: Service-level agreement violations

### Information to Collect

Before escalating, gather:

1. **Agent Configuration**: `.agent` file and environment details
2. **Recent Logs**: Last 24 hours of diagnostic logs  
3. **Timeline Records**: Azure DevOps job execution history
4. **System Information**: OS, hardware, network configuration
5. **Reproduction Steps**: Exact steps to reproduce the issue

### Support Channels

1. **Internal Teams**: Agent development team via Teams
2. **Azure Support**: For Azure DevOps service issues
3. **On-Call**: For critical production issues
4. **GitHub Issues**: For agent bugs and feature requests

## 🎯 Prevention Best Practices

### Proactive Monitoring
- **Regular Health Checks**: Automated monitoring scripts
- **Log Analysis**: Trending analysis of error patterns
- **Performance Baselines**: Establish and monitor performance metrics
- **Capacity Planning**: Monitor resource usage trends

### Maintenance Procedures
- **Regular Updates**: Keep agent versions current
- **Log Rotation**: Prevent disk space issues with log cleanup
- **Configuration Backups**: Regular backup of agent configuration
- **Security Reviews**: Periodic security configuration audits

---

**Remember**: When troubleshooting, start with the most recent logs and work backwards. Most issues are related to configuration, permissions, or resource availability.
