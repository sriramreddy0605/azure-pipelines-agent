using Moq;
using System.Collections.Generic;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Tests;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Handlers;
using Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Test.L0.Worker.Handlers;

public class HandlersOutputDataL0
{
    [Fact]
    public void Test()
    {
        using var hc = new TestHostContext(this);
        hc.SetSingleton(new WorkerCommandManager() as IWorkerCommandManager);
        hc.SetSingleton(new ExtensionManager() as IExtensionManager);

        var ec = GetFakeExecContext(hc);

        var handler = new NodeHandler();
        handler.Initialize(hc);

        var sh = new DefaultStepHost();
        sh.Initialize(hc);

        var outputLines = new List<string>();

        ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Callback<string, string, bool>((_, msg, _) =>
        {
            outputLines.Add(msg);
        });

        handler.Endpoints = new();
        handler.Task = new TaskStepDefinitionReference();
        handler.Environment = new();
        handler.RuntimeVariables = ec.Object.Variables;
        handler.ExecutionContext = ec.Object;
        handler.StepHost = sh;
        handler.Inputs = new();
        handler.SecureFiles = new();
        handler.TaskDirectory = "";

        var customLines = new List<string>
        {
            "\u001b[31;1mThis is \u001b[36;1mCustom line \u001b[0mOf text\u001b[0m"
        };

        var expectedLines = new List<string>
        {
            "This is Custom line Of text"
        };

        // using var sr = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(string.Join("\n", customLines))));

        // var proc = new Mock<Process>();
        // proc.Setup(x => x.StandardOutput).Returns(sr);

        // var pi = GetFakeProcessInvoker();

        var pi = new Mock<IProcessInvoker>();
        // pi.Initialize(hc);

        var outputEvent = pi.GetType().GetEvent("OutputDataReceived");
        var method = outputEvent.GetRaiseMethod();
        if (method != null)
        {
            foreach (var line in customLines)
            {
                method.Invoke(pi, new string[] { line });
            }
        }

        Assert.Equal(expectedLines, outputLines);
    }

    private Mock<IExecutionContext> GetFakeExecContext(IHostContext hc)
    {
        var variables = new Dictionary<string, VariableValue>()
        {
            ["AZP_AGENT_NO_COLOR_LOGS"] = new("true")
        };

        var fakeContext = new Mock<IExecutionContext>();
        fakeContext.SetupAllProperties();

        fakeContext.Setup(x => x.Variables).Returns(new Variables(hc, variables, out _));


        return fakeContext;
    }

    private Mock<IProcessInvoker> GetFakeProcessInvoker()
    {
        var fakePI = new Mock<IProcessInvoker>();
        fakePI.SetupAllProperties();

        return fakePI;
    }
}
