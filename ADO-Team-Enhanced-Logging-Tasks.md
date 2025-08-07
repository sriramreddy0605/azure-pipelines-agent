# Azure DevOps Agent Enhanced Logging Implementation

## Epic Overview
Implement comprehensive structured logging across the Azure DevOps Agent codebase to improve debugging, monitoring, and operational visibility.

## Enhanced Log Format
```
[COMPONENT] [PHASE] [CORRELATIONID][OPERATION] message (duration: XXXms) [metadata]
```

## Team Task Distribution

### 🎯 **Rishabh's Tasks**

#### **Task 1: Method Lifecycle Decorators** 
- **Priority**: High
- **Estimate**: 3-5 days
- **Description**: Create and implement decorators for logging method start/end/duration on all main methods
- **Acceptance Criteria**:
  - [ ] Create method decorators that automatically log method entry/exit
  - [ ] Include duration tracking for performance monitoring
  - [ ] Apply to all main execution methods (RunJobAsync, ExecuteStepAsync, etc.)
  - [ ] Ensure minimal performance impact
- **Implementation Notes**:
  - Use `[CallerMemberName]` and `[CallerFilePath]` attributes
  - Build on current `Entering()`/`Leaving()` enhancement pattern
  - Include automatic component detection

#### **Task 2: Task Lifecycle Logging**
- **Priority**: High  
- **Estimate**: 2-3 days
- **Description**: Implement comprehensive logging for steps runner including input parameter logging
- **Acceptance Criteria**:
  - [ ] Log task initialization with input parameters
  - [ ] Track task execution phases (preparation, execution, cleanup)
  - [ ] Include task metadata (task name, version, timeout settings)
  - [ ] Log task completion status and results
- **Files to Update**:
  - `StepsRunner.cs`
  - `TaskRunner.cs` 
  - Related task execution components

#### **Task 3: Agent Lifecycle Logging**
- **Priority**: Medium
- **Estimate**: 2-3 days  
- **Description**: Implement logging for agent initialization, execution phases, and cleanup
- **Acceptance Criteria**:
  - [ ] Log agent startup and initialization phases
  - [ ] Track job assignment and processing lifecycle
  - [ ] Monitor agent health and resource usage
  - [ ] Log clean shutdown and error scenarios
- **Files to Update**:
  - `JobDispatcher.cs`
  - `MessageListener.cs`
  - `AgentService.cs` base classes

#### **Task 4: Debug Flags Audit**
- **Priority**: Medium
- **Estimate**: 1-2 days
- **Description**: Review and update all debugging flags, ensure agent diagnostics have basic info logs without requiring feature flags
- **Acceptance Criteria**:
  - [ ] Audit existing debug flags and trace settings
  - [ ] Ensure essential diagnostic info is logged by default
  - [ ] Update flag documentation and usage patterns
  - [ ] Validate agent diagnostics output quality

---

### 🎯 **Sanju's Tasks**

#### **Task 5: Log Format Migration**
- **Priority**: High
- **Estimate**: 5-7 days
- **Description**: Convert all existing logging statements across the codebase to the new format
- **Acceptance Criteria**:
  - [ ] Migrate all `Trace.Info()`, `Trace.Error()`, `Trace.Warning()` calls
  - [ ] Ensure consistent structured format usage
  - [ ] Maintain backward compatibility during transition
  - [ ] Update test assertions for new log format
- **Implementation Strategy**:
  - Start with high-traffic components (JobRunner, TaskRunner)
  - Use search/replace patterns for common logging calls
  - Validate output with test scenarios

#### **Task 6: Exception Handling Updates**
- **Priority**: High
- **Estimate**: 3-4 days
- **Description**: Update all exception handling blocks to use the new logging format
- **Acceptance Criteria**:
  - [ ] Standardize exception logging with structured format
  - [ ] Include exception metadata (type, inner exceptions, stack trace summary)
  - [ ] Ensure correlation IDs are preserved in error scenarios
  - [ ] Add operation context to exception logs

#### **Task 7: Job Finalization Logging**
- **Priority**: Medium
- **Estimate**: 2-3 days
- **Description**: Implement logging for job completion and cancellation scenarios
- **Acceptance Criteria**:
  - [ ] Log job completion with final status and summary
  - [ ] Track job cancellation events and cleanup
  - [ ] Include job-level performance metrics
  - [ ] Handle timeout and error termination scenarios

#### **Task 8: HTTP Trace Enhancement**
- **Priority**: Low
- **Estimate**: 2-3 days
- **Description**: Investigate and implement job-level HTTP tracing capabilities
- **Acceptance Criteria**:
  - [ ] Research current HTTP tracing implementation
  - [ ] Design job-level correlation for HTTP requests
  - [ ] Implement HTTP trace correlation with job context
  - [ ] Validate with Azure DevOps service calls

---

## 🔄 **Shared Infrastructure (Foundation Work)**

### **Common Functions Update** - *Rishabh (Current Focus)*
- **Description**: Update common tracing functions to support new structured format
- **Status**: ✅ In Progress
- **Current Work**:
  - Enhanced `Entering()`/`Leaving()` methods with automatic component detection
  - Correlation ID parameter support for consumption by team functions
  - Backward compatibility maintained

---

## 📋 **Dependencies & Coordination**

### **Rishabh → Sanju Dependencies**:
- Correlation ID creation patterns (Rishabh provides, Sanju consumes)
- Enhanced tracing method signatures (Rishabh defines, Sanju uses)

### **Sanju → Rishabh Dependencies**:
- Log format migration feedback (impacts decorator implementation)
- Exception handling patterns (informs lifecycle logging design)

---

## 🧪 **Testing Strategy**

### **Unit Testing**:
- [ ] Test enhanced logging methods with various input scenarios
- [ ] Validate component detection from file paths
- [ ] Ensure correlation ID propagation

### **Integration Testing**:
- [ ] End-to-end job execution with full logging enabled
- [ ] Performance impact assessment
- [ ] Log output format validation

### **Performance Testing**:
- [ ] Measure logging overhead on job execution time
- [ ] Validate memory usage with enhanced logging
- [ ] Test with high-frequency logging scenarios

---

## 📊 **Success Metrics**

- **Code Coverage**: All main execution paths have structured logging
- **Performance Impact**: <5% overhead on job execution time
- **Log Quality**: Consistent format across all components
- **Debugging Improvement**: Faster issue resolution with enhanced context

---

## 🚀 **Rollout Plan**

### **Phase 1** (Week 1-2):
- Complete common functions infrastructure (Rishabh)
- Begin high-priority log format migration (Sanju)

### **Phase 2** (Week 3-4):
- Implement method decorators (Rishabh)
- Continue exception handling updates (Sanju)

### **Phase 3** (Week 5-6):
- Complete lifecycle logging (Both)
- HTTP trace enhancement (Sanju)
- Debug flags audit (Rishabh)

### **Phase 4** (Week 7):
- Final testing and validation
- Documentation updates
- Production rollout preparation
