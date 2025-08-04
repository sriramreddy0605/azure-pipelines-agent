# Security Implementation

The Azure DevOps Agent implements comprehensive security measures to protect sensitive data, ensure secure communication, and maintain process isolation. This page details the security architecture and implementation.

## 🛡️ Security Architecture Overview

### Multi-Layer Security Model

```
┌─────────────────────────────────────────────────────────────┐
│                    Security Layers                         │
├─────────────────────────────────────────────────────────────┤
│  Layer 1: Process Isolation                                │
│  ├── Separate memory spaces                                │
│  ├── File system isolation                                 │
│  └── Resource boundaries                                   │
├─────────────────────────────────────────────────────────────┤
│  Layer 2: Communication Security                           │
│  ├── Encrypted channels (HTTPS/WSS)                       │
│  ├── Certificate validation                                │
│  └── Authenticated IPC                                     │
├─────────────────────────────────────────────────────────────┤
│  Layer 3: Secret Management                                │
│  ├── Multi-pattern secret masking                         │
│  ├── Dynamic secret detection                              │
│  └── Cross-process protection                              │
├─────────────────────────────────────────────────────────────┤
│  Layer 4: Access Control                                   │
│  ├── Capability-based routing                              │
│  ├── Agent authentication                                  │
│  └── Permission validation                                 │
└─────────────────────────────────────────────────────────────┘
```

## 🔐 Secret Management System

### SecretMasker Architecture

The `SecretMasker` is the core component responsible for detecting and masking sensitive information throughout the agent execution pipeline.

**Location**: `src/Microsoft.VisualStudio.Services.Agent/SecretMasker.cs`

#### Multi-Pattern Detection

```csharp
public class SecretMasker : ISecretMasker
{
    private readonly List<string> _secrets = new List<string>();
    private readonly List<Regex> _regexSecrets = new List<Regex>();
    private readonly ReaderWriterLockSlim _secretsLock = new ReaderWriterLockSlim();
    
    public void AddValue(string value, string source)
    {
        if (string.IsNullOrEmpty(value) || value.Length < MinSecretLength)
            return;
            
        _secretsLock.EnterWriteLock();
        try
        {
            _secrets.Add(value);
            Trace.Info($"Secret added from source: {source}");
        }
        finally
        {
            _secretsLock.ExitWriteLock();
        }
    }
    
    public void AddRegex(string pattern, string source)
    {
        var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        _regexSecrets.Add(regex);
        Trace.Info($"Secret regex pattern added from source: {source}");
    }
}
```

#### Secret Registration Points

Secrets are registered at multiple points throughout execution:

```csharp
// 1. Variable-level secrets (Worker.cs)
foreach (var variable in jobMessage.Variables ?? new Dictionary<string, VariableValue>())
{
    if (variable.Value.IsSecret && !string.IsNullOrWhiteSpace(variable.Value.Value))
    {
        AddUserSuppliedSecret(variable.Value.Value);
        
        // Handle escaped versions for shell usage
        var escapedSecret = variable.Value.Value.Replace("%", "%AZP25")
                                              .Replace("\r", "%0D")
                                              .Replace("\n", "%0A");
        AddUserSuppliedSecret(escapedSecret);
        
        // Base64 encoded versions
        var base64Secret = Convert.ToBase64String(Encoding.UTF8.GetBytes(variable.Value.Value));
        AddUserSuppliedSecret(base64Secret);
    }
}

// 2. Endpoint authentication parameters
foreach (var endpoint in message.Resources.Endpoints ?? new List<ServiceEndpoint>())
{
    foreach (var auth in endpoint.Authorization?.Parameters ?? new Dictionary<string, string>())
    {
        if (!string.IsNullOrEmpty(auth.Value) && MaskingUtil.IsEndpointAuthorizationParametersSecret(auth.Key))
        {
            HostContext.SecretMasker.AddValue(auth.Value, $"Endpoint_{auth.Key}");
        }
    }
}

// 3. Secure file tickets
foreach (var file in message.Resources.SecureFiles ?? new List<SecureFile>())
{
    if (!string.IsNullOrEmpty(file.Ticket))
    {
        HostContext.SecretMasker.AddValue(file.Ticket, WellKnownSecretAliases.SecureFileTicket);
    }
}
```

