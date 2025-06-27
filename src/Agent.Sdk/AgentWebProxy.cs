// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Agent.Sdk
{
    public class AgentWebProxySettings
    {
        public static string AgentProxyUrlKey = "Agent.ProxyUrl".ToLower();
        public static string AgentProxyUsernameKey = "Agent.ProxyUsername".ToLower();
        public static string AgentProxyPasswordKey = "Agent.ProxyPassword".ToLower();
        public static string AgentProxyBypassListKey = "Agent.ProxyBypassList".ToLower();
        public string ProxyAddress { get; set; }
        public string ProxyUsername { get; set; }
        public string ProxyPassword { get; set; }
        public List<string> ProxyBypassList { get; set; }
        public IWebProxy WebProxy { get; set; }
    }

    public class AgentWebProxy : IWebProxy
    {
        private string _proxyAddress;
        private readonly List<Regex> _regExBypassList = new List<Regex>();

        public ICredentials Credentials { get; set; }

        public AgentWebProxy()
        {
        }

        public AgentWebProxy(string proxyAddress, string proxyUsername, string proxyPassword, List<string> proxyBypassList)
        {
            Update(proxyAddress, proxyUsername, proxyPassword, proxyBypassList);
        }

        public void Update(string proxyAddress, string proxyUsername, string proxyPassword, List<string> proxyBypassList)
        {
            _proxyAddress = proxyAddress?.Trim();

            if (string.IsNullOrEmpty(proxyUsername) || string.IsNullOrEmpty(proxyPassword))
            {
                Credentials = CredentialCache.DefaultNetworkCredentials;
            }
            else
            {
                // Enhanced proxy authentication for Linux/macOS with domain support and aggressive credential caching
                if ((PlatformUtil.RunningOnLinux || PlatformUtil.RunningOnMacOS) && !string.IsNullOrEmpty(_proxyAddress))
                {
                    try
                    {
                        var proxyUri = new Uri(_proxyAddress);
                        var credCache = new CredentialCache();
                        
                        // Handle domain-based authentication by checking if username contains domain
                        var username = proxyUsername;
                        var domain = string.Empty;
                        if (username.Contains("\\"))
                        {
                            var parts = username.Split('\\');
                            domain = parts[0];
                            username = parts[1];
                        }
                        
                        var netCred = new NetworkCredential(username, proxyPassword, domain);
                        
                        // Add credentials for multiple authentication schemes that the proxy might use
                        // This forces .NET to send credentials proactively without waiting for 407 challenge
                        credCache.Add(proxyUri, "Basic", netCred);
                        credCache.Add(proxyUri, "Digest", netCred);
                        credCache.Add(proxyUri, "NTLM", netCred);
                        credCache.Add(proxyUri, "Negotiate", netCred);
                        
                        Credentials = credCache;
                    }
                    catch
                    {
                        // Fallback to standard NetworkCredential if the aggressive approach fails
                        Credentials = new NetworkCredential(proxyUsername, proxyPassword);
                    }
                }
                else
                {
                    // Standard approach for Windows or when proxy address is not available
                    Credentials = new NetworkCredential(proxyUsername, proxyPassword);
                }
            }

            if (proxyBypassList != null)
            {
                foreach (string bypass in proxyBypassList)
                {
                    if (string.IsNullOrWhiteSpace(bypass))
                    {
                        continue;
                    }
                    else
                    {
                        try
                        {
                            Regex bypassRegex = new Regex(bypass.Trim(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ECMAScript);
                            _regExBypassList.Add(bypassRegex);
                        }
                        catch (Exception)
                        {
                            // eat all exceptions
                        }
                    }
                }
            }
        }

        public Uri GetProxy(Uri destination)
        {
            if (IsBypassed(destination))
            {
                return destination;
            }
            else
            {
                return new Uri(_proxyAddress);
            }
        }

        public bool IsBypassed(Uri uri)
        {
            ArgUtil.NotNull(uri, nameof(uri));
            return string.IsNullOrEmpty(_proxyAddress) || uri.IsLoopback || IsMatchInBypassList(uri);
        }

        private bool IsMatchInBypassList(Uri input)
        {
            string matchUriString = input.IsDefaultPort ?
                input.Scheme + "://" + input.Host :
                input.Scheme + "://" + input.Host + ":" + input.Port.ToString();

            foreach (Regex r in _regExBypassList)
            {
                if (r.IsMatch(matchUriString))
                {
                    return true;
                }
            }

            return false;
        }
    }
}