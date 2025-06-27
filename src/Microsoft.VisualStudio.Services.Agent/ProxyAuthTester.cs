// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent
{
    /// <summary>
    /// A diagnostic tool to test proxy authentication scenarios and help troubleshoot
    /// proxy connectivity issues, particularly on Linux systems.
    /// </summary>
    public static class ProxyAuthTester
    {
        /// <summary>
        /// Test proxy authentication by making a simple HTTP request through the proxy.
        /// This helps diagnose whether credentials are being sent properly.
        /// </summary>
        /// <param name="context">The host context</param>
        /// <param name="testUrl">URL to test (default: http://httpbin.org/ip)</param>
        /// <returns>Test results and diagnostic information</returns>
        public static async Task<ProxyTestResult> TestProxyAuthenticationAsync(IHostContext context, string testUrl = "http://httpbin.org/ip")
        {
            var result = new ProxyTestResult();
            var agentWebProxy = context.GetService<IVstsAgentWebProxy>();
            var trace = context.GetTrace("ProxyAuthTester");
            
            result.ProxyAddress = agentWebProxy.ProxyAddress;
            result.ProxyUsername = agentWebProxy.ProxyUsername;
            result.HasProxyCredentials = !string.IsNullOrEmpty(agentWebProxy.ProxyUsername) && !string.IsNullOrEmpty(agentWebProxy.ProxyPassword);
            result.Platform = PlatformUtil.HostOS.ToString();
            
            trace.Info("=== Proxy Authentication Test ===");
            trace.Info("Proxy Address: {0}", agentWebProxy.ProxyAddress ?? "None");
            trace.Info("Proxy Username: {0}", agentWebProxy.ProxyUsername ?? "None");
            trace.Info("Has Credentials: {0}", result.HasProxyCredentials);
            trace.Info("Platform: {0}", result.Platform);
            trace.Info("Test URL: {0}", testUrl);
            
            if (string.IsNullOrEmpty(agentWebProxy.ProxyAddress))
            {
                result.Success = false;
                result.ErrorMessage = "No proxy address configured";
                trace.Warning("No proxy address configured - skipping test");
                return result;
            }
            
            // Test 1: Standard HttpClientHandler approach
            trace.Info("--- Test 1: Standard HttpClientHandler ---");
            await TestStandardApproach(context, testUrl, result, trace);
            
            // Test 2: Manual proxy authentication headers approach
            trace.Info("--- Test 2: Manual Proxy-Authorization Header ---");
            await TestManualHeaderApproach(context, testUrl, result, trace);
            
            // Test 3: Curl-like approach with raw HttpClient
            trace.Info("--- Test 3: Raw HttpClient with Manual Auth ---");
            await TestRawHttpClientApproach(context, testUrl, result, trace);
            
            trace.Info("=== Proxy Authentication Test Complete ===");
            return result;
        }
        
        private static async Task TestStandardApproach(IHostContext context, string testUrl, ProxyTestResult result, Tracing trace)
        {
            try
            {
                using var handler = context.CreateHttpClientHandler();
                using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
                
                trace.Info("Using CreateHttpClientHandler with PreAuthenticate={0}", handler.PreAuthenticate);
                trace.Info("Proxy type: {0}", handler.Proxy?.GetType()?.Name ?? "None");
                
                var response = await client.GetAsync(testUrl);
                result.StandardApproachSuccess = response.IsSuccessStatusCode;
                result.StandardApproachStatusCode = (int)response.StatusCode;
                result.StandardApproachResponse = await response.Content.ReadAsStringAsync();
                
                trace.Info("Standard approach result: {0} ({1})", response.StatusCode, (int)response.StatusCode);
                if (!response.IsSuccessStatusCode)
                {
                    trace.Warning("Standard approach failed: {0}", result.StandardApproachResponse);
                }
            }
            catch (Exception ex)
            {
                result.StandardApproachSuccess = false;
                result.StandardApproachError = ex.Message;
                trace.Error("Standard approach exception: {0}", ex.Message);
            }
        }
        
        private static async Task TestManualHeaderApproach(IHostContext context, string testUrl, ProxyTestResult result, Tracing trace)
        {
            try
            {
                var agentWebProxy = context.GetService<IVstsAgentWebProxy>();
                
                // Create handler with proxy but without automatic authentication
                using var handler = new HttpClientHandler()
                {
                    Proxy = new WebProxy(new Uri(agentWebProxy.ProxyAddress)),
                    UseProxy = true,
                    PreAuthenticate = false // Disable automatic authentication
                };
                
                using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
                
                // Manually add Proxy-Authorization header
                if (result.HasProxyCredentials)
                {
                    var username = agentWebProxy.ProxyUsername;
                    var domain = string.Empty;
                    
                    // Handle domain authentication
                    if (username.Contains("\\"))
                    {
                        var parts = username.Split('\\');
                        domain = parts[0];
                        username = parts[1];
                    }
                    
                    var credentials = $"{username}:{agentWebProxy.ProxyPassword}";
                    var encodedCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));
                    var proxyAuthHeader = $"Basic {encodedCredentials}";
                    
                    client.DefaultRequestHeaders.Add("Proxy-Authorization", proxyAuthHeader);
                    trace.Info("Added manual Proxy-Authorization header for user: {0}", 
                        string.IsNullOrEmpty(domain) ? username : $"{domain}\\{username}");
                }
                
                var response = await client.GetAsync(testUrl);
                result.ManualHeaderSuccess = response.IsSuccessStatusCode;
                result.ManualHeaderStatusCode = (int)response.StatusCode;
                result.ManualHeaderResponse = await response.Content.ReadAsStringAsync();
                
                trace.Info("Manual header approach result: {0} ({1})", response.StatusCode, (int)response.StatusCode);
                if (!response.IsSuccessStatusCode)
                {
                    trace.Warning("Manual header approach failed: {0}", result.ManualHeaderResponse);
                }
                else
                {
                    trace.Info("Manual header approach succeeded!");
                    result.Success = true;
                }
            }
            catch (Exception ex)
            {
                result.ManualHeaderSuccess = false;
                result.ManualHeaderError = ex.Message;
                trace.Error("Manual header approach exception: {0}", ex.Message);
            }
        }
        
        private static async Task TestRawHttpClientApproach(IHostContext context, string testUrl, ProxyTestResult result, Tracing trace)
        {
            try
            {
                var agentWebProxy = context.GetService<IVstsAgentWebProxy>();
                
                // Create a completely raw HttpClient with minimal proxy settings
                using var handler = new HttpClientHandler()
                {
                    Proxy = new WebProxy(agentWebProxy.ProxyAddress),
                    UseProxy = true,
                    PreAuthenticate = true,
                    UseDefaultCredentials = false
                };
                
                // Set up credentials directly on the proxy
                if (result.HasProxyCredentials)
                {
                    var username = agentWebProxy.ProxyUsername;
                    var domain = string.Empty;
                    
                    if (username.Contains("\\"))
                    {
                        var parts = username.Split('\\');
                        domain = parts[0];
                        username = parts[1];
                    }
                    
                    var netCred = new NetworkCredential(username, agentWebProxy.ProxyPassword, domain);
                    ((WebProxy)handler.Proxy).Credentials = netCred;
                }
                
                using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
                
                var response = await client.GetAsync(testUrl);
                result.RawClientSuccess = response.IsSuccessStatusCode;
                result.RawClientStatusCode = (int)response.StatusCode;
                result.RawClientResponse = await response.Content.ReadAsStringAsync();
                
                trace.Info("Raw client approach result: {0} ({1})", response.StatusCode, (int)response.StatusCode);
                if (!response.IsSuccessStatusCode)
                {
                    trace.Warning("Raw client approach failed: {0}", result.RawClientResponse);
                }
                else if (!result.Success)
                {
                    trace.Info("Raw client approach succeeded!");
                    result.Success = true;
                }
            }
            catch (Exception ex)
            {
                result.RawClientSuccess = false;
                result.RawClientError = ex.Message;
                trace.Error("Raw client approach exception: {0}", ex.Message);
            }
        }
    }
    
    /// <summary>
    /// Results from proxy authentication testing
    /// </summary>
    public class ProxyTestResult
    {
        public string ProxyAddress { get; set; }
        public string ProxyUsername { get; set; }
        public bool HasProxyCredentials { get; set; }
        public string Platform { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        
        // Standard approach results
        public bool StandardApproachSuccess { get; set; }
        public int StandardApproachStatusCode { get; set; }
        public string StandardApproachResponse { get; set; }
        public string StandardApproachError { get; set; }
        
        // Manual header approach results
        public bool ManualHeaderSuccess { get; set; }
        public int ManualHeaderStatusCode { get; set; }
        public string ManualHeaderResponse { get; set; }
        public string ManualHeaderError { get; set; }
        
        // Raw client approach results
        public bool RawClientSuccess { get; set; }
        public int RawClientStatusCode { get; set; }
        public string RawClientResponse { get; set; }
        public string RawClientError { get; set; }
        
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Proxy Test Results:");
            sb.AppendLine($"  Proxy: {ProxyAddress}");
            sb.AppendLine($"  Username: {ProxyUsername}");
            sb.AppendLine($"  Has Credentials: {HasProxyCredentials}");
            sb.AppendLine($"  Platform: {Platform}");
            sb.AppendLine($"  Overall Success: {Success}");
            sb.AppendLine($"  Standard Approach: {StandardApproachSuccess} (Code: {StandardApproachStatusCode})");
            sb.AppendLine($"  Manual Header: {ManualHeaderSuccess} (Code: {ManualHeaderStatusCode})");
            sb.AppendLine($"  Raw Client: {RawClientSuccess} (Code: {RawClientStatusCode})");
            
            if (!string.IsNullOrEmpty(ErrorMessage))
                sb.AppendLine($"  Error: {ErrorMessage}");
                
            return sb.ToString();
        }
    }
}