#### Advanced Secret Handling

```csharp
private void AddUserSuppliedSecret(string secret)
{
    ArgUtil.NotNull(secret, nameof(secret));
    HostContext.SecretMasker.AddValue(secret, WellKnownSecretAliases.UserSuppliedSecret);
    
    // Handle quote-wrapped secrets (addresses shell stripping)
    foreach (var quoteChar in new char[] { '\'', '"' })
    {
        if (secret.StartsWith(quoteChar) && secret.EndsWith(quoteChar))
        {
            HostContext.SecretMasker.AddValue(secret.Trim(quoteChar), WellKnownSecretAliases.UserSuppliedSecret);
        }
    }
    
    // Handle whitespace variations
    var trimChars = new char[] { '\r', '\n', ' ' };
    HostContext.SecretMasker.AddValue(secret.Trim(trimChars), WellKnownSecretAliases.UserSuppliedSecret);
}
```

### Secret Masking in Practice

#### Output Masking
All output goes through the secret masker before being displayed or logged:

```csharp
public void WriteLine(string message)
{
    // Apply secret masking to all output
    string maskedMessage = HostContext.SecretMasker.MaskSecrets(message);
    
    // Write to console and logs
    Console.WriteLine(maskedMessage);
    Trace.Info(maskedMessage);
    
    // Send to timeline
    _timelineManager.AddOutput(maskedMessage);
}
```

#### Variable Expansion Security
Variable expansion includes automatic secret detection:

```csharp
public string ExpandValue(string value)
{
    if (string.IsNullOrEmpty(value))
        return value;
        
    // Expand variables
    string expandedValue = Regex.Replace(value, @"\$\(([^)]+)\)", match =>
    {
        var variableName = match.Groups[1].Value;
        var variableValue = GetVariableValue(variableName);
        
        // Check if expanded value should be treated as secret
        if (IsVariableSecret(variableName))
        {
            HostContext.SecretMasker.AddValue(variableValue, $"ExpandedVariable_{variableName}");
        }
        
        return variableValue ?? match.Value;
    });
    
    return expandedValue;
}
```

## 🔒 Process Isolation Security

### Memory Isolation

Each worker process operates in completely isolated memory space:

```csharp
// Worker process creation with security considerations
private Process CreateWorkerProcess(Guid jobId)
{
    var processInfo = new ProcessStartInfo
    {
        FileName = GetWorkerExecutablePath(),
        Arguments = $"--pipeIn {pipeIn} --pipeOut {pipeOut}",
        
        // Security settings
        CreateNoWindow = true,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        
        // Environment isolation
        Environment = CreateIsolatedEnvironment()
    };
    
    // Apply security restrictions
    ApplyProcessSecuritySettings(processInfo);
    
    return Process.Start(processInfo);
}
```

### File System Isolation

Each job gets isolated working directory with restricted access:

```csharp
// Working directory setup with security boundaries
private void SetupSecureWorkingDirectory(IExecutionContext context, Guid jobId)
{
    var workFolder = HostContext.GetDirectory(WellKnownDirectory.Work);
    var jobFolder = Path.Combine(workFolder, jobId.ToString());
    
    // Create isolated directory
    Directory.CreateDirectory(jobFolder);
    
    // Set restrictive permissions
    SetDirectoryPermissions(jobFolder, restrictive: true);
    
    // Configure temp directory
    var tempFolder = Path.Combine(jobFolder, "_temp");
    Directory.CreateDirectory(tempFolder);
    Environment.SetEnvironmentVariable("TMP", tempFolder);
    Environment.SetEnvironmentVariable("TEMP", tempFolder);
    
    context.Variables.Set(Constants.Variables.Agent.WorkFolder, jobFolder);
}
```

