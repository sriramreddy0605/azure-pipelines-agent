// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Agent.Sdk;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Examples
{
    /// <summary>
    /// Example demonstrating the enhanced proxy pre-authentication capabilities
    /// for Linux agents behind authenticated proxies that require credentials
    /// on the first request (pre-auth scenarios).
    /// </summary>
    public class ProxyPreAuthExample : AgentService
    {
        /// <summary>
        /// Example 1: Using the standard enhanced CreateHttpClientHandler method
        /// This is the recommended approach for most scenarios as it automatically
        /// enables pre-authentication when running on Linux/macOS with proxy credentials.
        /// </summary>
        public async Task<string> StandardProxyPreAuthExample(string url)
        {
            var trace = HostContext.GetTrace("ProxyPreAuthExample");
            trace.Info("Making HTTP request using standard proxy pre-authentication: {0}", url);

            // The standard CreateHttpClientHandler method now automatically enables
            // proxy pre-authentication on Linux/macOS when proxy credentials are available
            using (var handler = HostContext.CreateHttpClientHandler())
            {
                handler.CheckCertificateRevocationList = true;
                using (var httpClient = new HttpClient(handler))
                {
                    try
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(30);
                        var response = await httpClient.GetAsync(url);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            trace.Info("HTTP request successful with status: {0}", response.StatusCode);
                            return await response.Content.ReadAsStringAsync();
                        }
                        else
                        {
                            trace.Warning("HTTP request failed with status: {0}", response.StatusCode);
                            return null;
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        trace.Error("HTTP request failed: {0}", ex.Message);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Example 2: Using the enhanced CreateProxyPreAuthHttpClientHandler method
        /// This method provides additional logging and explicit proxy configuration
        /// for scenarios requiring more detailed proxy handling.
        /// </summary>
        public async Task<string> EnhancedProxyPreAuthExample(string url)
        {
            var trace = HostContext.GetTrace("ProxyPreAuthExample");
            trace.Info("Making HTTP request using enhanced proxy pre-authentication: {0}", url);

            // The enhanced method provides more detailed logging and explicit proxy configuration
            using (var handler = HostContext.CreateProxyPreAuthHttpClientHandler())
            {
                handler.CheckCertificateRevocationList = true;
                using (var httpClient = new HttpClient(handler))
                {
                try
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    
                    // Add custom headers if needed for proxy authentication scenarios
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Azure-Pipelines-Agent-PreAuth/1.0");
                    
                    var response = await httpClient.GetAsync(url);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        trace.Info("Enhanced HTTP request successful with status: {0}", response.StatusCode);
                        return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        trace.Warning("Enhanced HTTP request failed with status: {0}", response.StatusCode);
                        return null;
                    }
                }
                catch (HttpRequestException ex)
                {
                    trace.Error("Enhanced HTTP request failed: {0}", ex.Message);
                    throw;
                }
            }
        }
        }

        /// <summary>
        /// Example 3: Testing proxy pre-authentication forcefully
        /// This method can be used for testing proxy scenarios even on Windows
        /// by forcing the pre-authentication behavior.
        /// </summary>
        public async Task<bool> TestProxyPreAuthentication(string testUrl)
        {
            var trace = HostContext.GetTrace("ProxyPreAuthExample");
            trace.Info("Testing proxy pre-authentication functionality: {0}", testUrl);

            try
            {
                // Force proxy pre-authentication even on Windows for testing
                using (var handler = HostContext.CreateProxyPreAuthHttpClientHandler(forceProxyAuth: true))
                {
                    handler.CheckCertificateRevocationList = true;
                    using (var httpClient = new HttpClient(handler))
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(10);
                        
                        var response = await httpClient.GetAsync(testUrl);
                        bool success = response.IsSuccessStatusCode;
                        
                        trace.Info("Proxy pre-authentication test result: {0} (Status: {1})",
                                   success ? "SUCCESS" : "FAILED", response.StatusCode);
                        
                        return success;
                    }
                }
            }
            catch (Exception ex)
            {
                trace.Error("Proxy pre-authentication test failed: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Example 4: Using the CreatePreAuthProxy utility for custom scenarios
        /// This method demonstrates how to get a pre-configured proxy object
        /// for use in custom HTTP handling scenarios.
        /// </summary>
        public async Task<string> CustomProxyExample(string url)
        {
            var trace = HostContext.GetTrace("ProxyPreAuthExample");
            trace.Info("Making HTTP request using custom proxy configuration: {0}", url);

            // Get a pre-configured proxy for custom scenarios
            var proxy = HostContext.CreatePreAuthProxy();
            
            using (var handler = new HttpClientHandler())
            {
                handler.CheckCertificateRevocationList = true;
                handler.Proxy = proxy;
                handler.UseProxy = proxy != null;
                
                // Enable pre-authentication for Linux/macOS if we have a proxy
                if ((PlatformUtil.RunningOnLinux || PlatformUtil.RunningOnMacOS) && proxy != null)
                {
                    handler.PreAuthenticate = true;
                    trace.Info("Enabled custom proxy pre-authentication");
                }

                using (var httpClient = new HttpClient(handler))
                {
                    try
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(30);
                        var response = await httpClient.GetAsync(url);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            trace.Info("Custom proxy HTTP request successful");
                            return await response.Content.ReadAsStringAsync();
                        }
                        else
                        {
                            trace.Warning("Custom proxy HTTP request failed with status: {0}", response.StatusCode);
                            return null;
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        trace.Error("Custom proxy HTTP request failed: {0}", ex.Message);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Diagnostic method to check proxy configuration and pre-authentication status
        /// </summary>
        public void DiagnoseProxyConfiguration()
        {
            var trace = HostContext.GetTrace("ProxyPreAuthExample");
            var agentWebProxy = HostContext.GetService<IVstsAgentWebProxy>();
            
            trace.Info("=== Proxy Configuration Diagnostics ===");
            trace.Info("Platform: {0}", PlatformUtil.HostOS);
            trace.Info("Proxy Address: {0}", string.IsNullOrEmpty(agentWebProxy.ProxyAddress) ? "Not configured" : agentWebProxy.ProxyAddress);
            trace.Info("Has Proxy Username: {0}", !string.IsNullOrEmpty(agentWebProxy.ProxyUsername));
            trace.Info("Has Proxy Password: {0}", !string.IsNullOrEmpty(agentWebProxy.ProxyPassword));
            trace.Info("Proxy Bypass List Count: {0}", agentWebProxy.ProxyBypassList?.Count ?? 0);
            trace.Info("WebProxy Credentials Available: {0}", agentWebProxy.WebProxy?.Credentials != null);
            
            // Check if pre-authentication would be enabled
            bool wouldEnablePreAuth = (PlatformUtil.RunningOnLinux || PlatformUtil.RunningOnMacOS) &&
                                    agentWebProxy.WebProxy?.Credentials != null &&
                                    !string.IsNullOrEmpty(agentWebProxy.ProxyAddress) &&
                                    !string.IsNullOrEmpty(agentWebProxy.ProxyUsername) &&
                                    !string.IsNullOrEmpty(agentWebProxy.ProxyPassword);
            
            trace.Info("Pre-authentication would be enabled: {0}", wouldEnablePreAuth);
            trace.Info("=== End Proxy Diagnostics ===");
        }
    }
}
