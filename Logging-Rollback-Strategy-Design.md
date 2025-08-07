# Logging Format Rollback Strategy - Design Options

## 🎯 **Core Requirements**
- **Zero Downtime**: Switch between old/new formats without agent restart
- **Granular Control**: Per-component, per-severity level control
- **Performance**: Minimal overhead when checking flags
- **Safety**: Default to stable/old format if flag service unavailable

---

## 🚀 **Option 1: Simple Global Feature Flag (Recommended for MVP)**

### **Implementation:**
```csharp
// In Tracing.cs
private static bool _useEnhancedLogging = AgentKnobs.EnhancedLogging.GetValue().AsBoolean();

public void Entering([CallerMemberName] string name = "", [CallerFilePath] string filePath = "")
{
    if (_useEnhancedLogging)
    {
        // New enhanced format
        var component = ExtractComponentFromFilePath(filePath);
        var message = BuildStructuredMessage(component, _defaultPhase, _currentCorrelationId, name, $"Entering {name}");
        Trace(TraceEventType.Verbose, message);
    }
    else
    {
        // Original format
        Trace(TraceEventType.Verbose, $"Entering {name}");
    }
}
```

### **Pros:**
- ✅ Simple to implement and understand
- ✅ Single point of control
- ✅ Fast flag lookup (cached)
- ✅ Easy rollback in emergency

### **Cons:**
- ❌ All-or-nothing approach
- ❌ Can't selectively enable for specific components

---

## 🔧 **Option 2: Granular Component-Level Flags**

### **Implementation:**
```csharp
// In AgentKnobs or TracingConfiguration
public static class LoggingFlags
{
    public static bool IsEnhancedLoggingEnabled(string component)
    {
        // Check component-specific flag first
        var componentFlag = AgentKnobs.GetValue($"EnhancedLogging.{component}");
        if (componentFlag != null) return componentFlag.AsBoolean();
        
        // Fallback to global flag
        return AgentKnobs.EnhancedLogging.GetValue().AsBoolean();
    }
}

// In Tracing.cs
public void Entering([CallerMemberName] string name = "", [CallerFilePath] string filePath = "")
{
    var component = ExtractComponentFromFilePath(filePath);
    
    if (LoggingFlags.IsEnhancedLoggingEnabled(component))
    {
        var message = BuildStructuredMessage(component, _defaultPhase, _currentCorrelationId, name, $"Entering {name}");
        Trace(TraceEventType.Verbose, message);
    }
    else
    {
        Trace(TraceEventType.Verbose, $"Entering {name}");
    }
}
```

### **Feature Flags:**
```
EnhancedLogging.Global=false          # Master switch
EnhancedLogging.JobRunner=true        # Enable for JobRunner only
EnhancedLogging.TaskRunner=false      # Keep TaskRunner on old format
EnhancedLogging.StepsRunner=true      # Enable for StepsRunner
```

### **Pros:**
- ✅ Gradual rollout capability
- ✅ Can isolate issues to specific components
- ✅ A/B testing possibilities
- ✅ Safer production deployment

### **Cons:**
- ❌ More complex implementation
- ❌ Multiple flags to manage

---

## ⚡ **Option 3: Performance-Optimized with Caching**

### **Implementation:**
```csharp
public sealed class Tracing : ITraceWriter, IDisposable
{
    private readonly LoggingMode _loggingMode;
    private readonly string _componentName;
    
    // Cached flag value - updated periodically
    private static readonly ConcurrentDictionary<string, bool> _enhancedLoggingCache = 
        new ConcurrentDictionary<string, bool>();
    
    public Tracing(string name, ...)
    {
        _componentName = ExtractComponentFromName(name);
        _loggingMode = GetLoggingMode(_componentName);
        
        // Start cache refresh timer
        StartFlagRefreshTimer();
    }
    
    public void Entering([CallerMemberName] string name = "", [CallerFilePath] string filePath = "")
    {
        switch (_loggingMode)
        {
            case LoggingMode.Enhanced:
                LogEnhanced(TraceEventType.Verbose, name, filePath, $"Entering {name}");
                break;
            case LoggingMode.Legacy:
                Trace(TraceEventType.Verbose, $"Entering {name}");
                break;
            case LoggingMode.Dynamic:
                if (IsEnhancedLoggingCached(_componentName))
                    LogEnhanced(TraceEventType.Verbose, name, filePath, $"Entering {name}");
                else
                    Trace(TraceEventType.Verbose, $"Entering {name}");
                break;
        }
    }
}

public enum LoggingMode
{
    Legacy,     // Always use old format
    Enhanced,   // Always use new format  
    Dynamic     // Check feature flag
}
```