### Resource Boundaries

Process-level resource monitoring and limits:

```csharp
public class ProcessResourceMonitor
{
    public async Task MonitorWorkerResources(Process workerProcess, CancellationToken cancellationToken)
    {
        while (!workerProcess.HasExited && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Memory usage check
                if (workerProcess.WorkingSet64 > MaxMemoryThreshold)
                {
                    Trace.Warning($"Worker process {workerProcess.Id} exceeding memory threshold");
                    // Implement memory pressure handling
                }
                
                // CPU usage check
                var cpuUsage = GetProcessCpuUsage(workerProcess);
                if (cpuUsage > MaxCpuThreshold)
                {
                    Trace.Warning($"Worker process {workerProcess.Id} high CPU usage: {cpuUsage}%");
                }
                
                await Task.Delay(ResourceMonitoringInterval, cancellationToken);
            }
            catch (Exception ex)
            {
                Trace.Error($"Error monitoring worker resources: {ex}");
            }
        }
    }
}
```

## 🌐 Communication Security

### Azure DevOps Communication

#### HTTPS/WebSocket Security
All communication with Azure DevOps Services uses encrypted channels:

```csharp
public class MessageListener : AgentService, IMessageListener
{
    public async Task<VssConnection> CreateConnectionAsync(Uri serverUrl, VssCredentials credentials)
    {
        var connection = new VssConnection(serverUrl, credentials);
        
        // Configure security settings
        connection.Settings.MaxRetryRequest = 3;
        connection.Settings.SendTimeout = TimeSpan.FromMinutes(5);
        
        // Certificate validation
        if (!AgentKnobs.SkipCertificateValidation.GetValue(HostContext).AsBoolean())
        {
            ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
        }
        
        return connection;
    }
    
    private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
            return true;
            
        // Custom certificate validation logic
        Trace.Warning($"Certificate validation issues: {sslPolicyErrors}");
        return false; // Reject invalid certificates
    }
}
```

#### Authentication Security
Personal Access Token (PAT) management:

```csharp
public class AgentCredentialProvider
{
    public VssCredentials GetCredentials()
    {
        var credentialData = LoadSecureCredentials();
        
        // Create credentials with secure token handling
        var credentials = new VssBasicCredential(string.Empty, credentialData.Token);
        
        // Register token for masking
        HostContext.SecretMasker.AddValue(credentialData.Token, WellKnownSecretAliases.AuthToken);
        
        return credentials;
    }
    
    private CredentialData LoadSecureCredentials()
    {
        // Load from secure storage with encryption
        var encryptedData = File.ReadAllBytes(GetCredentialFilePath());
        var decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.LocalMachine);
        return JsonConvert.DeserializeObject<CredentialData>(Encoding.UTF8.GetString(decryptedData));
    }
}
```

### Inter-Process Communication (IPC) Security

#### Pipe Security (Windows)
```csharp
public class ProcessChannel : IProcessChannel
{
    private void CreateSecureNamedPipe(string pipeName)
    {
        var pipeSecurity = new PipeSecurity();
        
        // Allow current user full control
        var currentUser = WindowsIdentity.GetCurrent();
        pipeSecurity.AddAccessRule(new PipeAccessRule(currentUser.User, PipeAccessRights.FullControl, AccessControlType.Allow));
        
        // Deny access to other users
        pipeSecurity.AddAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.FullControl, AccessControlType.Deny));
        
        _pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 4096, 4096, pipeSecurity);
    }
}
```

