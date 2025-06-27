# Linux Proxy Pre-Authentication Enhancement

## Problem Description

The Azure DevOps agent on Linux fails to work behind authenticated proxies that require credentials to be sent with the first request (pre-authentication), while the Windows agent works fine. This is due to a difference in how .NET Core handles proxy authentication on different platforms.

On Windows, the HttpClientHandler automatically handles proxy authentication more aggressively, while on Linux, it waits for a 407 Proxy Authentication Required response before sending credentials. Some proxy configurations (especially corporate proxies with pre-authentication requirements) don't send a 407 challenge and instead require credentials with the initial request.

## Solution

The solution involves enabling the `PreAuthenticate` property on `HttpClientHandler` when:
1. Running on Linux or macOS
2. Proxy credentials are configured
3. A proxy address is specified

## Changes Made

### 1. Enhanced `CreateHttpClientHandler` Method

Modified `src/Microsoft.VisualStudio.Services.Agent/HostContext.cs`:

```csharp
public static HttpClientHandler CreateHttpClientHandler(this IHostContext context)
{
    ArgUtil.NotNull(context, nameof(context));
    HttpClientHandler clientHandler = new HttpClientHandler();
    var agentWebProxy = context.GetService<IVstsAgentWebProxy>();
    clientHandler.Proxy = agentWebProxy.WebProxy;

    // Enable proxy pre-authentication on Linux when proxy credentials are available
    // This is needed because some proxy servers require credentials on the first request
    // and don't send a 407 challenge response that would trigger authentication
    if ((PlatformUtil.RunningOnLinux || PlatformUtil.RunningOnMacOS) &&
        agentWebProxy.WebProxy?.Credentials != null &&
        !string.IsNullOrEmpty(agentWebProxy.ProxyAddress) &&
        !string.IsNullOrEmpty(agentWebProxy.ProxyUsername) &&
        !string.IsNullOrEmpty(agentWebProxy.ProxyPassword))
    {
        clientHandler.PreAuthenticate = true;
    }

    // ... rest of the method remains the same
}
```

### 2. Added Specialized Method for Enhanced Proxy Support

Added `CreateLinuxProxyAwareHttpClientHandler` method that provides more detailed logging and explicit proxy configuration for scenarios requiring additional proxy handling.

## Usage

### For Existing Code
No changes required! The existing `HostContext.CreateHttpClientHandler()` method will automatically enable pre-authentication on Linux when proxy credentials are available.

### For New Code Requiring Enhanced Proxy Support
Use the new specialized method:

```csharp
using (var handler = HostContext.CreateLinuxProxyAwareHttpClientHandler())
using (var httpClient = new HttpClient(handler))
{
    // Your HTTP requests will now properly authenticate with pre-auth proxies
    var response = await httpClient.GetAsync("https://example.com");
}
```

## Configuration

The agent should be configured with proxy settings as usual:

```bash
# During agent configuration
./config.sh --proxyurl http://proxy.example.com:8080 --proxyusername myuser --proxypassword mypass

# Or via environment variables
export VSTS_HTTP_PROXY_USERNAME=myuser
export VSTS_HTTP_PROXY_PASSWORD=mypass
echo "http://proxy.example.com:8080" > .proxy
```

## Technical Details

### Why This Happens
- **Windows**: WinHTTP (used by HttpClientHandler on Windows) is more aggressive about proxy authentication
- **Linux**: libcurl (used by HttpClientHandler on Linux) waits for explicit 407 responses before sending credentials
- **Pre-auth proxies**: Some corporate proxies require credentials immediately and don't send 407 challenges

### The Fix
Setting `PreAuthenticate = true` tells the HttpClientHandler to include proxy credentials with the initial request instead of waiting for a challenge-response cycle.

## Testing

To test the fix:

1. Configure a Linux agent behind an authenticated proxy that requires pre-authentication
2. Run a build that makes HTTP requests
3. Verify that HTTP requests succeed without 407 authentication errors

## Backward Compatibility

This change is fully backward compatible:
- Existing functionality is unchanged
- Only adds pre-authentication when proxy credentials are available
- No impact on agents not using authenticated proxies
- No impact on Windows agents (they already work correctly)

## Files Modified

- `src/Microsoft.VisualStudio.Services.Agent/HostContext.cs` - Enhanced proxy pre-authentication support
