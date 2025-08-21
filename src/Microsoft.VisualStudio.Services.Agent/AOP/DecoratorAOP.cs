using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.VisualStudio.Services.Agent.Util;
using Agent.Sdk;

namespace Microsoft.VisualStudio.Services.Agent.AOP
{
    /// <summary>
    /// True AOP attribute - just decorate your method and it automatically gets entry/exit logging.
    /// Works like Python decorators - no code changes needed in method body.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class LogMethodAttribute : Attribute
    {
        public string OperationName { get; private set; }
        public bool LogParameters { get; set; } = false;
        public bool LogReturnValue { get; set; } = false;
        public LogLevel LogLevel { get; set; } = LogLevel.Info;
        public int MinDurationMs { get; set; } = 0;

        public LogMethodAttribute() { }
        public LogMethodAttribute(string operationName) { OperationName = operationName; }
    }

    /// <summary>
    /// Interceptor that automatically adds entry/exit logging to decorated methods.
    /// This is the "magic" that makes AOP work without modifying method bodies.
    /// </summary>
    public class MethodLoggingInterceptor : IInterceptor
    {
        private readonly ITraceWriter _traceWriter;
        private static bool _isEnabled = true;

        public MethodLoggingInterceptor(ITraceWriter traceWriter)
        {
            _traceWriter = traceWriter ?? throw new ArgumentNullException(nameof(traceWriter));
        }

        public static bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public void Intercept(IInvocation invocation)
        {
            LogMessage($"[Intercept] {invocation.Method.Name}", LogLevel.Verbose);
            if (!_isEnabled)
            {
                invocation.Proceed();
                return;
            }
            LogMessage($"[After Intercept] {invocation.Method.Name}", LogLevel.Verbose);
            var method = invocation.Method;
            
            // For interface proxies, check the target implementation for attributes
            var logAttribute = method.GetCustomAttribute<LogMethodAttribute>();
            if (logAttribute == null && invocation.InvocationTarget != null)
            {
                // Look for the method on the target implementation class
                var targetType = invocation.InvocationTarget.GetType();
                var targetMethod = targetType.GetMethod(method.Name, method.GetParameters().Select(p => p.ParameterType).ToArray());
                logAttribute = targetMethod?.GetCustomAttribute<LogMethodAttribute>() 
                              ?? targetType.GetCustomAttribute<LogMethodAttribute>();
            }
            
            LogMessage($"[LogAttribute] {logAttribute?.OperationName}", LogLevel.Verbose);
            if (logAttribute == null)
            {
                invocation.Proceed();
                return;
            }
            LogMessage($"[LogAttribute After IF] {logAttribute?.OperationName}", LogLevel.Verbose);
            var operationName = logAttribute.OperationName ?? $"{method.DeclaringType?.Name}.{method.Name}";
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Log method entry
            LogMessage($"[ENTRY] {operationName}", logAttribute.LogLevel);

            try
            {
                invocation.Proceed();

                // Handle async methods
                if (invocation.ReturnValue is Task task)
                {
                    invocation.ReturnValue = HandleAsync(task, operationName, logAttribute, stopwatch);
                }
                else
                {
                    // Synchronous method - log exit immediately
                    LogExit(operationName, logAttribute, stopwatch);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogMessage($"[EXIT] {operationName} (FAILED after {stopwatch.ElapsedMilliseconds}ms): {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private async Task HandleAsync(Task task, string operationName, LogMethodAttribute logAttribute, System.Diagnostics.Stopwatch stopwatch)
        {
            LogMessage($"[Before Await] {operationName}", LogLevel.Verbose);
            try
            {
                await task;
                LogExit(operationName, logAttribute, stopwatch);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogMessage($"[EXIT] {operationName} (FAILED after {stopwatch.ElapsedMilliseconds}ms): {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private void LogExit(string operationName, LogMethodAttribute logAttribute, System.Diagnostics.Stopwatch stopwatch)
        {
            stopwatch.Stop();
            var durationMs = stopwatch.ElapsedMilliseconds;

            var message = $"[EXIT] {operationName} (duration: {durationMs}ms)";
                
            // Auto-escalate log level for long operations
            var effectiveLogLevel = logAttribute.LogLevel;
            if (durationMs > 1000000) effectiveLogLevel = LogLevel.Error;
            else if (durationMs > 500000) effectiveLogLevel = LogLevel.Warning;
            else if (durationMs > 100000) effectiveLogLevel = LogLevel.Info;

            LogMessage(message, effectiveLogLevel);
        }

        private void LogMessage(string message, LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Verbose: _traceWriter.Verbose(message); break;
                case LogLevel.Info: _traceWriter.Info(message); break;
                case LogLevel.Warning: _traceWriter.Info($"WARNING: {message}"); break;
                case LogLevel.Error: _traceWriter.Info($"ERROR: {message}"); break;
            }
        }
    }

    /// <summary>
    /// Factory for creating AOP-enabled proxies.
    /// This is what makes the "decorator magic" work.
    /// </summary>
    public static class AOPProxyFactory
    {
        private static readonly ProxyGenerator _proxyGenerator = new ProxyGenerator();
        private static ITraceWriter _defaultTraceWriter;

        public static void SetDefaultTraceWriter(ITraceWriter traceWriter)
        {
            _defaultTraceWriter = traceWriter;
        }

        /// <summary>
        /// Create a proxy that automatically intercepts methods decorated with [LogMethod].
        /// This is the key to decorator-style AOP.
        /// </summary>
        public static T CreateProxy<T>(T target, ITraceWriter traceWriter = null) where T : class
        {
            var interceptor = new MethodLoggingInterceptor(traceWriter ?? _defaultTraceWriter);
            
            // For interfaces, we need to look at the concrete target class for attributes
            return _proxyGenerator.CreateInterfaceProxyWithTarget<T>(target, interceptor);
        }

        /// <summary>
        /// Create a class-based proxy for concrete types with [LogMethod] attributes
        /// </summary>
        public static T CreateClassProxy<T>(T target, ITraceWriter traceWriter = null) where T : class
        {
            var interceptor = new MethodLoggingInterceptor(traceWriter ?? _defaultTraceWriter);
            return _proxyGenerator.CreateClassProxyWithTarget<T>(target, interceptor);
        }

        /// <summary>
        /// Initialize AOP system
        /// </summary>
        public static void Initialize(ITraceWriter traceWriter)
        {
            SetDefaultTraceWriter(traceWriter);
            
            //var enabled = Environment.GetEnvironmentVariable("AGENT_ENABLE_AOP_LOGGING") || "true";
            MethodLoggingInterceptor.IsEnabled = true;

            if (MethodLoggingInterceptor.IsEnabled)
            {
                traceWriter?.Info("✓ AOP Method logging enabled (decorator-style)");
            }
        }
    }

    public enum LogLevel
    {
        Verbose,
        Info,
        Warning,
        Error
    }
}