#### Message Authentication
```csharp
public class SecureWorkerMessage : WorkerMessage
{
    public string MessageHash { get; set; }
    public DateTime Timestamp { get; set; }
    
    public bool ValidateAuthenticity(string sharedSecret)
    {
        var expectedHash = ComputeMessageHash(sharedSecret);
        return MessageHash == expectedHash && 
               DateTime.UtcNow - Timestamp < TimeSpan.FromMinutes(5); // Prevent replay attacks
    }
    
    private string ComputeMessageHash(string sharedSecret)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(sharedSecret)))
        {
            var data = Encoding.UTF8.GetBytes($"{MessageType}|{Body}|{Timestamp:O}");
            var hash = hmac.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }
    }
}
```

## 🔑 Access Control and Authorization

### Capability-Based Access Control

Jobs are routed only to agents with required capabilities:

```csharp
public class JobCapabilityMatcher
{
    public bool CanAgentExecuteJob(TaskAgentJobRequest jobRequest, TaskAgent agent)
    {
        foreach (var demand in jobRequest.Demands ?? new List<Demand>())
        {
            if (!agent.UserCapabilities.ContainsKey(demand.Name) && 
                !agent.SystemCapabilities.ContainsKey(demand.Name))
            {
                Trace.Info($"Agent {agent.Name} missing required capability: {demand.Name}");
                return false;
            }
            
            // Value-based capability matching
            if (!string.IsNullOrEmpty(demand.Value))
            {
                var agentValue = agent.UserCapabilities.GetValueOrDefault(demand.Name) ??
                               agent.SystemCapabilities.GetValueOrDefault(demand.Name);
                               
                if (!MatchCapabilityValue(agentValue, demand.Value))
                {
                    return false;
                }
            }
        }
        
        return true;
    }
}
```

### Agent Authentication

```csharp
public class AgentAuthentication
{
    public async Task<bool> AuthenticateAgentAsync(string agentName, string token)
    {
        try
        {
            var connection = await CreateAuthenticatedConnection(token);
            var agentApi = connection.GetClient<TaskAgentHttpClient>();
            
            // Verify agent exists and token has access
            var agent = await agentApi.GetAgentAsync(_poolId, agentName);
            
            // Additional security checks
            if (agent.Status != TaskAgentStatus.Online)
            {
                Trace.Warning($"Agent {agentName} not in online status");
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Trace.Error($"Agent authentication failed: {ex}");
            return false;
        }
    }
}
```

## 🔍 Security Monitoring and Auditing

### Security Event Logging

```csharp
public class SecurityEventLogger
{
    public void LogSecurityEvent(SecurityEventType eventType, string details, string source = null)
    {
        var securityEvent = new SecurityEvent
        {
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            Details = details,
            Source = source ?? GetCallingComponent(),
            AgentId = HostContext.GetService<IConfigurationStore>().GetSettings().AgentId,
            SessionId = HostContext.GetService<IHostContext>().SessionId
        };
        
        // Log to security audit trail
        Trace.Info($"SECURITY_EVENT: {JsonConvert.SerializeObject(securityEvent)}");
        
        // Send to centralized security monitoring (if configured)
        if (AgentKnobs.EnableSecurityEventReporting.GetValue(HostContext).AsBoolean())
        {
            await SendSecurityEventAsync(securityEvent);
        }
    }
}

public enum SecurityEventType
{
    AgentAuthentication,
    SecretMaskingFailure,
    UnauthorizedAccess,
    CertificateValidationFailure,
    ProcessIsolationViolation
}
```

### Secret Leakage Detection

