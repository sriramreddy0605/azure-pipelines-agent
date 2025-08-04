# Azure DevOps Agent Lifecycle Wiki

Welcome to the Azure DevOps Agent Lifecycle Wiki! This resource provides comprehensive documentation for understanding how the Azure DevOps Agent works internally.

## 🎯 Quick Navigation

### 📖 Getting Started
- **[Agent Overview](./Agent-Overview.md)** - High-level architecture and concepts
- **[Quick Start Guide](./Quick-Start-Guide.md)** - Essential information for new team members
- **[Glossary](./Glossary.md)** - Key terms and definitions

### 🔄 Core Processes
- **[Agent Lifecycle Flow](./Agent-Lifecycle-Flow.md)** - Complete execution flow from start to finish
- **[Process Architecture](./Process-Architecture.md)** - Multi-process design and communication
- **[Job Execution Pipeline](./Job-Execution-Pipeline.md)** - How jobs are processed step-by-step

### 🛠️ Technical Deep Dive
- **[Component Reference](./Component-Reference.md)** - Detailed component documentation
- **[Communication Patterns](./Communication-Patterns.md)** - IPC, message handling, and protocols
- **[Security Implementation](./Security-Implementation.md)** - Secret masking, certificates, and secure execution
- **[Error Handling](./Error-Handling.md)** - Exception management and recovery mechanisms

### 📊 Operations & Monitoring
- **[Logging Architecture](./Logging-Architecture.md)** - Current logging system and best practices
- **[Performance Monitoring](./Performance-Monitoring.md)** - Metrics, counters, and optimization
- **[Troubleshooting Guide](./Troubleshooting-Guide.md)** - Common issues and debugging techniques

### 🔧 Development & Maintenance
- **[Development Guidelines](./Development-Guidelines.md)** - Best practices for agent development
- **[Testing Strategies](./Testing-Strategies.md)** - Unit, integration, and system testing
- **[Configuration Management](./Configuration-Management.md)** - Settings, knobs, and environment variables

## 🏗️ Architecture at a Glance

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Azure DevOps  │────│  Agent.Listener │────│  Agent.Worker   │
│     Service     │    │   (Persistent)  │    │   (Per Job)     │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                              │                         │
                              ▼                         ▼
                       ┌─────────────┐         ┌─────────────┐
                       │JobDispatcher│         │ JobRunner   │
                       └─────────────┘         └─────────────┘
                                                       │
                                                       ▼
                                               ┌─────────────┐
                                               │StepsRunner  │
                                               └─────────────┘
                                                       │
                                                       ▼
                                               ┌─────────────┐
                                               │TaskRunner(s)│
                                               └─────────────┘
```

## 📋 Recent Updates

| Date | Update | Description |
|------|---------|-------------|
| 2024-07-29 | Initial Wiki | Created comprehensive wiki documentation |
| 2024-07-29 | Lifecycle Analysis | Added detailed process flow documentation |
| 2024-07-29 | Architecture Review | Updated component relationships and diagrams |

## 🤝 Contributing

This wiki is maintained by the Azure DevOps Agent team. To contribute:

1. **Update Documentation**: Keep content current with code changes
2. **Add Examples**: Include real-world scenarios and code snippets
3. **Improve Clarity**: Enhance explanations based on team feedback
4. **Report Issues**: Use GitHub issues for documentation bugs

## 📞 Getting Help

- **Team Questions**: Use Teams channel `#agent-development`
- **Technical Issues**: Create GitHub issues with `documentation` label
- **Architecture Questions**: Contact the platform architecture team
- **Urgent Issues**: Follow on-call escalation procedures

## 🎓 Learning Path

### For New Team Members
1. Start with [Agent Overview](./Agent-Overview.md)
2. Read [Quick Start Guide](./Quick-Start-Guide.md)
3. Review [Agent Lifecycle Flow](./Agent-Lifecycle-Flow.md)
4. Explore [Component Reference](./Component-Reference.md)

### For Experienced Developers
1. Review [Process Architecture](./Process-Architecture.md)
2. Study [Security Implementation](./Security-Implementation.md)
3. Understand [Performance Monitoring](./Performance-Monitoring.md)
4. Follow [Development Guidelines](./Development-Guidelines.md)

### For Operations/SRE
1. Read [Troubleshooting Guide](./Troubleshooting-Guide.md)
2. Study [Logging Architecture](./Logging-Architecture.md)
3. Review [Performance Monitoring](./Performance-Monitoring.md)
4. Understand [Configuration Management](./Configuration-Management.md)

---

**Last Updated**: July 29, 2024  
**Version**: 1.0  
**Maintainer**: Azure DevOps Agent Team
