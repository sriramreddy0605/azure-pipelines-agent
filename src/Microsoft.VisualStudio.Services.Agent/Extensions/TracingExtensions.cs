// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.Agent.Extensions
{
    /// <summary>
    /// Extension methods for enhanced logging that provide consistent patterns
    /// for both decorator-based logging and format migration work
    /// </summary>
    public static class TracingExtensions
    {
        /// <summary>
        /// Creates a method scope for automatic entry/exit logging with duration tracking
        /// Used by Rishabh's decorator implementation
        /// </summary>
        public static IDisposable LogMethodScope(this Tracing trace, 
            string component = null, 
            [CallerMemberName] string methodName = "", 
            object context = null)
        {
            return trace.MethodScope(component, methodName, context);
        }

        /// <summary>
        /// Logs structured messages with consistent format
        /// Used by Sanju's format migration work
        /// </summary>
        public static void LogStructured(this Tracing trace, 
            LogLevel level, 
            string component, 
            string operation, 
            string message, 
            object metadata = null)
        {
            var correlationContext = metadata != null ? 
                new { Message = message, Metadata = metadata } : 
                new { Message = message };

            switch (level)
            {
                case LogLevel.Info:
                    trace.LogComponentProgress(component, operation, message, metadata);
                    break;
                case LogLevel.Error:
                    trace.Error($"[{component}] {operation}: {message}");
                    if (metadata != null)
                    {
                        var metadataJson = JsonConvert.SerializeObject(metadata, Formatting.Indented);
                        trace.Verbose($"[{component}] {operation} metadata: {metadataJson}");
                    }
                    break;
                case LogLevel.Warning:
                    trace.Warning($"[{component}] {operation}: {message}");
                    if (metadata != null)
                    {
                        var metadataJson = JsonConvert.SerializeObject(metadata, Formatting.Indented);
                        trace.Verbose($"[{component}] {operation} metadata: {metadataJson}");
                    }
                    break;
                case LogLevel.Verbose:
                    trace.Verbose($"[{component}] {operation}: {message}");
                    break;
            }
        }

        /// <summary>
        /// Standardized exception logging for both workstreams
        /// </summary>
        public static void LogException(this Tracing trace, 
            string component, 
            string operation, 
            Exception ex, 
            object context = null)
        {
            trace.LogError(component, operation, ex, context);
        }

        /// <summary>
        /// Logs task input parameters (P1 requirement for Rishabh)
        /// Can be used by Sanju during format migration
        /// </summary>
        public static void LogTaskInputs(this Tracing trace,
            string taskName,
            System.Collections.Generic.Dictionary<string, string> inputs,
            bool isDebugEnabled = false)
        {
            if (isDebugEnabled && inputs != null)
            {
                var sanitizedInputs = new System.Collections.Generic.Dictionary<string, string>();
                foreach (var input in inputs)
                {
                    // Basic secret detection - extend as needed
                    var isSensitive = input.Key.ToLowerInvariant().Contains("password") ||
                                    input.Key.ToLowerInvariant().Contains("secret") ||
                                    input.Key.ToLowerInvariant().Contains("token");
                    
                    sanitizedInputs[input.Key] = isSensitive ? "***" : input.Value;
                }

                trace.LogStructured(LogLevel.Verbose, "TaskRunner", "InputLogging", 
                    $"Task {taskName} inputs", sanitizedInputs);
            }
        }

        /// <summary>
        /// Helper for phase/component transitions (used by both workstreams)
        /// </summary>
        public static void LogPhaseTransition(this Tracing trace,
            string fromPhase,
            string toPhase,
            string correlationId = null,
            object transitionContext = null)
        {
            var message = $"Transitioning: {fromPhase} → {toPhase}";
            trace.LogStructured(LogLevel.Info, "Agent", "PhaseTransition", message, transitionContext);
        }
    }

    /// <summary>
    /// Log levels for structured logging
    /// </summary>
    public enum LogLevel
    {
        Verbose,
        Info,
        Warning,
        Error
    }
}
