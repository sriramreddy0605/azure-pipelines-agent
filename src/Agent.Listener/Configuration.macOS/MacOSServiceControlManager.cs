// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Services.Agent.Util;
using System.Runtime.Versioning;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    [ServiceLocator(Default = typeof(MacOSServiceControlManager))]
    [SupportedOSPlatform("macos")]
    public interface IMacOSServiceControlManager : IAgentService
    {
        void GenerateScripts(AgentSettings settings);
    }

    [SupportedOSPlatform("macos")]
    public class MacOSServiceControlManager : ServiceControlManager, IMacOSServiceControlManager
    {
        // This is the name you would see when you do `systemctl list-units | grep vsts`
        private const string _svcNamePattern = "vsts.agent.{0}.{1}.{2}";
        private const string _svcDisplayPattern = "Azure Pipelines Agent ({0}.{1}.{2})";
        private const string _shTemplate = "darwin.svc.sh.template";
        private const string _svcShName = "svc.sh";

        public void GenerateScripts(AgentSettings settings)
        {
            Trace.Entering();

            string serviceName;
            string serviceDisplayName;
            CalculateServiceName(settings, _svcNamePattern, _svcDisplayPattern, out serviceName, out serviceDisplayName);

            try
            {
                string svcShPath = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), _svcShName);

                // TODO: encoding?
                // TODO: Loc strings formatted into MSG_xxx vars in shellscript
                string svcShContent = File.ReadAllText(Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Bin), _shTemplate));
                var tokensToReplace = new Dictionary<string, string>
                                          {
                                              { "{{SvcDescription}}", serviceDisplayName },
                                              { "{{SvcNameVar}}", serviceName }
                                          };

                svcShContent = tokensToReplace.Aggregate(
                    svcShContent,
                    (current, item) => current.Replace(item.Key, item.Value));

                //TODO: encoding?
                File.WriteAllText(svcShPath, svcShContent);

                var unixUtil = HostContext.CreateService<IUnixUtil>();
                unixUtil.ChmodAsync("755", svcShPath).GetAwaiter().GetResult();
            }
            catch (FileNotFoundException fnfEx)
            {
                Trace.Error($"Service template file not found: {fnfEx.Message}");
                throw new InvalidOperationException($"Cannot find service template file: {fnfEx.Message}", fnfEx);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                Trace.Error($"Access denied creating service file: {uaEx.Message}");
                throw new InvalidOperationException($"Access denied writing service file. Run with appropriate permissions: {uaEx.Message}", uaEx);
            }
            catch (IOException ioEx)
            {
                Trace.Error($"I/O error creating service file: {ioEx.Message}");
                throw new InvalidOperationException($"Failed to write service file: {ioEx.Message}", ioEx);
            }
            catch (Exception e)
            {
                Trace.Error(e);
                throw;
            }
        }
    }
}
