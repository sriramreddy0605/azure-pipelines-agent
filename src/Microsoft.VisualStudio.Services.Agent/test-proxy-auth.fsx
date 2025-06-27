#!/usr/bin/env dotnet fsi

// Simple test script to verify proxy authentication on Linux
// Usage: dotnet fsi test-proxy-auth.fsx <proxy_url> <username> <password>

#r "System.Net.Http"

open System
open System.Net
open System.Net.Http
open System.Text
open System.Threading.Tasks

let testProxyAuth proxyUrl username password =
    async {
        printfn "Testing proxy authentication:"
        printfn "  Proxy: %s" proxyUrl
        printfn "  Username: %s" username
        printfn "  Platform: %s" Environment.OSVersion.Platform.ToString()
        
        let testUrl = "http://httpbin.org/ip"
        
        // Test 1: Manual Proxy-Authorization header (should work)
        printfn "\n--- Test 1: Manual Proxy-Authorization Header ---"
        try
            use handler = new HttpClientHandler()
            handler.Proxy <- new WebProxy(Uri(proxyUrl))
            handler.UseProxy <- true
            handler.PreAuthenticate <- false
            
            use client = new HttpClient(handler)
            client.Timeout <- TimeSpan.FromSeconds(30.0)
            
            // Create Basic auth header
            let credentials = sprintf "%s:%s" username password
            let encodedCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials))
            let proxyAuthHeader = sprintf "Basic %s" encodedCredentials
            
            client.DefaultRequestHeaders.Add("Proxy-Authorization", proxyAuthHeader)
            
            printfn "Sending request with manual Proxy-Authorization header..."
            let! response = client.GetAsync(testUrl) |> Async.AwaitTask
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            
            printfn "Status: %A (%d)" response.StatusCode (int response.StatusCode)
            if response.IsSuccessStatusCode then
                printfn "SUCCESS: Manual proxy auth worked!"
                printfn "Response: %s" (content.Substring(0, Math.Min(200, content.Length)))
            else
                printfn "FAILED: %s" content
                
        with ex ->
            printfn "ERROR: %s" ex.Message
        
        // Test 2: Standard .NET approach (likely to fail on Linux)
        printfn "\n--- Test 2: Standard .NET Proxy Authentication ---"
        try
            use handler = new HttpClientHandler()
            handler.Proxy <- new WebProxy(Uri(proxyUrl))
            handler.UseProxy <- true
            handler.PreAuthenticate <- true
            
            // Set credentials on proxy
            let proxy = handler.Proxy :?> WebProxy
            proxy.Credentials <- new NetworkCredential(username, password)
            
            use client = new HttpClient(handler)
            client.Timeout <- TimeSpan.FromSeconds(30.0)
            
            printfn "Sending request with standard .NET proxy authentication..."
            let! response = client.GetAsync(testUrl) |> Async.AwaitTask
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            
            printfn "Status: %A (%d)" response.StatusCode (int response.StatusCode)
            if response.IsSuccessStatusCode then
                printfn "SUCCESS: Standard proxy auth worked!"
                printfn "Response: %s" (content.Substring(0, Math.Min(200, content.Length)))
            else
                printfn "FAILED: %s" content
                
        with ex ->
            printfn "ERROR: %s" ex.Message
        
        printfn "\n--- Test Complete ---"
    }

// Parse command line arguments
let args = Environment.GetCommandLineArgs()
if args.Length < 4 then
    printfn "Usage: dotnet fsi test-proxy-auth.fsx <proxy_url> <username> <password>"
    printfn "Example: dotnet fsi test-proxy-auth.fsx http://172.18.0.4:8080 myuser mypass"
    exit 1

let proxyUrl = args.[1]
let username = args.[2]
let password = args.[3]

// Run the test
testProxyAuth proxyUrl username password |> Async.RunSynchronously