### **Pros:**
- ✅ High performance (cached flags)
- ✅ Flexible deployment modes
- ✅ Can lock mode for critical environments

### **Cons:**
- ❌ Most complex implementation
- ❌ Cache invalidation complexity

---

## 🏗️ **Option 4: Strategy Pattern (Most Flexible)**

### **Implementation:**
```csharp
public interface ILoggingStrategy
{
    void LogMessage(TraceEventType level, string component, string operation, string message, string filePath = "");
}

public class LegacyLoggingStrategy : ILoggingStrategy
{
    public void LogMessage(TraceEventType level, string component, string operation, string message, string filePath = "")
    {
        // Original format: just the message
        _traceSource.TraceEvent(level, 0, _secretMasker.MaskSecrets(message));
    }
}

public class EnhancedLoggingStrategy : ILoggingStrategy
{
    public void LogMessage(TraceEventType level, string component, string operation, string message, string filePath = "")
    {
        // New format: [component] [phase] [correlationid][operation] message
        var enhancedMessage = BuildStructuredMessage(component, _defaultPhase, _currentCorrelationId, operation, message);
        _traceSource.TraceEvent(level, 0, _secretMasker.MaskSecrets(enhancedMessage));
    }
}

public sealed class Tracing : ITraceWriter, IDisposable
{
    private ILoggingStrategy _loggingStrategy;
    
    public void Entering([CallerMemberName] string name = "", [CallerFilePath] string filePath = "")
    {
        var component = ExtractComponentFromFilePath(filePath);
        _loggingStrategy.LogMessage(TraceEventType.Verbose, component, name, $"Entering {name}", filePath);
    }
    
    // Dynamic strategy switching
    public void SwitchLoggingStrategy(ILoggingStrategy strategy)
    {
        _loggingStrategy = strategy;
    }
}
```

### **Pros:**
- ✅ Ultimate flexibility
- ✅ Easy to add new formats in future
- ✅ Clean separation of concerns
- ✅ Runtime strategy switching

### **Cons:**
- ❌ Over-engineering for current needs
- ❌ Additional abstraction overhead

---

## 🎯 **Recommended Approach: Hybrid (Option 1 + 2)**

### **Phase 1: Simple Global Flag (Quick Win)**
```csharp
// Start with simple global flag for initial rollout
private static bool _useEnhancedLogging => 
    AgentKnobs.EnhancedLogging.GetValue().AsBoolean();
```

### **Phase 2: Add Component Granularity (If Needed)**
```csharp
// Enhance with component-level control later
private bool IsEnhancedLoggingEnabled(string component)
{
    // Check specific component flag first
    var componentSetting = AgentKnobs.GetValue($"EnhancedLogging.{component}");
    if (componentSetting?.AsBoolean() == true) return true;
    
    // Check global exclude list
    var excludedComponents = AgentKnobs.EnhancedLogging.ExcludedComponents.GetValue();
    if (excludedComponents?.Contains(component) == true) return false;
    
    // Default to global setting
    return AgentKnobs.EnhancedLogging.GetValue().AsBoolean();
}
```

---

## 🔄 **Feature Flag Configuration Examples**

### **Conservative Rollout:**
```json
{
  "EnhancedLogging.Global": false,
  "EnhancedLogging.JobRunner": true,    // Test with most critical component first
  "EnhancedLogging.ExcludedComponents": ["HostContext", "HttpTrace"]
}
```

### **Full Rollout:**
```json
{
  "EnhancedLogging.Global": true,
  "EnhancedLogging.ExcludedComponents": []  // No exclusions
}
```

### **Emergency Rollback:**
```json
{
  "EnhancedLogging.Global": false  // Single flag flips everything back
}
```

---

## 📊 **Implementation Priorities**

### **Week 1 (MVP):**
- ✅ Implement simple global flag
- ✅ Add flag check to `Entering()`/`Leaving()` methods
- ✅ Test rollback scenarios

### **Week 2 (Enhanced):**
- ✅ Add component-level granularity
- ✅ Implement excluded components list
- ✅ Performance optimization with caching

### **Future (If Needed):**
- Strategy pattern for multiple formats
- A/B testing capabilities
- Automatic rollback on error rates

---

## 🚨 **Safety Considerations**

1. **Default to Safe**: If flag service is unavailable, default to legacy format
2. **Performance**: Cache flag values, refresh periodically (every 30s)
3. **Monitoring**: Log flag changes for audit trail
4. **Testing**: Automated tests for both flag states
5. **Documentation**: Clear runbook for emergency rollback

**What's your preference? I'd recommend starting with the simple global flag approach for quick implementation, then adding granularity if needed!**
