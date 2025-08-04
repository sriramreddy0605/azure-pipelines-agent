// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.VisualStudio.Services.Agent.Constants
{
    /// <summary>
    /// Shared constants for enhanced logging implementation
    /// Used by both decorator and format migration work
    /// </summary>
    public static class LoggingConstants
    {
        /// <summary>
        /// Component names for structured logging
        /// </summary>
        public static class Components
        {
            public const string Agent = "Agent";
            public const string Listener = "Listener";
            public const string Worker = "Worker";
            public const string JobRunner = "JobRunner";
            public const string StepsRunner = "StepsRunner";
            public const string TaskRunner = "TaskRunner";
            public const string JobDispatcher = "JobDispatcher";
            public const string MessageListener = "MessageListener";
            public const string WorkerDispatcher = "WorkerDispatcher";
            public const string HandlerFactory = "HandlerFactory";
            public const string TaskManager = "TaskManager";
            public const string ExecutionContext = "ExecutionContext";
            public const string JobServerQueue = "JobServerQueue";
            public const string ResourceManager = "ResourceManager";
            public const string DiagnosticManager = "DiagnosticManager";
        }

        /// <summary>
        /// Operation names for consistent logging
        /// </summary>
        public static class Operations
        {
            // Lifecycle operations
            public const string Initialize = "Initialize";
            public const string Start = "Start";
            public const string Execute = "Execute";
            public const string Complete = "Complete";
            public const string Cleanup = "Cleanup";
            public const string Shutdown = "Shutdown";
            public const string Cancel = "Cancel";

            // Agent-specific operations
            public const string AgentStartup = "AgentStartup";
            public const string SessionCreate = "SessionCreate";
            public const string MessageReceive = "MessageReceive";
            public const string JobDispatch = "JobDispatch";
            public const string WorkerCreate = "WorkerCreate";
            public const string WorkerTerminate = "WorkerTerminate";

            // Job execution operations
            public const string JobInitialize = "JobInitialize";
            public const string JobExecute = "JobExecute";
            public const string JobFinalize = "JobFinalize";
            public const string StepExecute = "StepExecute";
            public const string TaskExecute = "TaskExecute";
            public const string TaskInputLoad = "TaskInputLoad";
            public const string TaskHandlerSelect = "TaskHandlerSelect";

            // Error/Exception operations
            public const string ExceptionHandle = "ExceptionHandle";
            public const string ErrorRecover = "ErrorRecover";
            public const string TimeoutHandle = "TimeoutHandle";

            // Communication operations
            public const string IPCCommunication = "IPCCommunication";
            public const string ServerCommunication = "ServerCommunication";
            public const string HttpRequest = "HttpRequest";
            public const string FileUpload = "FileUpload";
        }

        /// <summary>
        /// Phase names for agent lifecycle tracking
        /// </summary>
        public static class Phases
        {
            public const string AgentStartup = "AgentStartup";
            public const string ListenerStartup = "ListenerStartup";
            public const string SessionEstablishment = "SessionEstablishment";
            public const string MessageListening = "MessageListening";
            public const string JobReceived = "JobReceived";
            public const string WorkerSpawn = "WorkerSpawn";
            public const string JobInitialization = "JobInitialization";
            public const string StepsExecution = "StepsExecution";
            public const string JobFinalization = "JobFinalization";
            public const string WorkerTermination = "WorkerTermination";
            public const string AgentShutdown = "AgentShutdown";
        }

        /// <summary>
        /// Environment variables for logging configuration
        /// </summary>
        public static class EnvironmentVariables
        {
            public const string AgentTrace = "VSTS_AGENT_TRACE";
            public const string AgentDebug = "ADO_AGENT_DEBUG";
            public const string AgentCorrelationId = "VSTS_AGENT_CORRELATION_ID";
            public const string AgentLogTaskInputs = "AGENT_LOG_TASK_INPUT_PARAMETERS";
            public const string AgentEnhancedLogging = "AGENT_ENHANCED_LOGGING";
        }

        /// <summary>
        /// Metadata keys for structured logging
        /// </summary>
        public static class MetadataKeys
        {
            public const string CorrelationId = "CorrelationId";
            public const string JobId = "JobId";
            public const string TaskId = "TaskId";
            public const string StepId = "StepId";
            public const string WorkerId = "WorkerId";
            public const string Duration = "Duration";
            public const string Result = "Result";
            public const string ErrorCode = "ErrorCode";
            public const string RetryCount = "RetryCount";
            public const string TaskName = "TaskName";
            public const string HandlerType = "HandlerType";
            public const string InputCount = "InputCount";
            public const string FileSize = "FileSize";
            public const string ExitCode = "ExitCode";
        }

        /// <summary>
        /// Correlation ID formats
        /// </summary>
        public static class CorrelationFormats
        {
            public const string Job = "JOB-{0}";
            public const string Worker = "WKR-{0}";
            public const string Task = "TSK-{0}";
            public const string Step = "STP-{0}";
            public const string Agent = "AGT-{0}";
            public const string Session = "SES-{0}";
            
            public static string CreateJobCorrelation(string jobId, string workerId = null)
            {
                return workerId != null ? 
                    $"JOB-{jobId}|WKR-{workerId}" : 
                    $"JOB-{jobId}";
            }
            
            public static string CreateTaskCorrelation(string jobId, string taskId)
            {
                return $"JOB-{jobId}|TSK-{taskId}";
            }
        }
    }
}
