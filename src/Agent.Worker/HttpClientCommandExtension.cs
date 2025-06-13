// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public sealed class AgentHealthProbeCommandExtension : BaseWorkerCommandExtension
    {
        public AgentHealthProbeCommandExtension()
        {
            CommandArea = "agenthealthprobe";
            SupportedHostTypes = HostTypes.All;
            InstallWorkerCommand(new AgentHealthProbeCallCommand());
        }
    }

    public sealed class AgentHealthProbeCallCommand : IWorkerCommand
    {
        public string Name => "call";
        public List<string> Aliases => null;

        public void Execute(IExecutionContext context, Command command)
        {
            var url = "https://download.agent.dev.azure.com/agent/health/probe";
            context.Output($"Calling health probe URL: {url}");
            using (var client = new HttpClient())
            {
                var responseTask = client.GetAsync(url);
                responseTask.Wait();
                var response = responseTask.Result;
                var contentTask = response.Content.ReadAsStringAsync();
                contentTask.Wait();
                var content = contentTask.Result;
                context.Output($"Response Status: {response.StatusCode}");
                context.Output($"Response Body: {content}");
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Health probe failed with status code: {response.StatusCode}");
                }
            }
        }
    }
}
