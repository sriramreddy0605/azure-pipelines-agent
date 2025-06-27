# Enhanced Linux Proxy Pre-Authentication

## Overview

This enhancement addresses the issue where Linux Azure Pipelines agents fail to work behind authenticated proxies that require credentials on the first request (pre-authentication scenarios), while Windows agents work correctly.

## Problem Description

Some corporate proxy configurations require HTTP clients to send credentials immediately with the first request rather than waiting for a 407 Proxy Authentication Required response. This is known as "pre-authentication" and is more common in high-security environments.

### Platform Differences
- **Windows**: WinHTTP (used by HttpClientHandler) is more aggressive about proxy authentication
- **Linux/macOS**: libcurl (used by HttpClientHandler) typically waits for 407 responses before sending credentials
- **Pre-auth proxies**: Some proxies require credentials immediately and don't send 407 challenges

## Solution

The enhanced proxy pre-authentication system includes several improvements:

### 1. Enhanced Standard Method
The `CreateHttpClientHandler()` method now includes:
- Explicit `UseProxy = true` setting
- Enhanced logging for proxy configuration
- Better credential validation

### 2. New Specialized Method
Added `CreateProxyPreAuthHttpClientHandler(bool forceProxyAuth = false)` which provides:
- Enhanced proxy authentication logging
- More detailed proxy configuration
- Option to force pre-authentication (useful for testing)
- Better error handling and diagnostics

### 3. Utility Method
Added `CreatePreAuthProxy()` which returns a pre-configured proxy object for custom scenarios.

## Usage Examples

### Standard Usage (Recommended)
```csharp
// This automatically enables pre-authentication on Linux/macOS when proxy credentials are available
using (var handler = HostContext.CreateHttpClientHandler())
using (var httpClient = new HttpClient(handler))
{
    var response = await httpClient.GetAsync("https://api.example.com");
}
```

### Enhanced Usage
```csharp
// For scenarios requiring additional logging and explicit proxy configuration
using (var handler = HostContext.CreateProxyPreAuthHttpClientHandler())
using (var httpClient = new HttpClient(handler))
{
    var response = await httpClient.GetAsync("https://api.example.com");
}
```

### Testing Usage
```csharp
// Force pre-authentication even on Windows for testing
using (var handler = HostContext.CreateProxyPreAuthHttpClientHandler(forceProxyAuth: true))
using (var httpClient = new HttpClient(handler))
{
    var response = await httpClient.GetAsync("https://api.example.com");
}
```

### Custom Proxy Usage
```csharp
// Get a pre-configured proxy for custom scenarios
var proxy = HostContext.CreatePreAuthProxy();
using (var handler = new HttpClientHandler())
{
    handler.Proxy = proxy;
    handler.UseProxy = proxy != null;
    
    if ((PlatformUtil.RunningOnLinux || PlatformUtil.RunningOnMacOS) && proxy != null)
    {
        handler.PreAuthenticate = true;
    }
    
    using (var httpClient = new HttpClient(handler))
    {
        var response = await httpClient.GetAsync("https://api.example.com");
    }
}
```

## Configuration

The agent should be configured with proxy settings as usual:

### Command Line Configuration
```bash
./config.sh --proxyurl http://proxy.example.com:8080 --proxyusername myuser --proxypassword mypass
```

### Environment Variables
```bash
export VSTS_HTTP_PROXY_USERNAME=myuser
export VSTS_HTTP_PROXY_PASSWORD=mypass
echo "http://proxy.example.com:8080" > .proxy
```

### Configuration File
Create a `.proxy` file in the agent root directory:
```
http://proxy.example.com:8080
```

## Pre-Authentication Conditions

Pre-authentication is automatically enabled when ALL of the following conditions are met:

1. **Platform**: Running on Linux or macOS (or forced via parameter)
2. **Proxy Address**: A proxy address is configured
3. **Credentials**: Both proxy username and password are provided
4. **WebProxy**: The WebProxy object has credentials configured

## Diagnostics

### Enable Detailed Logging
Set the following environment variable for detailed proxy logging:
```bash
export VSTS_AGENT_LOG_LEVEL=verbose
```

### Check Configuration
Use the diagnostic method to verify proxy configuration:
```csharp
var example = new ProxyPreAuthExample();
example.DiagnoseProxyConfiguration();
```

### Log Messages to Look For
When pre-authentication is working correctly, you should see log messages like:
```
[INFO] Enhanced proxy pre-authentication enabled for authenticated proxy: http://proxy.example.com:8080
[INFO] Proxy pre-authentication enabled: PreAuthenticate=True, UseProxy=True, Platform=Linux
```

## Troubleshooting

### Common Issues

1. **Credentials Not Set**: Ensure both `VSTS_HTTP_PROXY_USERNAME` and `VSTS_HTTP_PROXY_PASSWORD` are set
2. **Proxy URL Format**: Ensure the proxy URL includes the protocol (http:// or https://)
3. **Platform Detection**: Verify the agent is running on Linux/macOS for automatic pre-auth
4. **Network Connectivity**: Test basic connectivity to the proxy server

### Testing Proxy Configuration
```bash
# Test basic proxy connectivity
curl -x http://username:password@proxy.example.com:8080 https://httpbin.org/ip

# Verify agent can reach Azure DevOps through proxy
./Agent.Listener configure --help
```

### Debug Output
Add this to your agent code for debugging:
```csharp
var agentWebProxy = HostContext.GetService<IVstsAgentWebProxy>();
Console.WriteLine($"Proxy Address: {agentWebProxy.ProxyAddress}");
Console.WriteLine($"Has Username: {!string.IsNullOrEmpty(agentWebProxy.ProxyUsername)}");
Console.WriteLine($"Has Password: {!string.IsNullOrEmpty(agentWebProxy.ProxyPassword)}");
Console.WriteLine($"Platform: {PlatformUtil.HostOS}");
```

## Files Modified

- `src/Microsoft.VisualStudio.Services.Agent/HostContext.cs` - Enhanced proxy pre-authentication methods
- `src/Microsoft.VisualStudio.Services.Agent/ProxyPreAuthExample.cs` - Usage examples and diagnostics

## Backward Compatibility

This enhancement is fully backward compatible:
- Existing functionality remains unchanged
- Pre-authentication is only enabled when conditions are met
- No impact on agents not using authenticated proxies
- No impact on Windows agents (they already work correctly)
- No breaking changes to existing APIs

## Performance Impact

The changes have minimal performance impact:
- Pre-authentication only affects the initial proxy handshake
- No additional network round trips are required
- Logging overhead is minimal and only occurs when proxy is configured
- Memory usage is unchanged

## Security Considerations

- Proxy credentials are handled securely using existing agent credential storage
- No credentials are logged in plain text
- Pre-authentication only sends credentials to the configured proxy server
- All existing security measures remain in place