```csharp
public class SecretLeakageDetector
{
    private readonly List<Regex> _sensitivePatterns = new List<Regex>
    {
        new Regex(@"(?i)(password|passwd|pwd|secret|token|key|credential)[\s]*[:=][\s]*['""]?([^'""\\s]+)", RegexOptions.Compiled),
        new Regex(@"(?i)Bearer\s+([A-Za-z0-9\-\._~\+\/]+=*)", RegexOptions.Compiled),
        new Regex(@"(?i)Basic\s+([A-Za-z0-9\+\/]+=*)", RegexOptions.Compiled)
    };
    
    public bool ScanForPotentialSecrets(string content, out List<string> potentialSecrets)
    {
        potentialSecrets = new List<string>();
        
        foreach (var pattern in _sensitivePatterns)
        {
            var matches = pattern.Matches(content);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var potentialSecret = match.Groups[match.Groups.Count - 1].Value;
                    if (potentialSecret.Length >= MinSecretLength)
                    {
                        potentialSecrets.Add(potentialSecret);
                    }
                }
            }
        }
        
        return potentialSecrets.Count > 0;
    }
}
```

## 🚨 Security Best Practices

### For Development Teams

#### 1. Secret Handling
```csharp
// DO: Always register secrets with masker
HostContext.SecretMasker.AddValue(sensitiveValue, "ComponentName");

// DON'T: Log sensitive values directly
Trace.Info($"Processing token: {token}"); // NEVER DO THIS

// DO: Use masked logging
Trace.Info($"Processing token: {HostContext.SecretMasker.MaskSecrets(token)}");
```

#### 2. Input Validation
```csharp
// DO: Validate all inputs
public void ProcessUserInput(string input)
{
    ArgUtil.NotNullOrEmpty(input, nameof(input));
    
    if (input.Length > MaxInputLength)
    {
        throw new ArgumentException($"Input exceeds maximum length of {MaxInputLength}");
    }
    
    if (ContainsSuspiciousPatterns(input))
    {
        throw new SecurityException("Input contains potentially malicious content");
    }
}
```

#### 3. Resource Access
```csharp
// DO: Use least privilege principle
public void AccessFile(string filePath)
{
    var allowedPaths = GetAllowedPaths();
    var fullPath = Path.GetFullPath(filePath);
    
    if (!allowedPaths.Any(allowed => fullPath.StartsWith(allowed)))
    {
        throw new UnauthorizedAccessException($"Access denied to path: {filePath}");
    }
}
```

### For Operations Teams

#### 1. Agent Configuration
- **Use dedicated service accounts** with minimum required permissions
- **Enable certificate validation** unless in trusted environments
- **Regular token rotation** for authentication
- **Monitor resource usage** for unusual patterns

#### 2. Network Security
- **Firewall rules** restricting outbound connections to Azure DevOps
- **Proxy configuration** for corporate environments
- **Certificate pinning** where possible
- **Network segmentation** for agent infrastructure

#### 3. Monitoring and Alerting
- **Security event monitoring** for suspicious activities
- **Resource usage alerts** for potential attacks
- **Failed authentication monitoring**
- **Regular security audits** of agent configurations

## 🔄 Security Maintenance

### Regular Security Tasks

#### 1. Certificate Management
```powershell
# Check certificate expiration
Get-ChildItem Cert:\LocalMachine\My | Where-Object {$_.NotAfter -lt (Get-Date).AddDays(30)}

# Update certificates
./config.cmd --configure-certificates
```

#### 2. Secret Rotation
```powershell
# Rotate PAT tokens
./config.cmd --auth pat --token $newToken --replace
```

#### 3. Security Updates
```powershell
# Update agent to latest version
./run.cmd --once --update
```

## 🎯 Security Compliance

### Industry Standards
- **OWASP Top 10**: Protection against common vulnerabilities
- **NIST Cybersecurity Framework**: Comprehensive security controls
- **SOC 2 Type 2**: Data protection and availability
- **ISO 27001**: Information security management

### Compliance Features
- **Audit logging**: Comprehensive security event tracking
- **Data encryption**: At rest and in transit
- **Access controls**: Role-based and capability-based
- **Data retention**: Configurable log retention policies

---

**Security is a shared responsibility**: Development teams implement secure coding practices, operations teams maintain secure infrastructure, and the security team provides oversight and governance.
