// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Agent.Sdk.SecretMasking;

namespace Microsoft.VisualStudio.Services.Agent
{
    public sealed class EnhancedTracing : Tracing
    {
        public EnhancedTracing(string name, ILoggedSecretMasker secretMasker, SourceSwitch sourceSwitch, HostTraceListener traceListener)
            : base(name, secretMasker, sourceSwitch, traceListener)
        {
        }

        // Override ALL base methods to ensure enhanced logging for any call signature
        public override void Info(string message, [CallerMemberName] string operation = "")
        {
            LogWithOperation(TraceEventType.Information, message, operation);
        }

        public override void Info(string format, [CallerMemberName] string operation = "", params object[] args)
        {
            LogWithOperation(TraceEventType.Information, StringUtil.Format(format, args), operation);
        }

        public override void Info(object item, [CallerMemberName] string operation = "")
        {
            LogWithOperation(TraceEventType.Information, item?.ToString() ?? "null", operation);
        }

        // Override ALL Error methods to ensure enhanced logging
        public override void Error(Exception exception, [CallerMemberName] string operation = "")
        {
            ArgUtil.NotNull(exception, nameof(exception));
            LogWithOperation(TraceEventType.Error, exception.ToString(), operation);
        }

        public override void Error(string message, [CallerMemberName] string operation = "")
        {
            LogWithOperation(TraceEventType.Error, message, operation);
        }

        public override void Error(string format, [CallerMemberName] string operation = "", params object[] args)
        {
            LogWithOperation(TraceEventType.Error, StringUtil.Format(format, args), operation);
        }

        // Override ALL Warning methods to ensure enhanced logging
        public override void Warning(string message, [CallerMemberName] string operation = "")
        {
            LogWithOperation(TraceEventType.Warning, message, operation);
        }

        public override void Warning(string format, [CallerMemberName] string operation = "", params object[] args)
        {
            LogWithOperation(TraceEventType.Warning, StringUtil.Format(format, args), operation);
        }

        // Override ALL Verbose methods to ensure enhanced logging
        public override void Verbose(string message, [CallerMemberName] string operation = "")
        {
            LogWithOperation(TraceEventType.Verbose, message, operation);
        }

        public override void Verbose(string format, [CallerMemberName] string operation = "", params object[] args)
        {
            LogWithOperation(TraceEventType.Verbose, StringUtil.Format(format, args), operation);
        }

        public override void Verbose(object item, [CallerMemberName] string operation = "")
        {
            LogWithOperation(TraceEventType.Verbose, item?.ToString() ?? "null", operation);
        }

        public override void Entering([CallerMemberName] string name = "")
        {
            LogWithOperation(TraceEventType.Verbose, $"Entering --- {name}", name);
        }

        // public override IDisposable Entering([CallerMemberName] string name = "")
        // {
        //     LogWithOperation(TraceEventType.Verbose, $"Entering --- {name}", name);
        //     return new MethodTimer(this, name);
        // }

        /// <summary>
        /// Creates a disposable duration tracker that logs entering and leaving with duration.
        /// Usage: using (trace.EnteringWithDuration()) { /* method logic */ }
        /// This provides automatic duration tracking with exception-safe cleanup.
        /// </summary>
        public override IDisposable EnteringWithDuration([CallerMemberName] string name = "")
        {
            LogWithOperation(TraceEventType.Verbose, $"Entering --- {name}", name);
            return new MethodTimer(this, name);
        }

        public override void Leaving([CallerMemberName] string name = "")
        {
            LogWithOperation(TraceEventType.Verbose, $"Leaving --- {name}", name);
        }

        /// <summary>
        /// Internal method to log leaving with duration - called by MethodTimer.
        /// </summary>
        internal void LogLeavingWithDuration(string methodName, TimeSpan duration)
        {
            var message = $"Leaving --- {methodName} (Duration: {duration.TotalMilliseconds:F2}ms)";
            LogWithOperation(TraceEventType.Verbose, message, methodName);
        }

        private void LogWithOperation(TraceEventType eventType, string message, string operation)
        {
            var enhancedMessage = FormatEnhancedLogMessage(message, operation);
            base.Trace(eventType, enhancedMessage);
        }

        private string FormatEnhancedLogMessage(string message, string operation)
        {
            var operationPart = !string.IsNullOrEmpty(operation) ? $"[{operation}]" : "";
            return $"{operationPart} {message}".TrimEnd();
        }

        /// <summary>
        /// Private class that implements automatic duration tracking using the disposable pattern.
        /// This ensures guaranteed cleanup and duration logging regardless of how the method exits.
        /// </summary>
        private sealed class MethodTimer : IDisposable
        {
            private readonly EnhancedTracing _tracing;
            private readonly string _methodName;
            private readonly Stopwatch _stopwatch;
            private bool _disposed = false;

            public MethodTimer(EnhancedTracing tracing, string methodName)
            {
                _tracing = tracing;
                _methodName = methodName;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _tracing.Info($"In Method Timer Dispose function - disposed = {_disposed}");
                if (!_disposed)
                {
                    _disposed = true;
                    _stopwatch.Stop();
                    _tracing.LogLeavingWithDuration(_methodName, _stopwatch.Elapsed);
                }
            }
        }
    }
}
