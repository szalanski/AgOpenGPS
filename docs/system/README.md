# AgOpenGPS Backend System Documentation

This documentation describes the **as-built backend system** - what has been actually implemented in Workflows 001-003. This is high-level system documentation focused on domain concepts, architecture, and behavior (not code implementation details).

## Purpose

This documentation set serves as:
- **System architecture reference** - Understanding the backend system design
- **Context for AI tools** - Quick system comprehension for development assistance
- **Onboarding guide** - New team members understanding what has been built
- **Design rationale** - Why key architectural decisions were made

## What This Documents

The backend API system delivered through three completed workflows:
- **Workflow 001**: Backend state foundation (ApplicationOrchestrator, SignalR communication)
- **Workflow 002**: GPS/GNSS processing migration (UDP reception, coordinate transforms, state broadcasting)
- **Workflow 003**: Simulator domain model refactoring (DDD with bounded contexts)

## What This Is NOT

- **NOT code documentation** - We describe WHAT the system does, not HOW it's coded
- **NOT API reference** - For API details, see the code and inline comments
- **NOT workflow plans** - See [docs/workflow/](../workflow/) for task-based work documentation
- **NOT theoretical architecture** - See [docs/architecture/](../architecture/) for planned patterns

## Documentation Structure

### System Documentation (High-Level Concepts)

1. **[01-system-overview.md](01-system-overview.md)** - Two-process architecture and system lifecycle
2. **[02-main-processing-loop.md](02-main-processing-loop.md)** - Event-driven main loop (GPS-driven)
3. **[03-real-time-communication.md](03-real-time-communication.md)** - Bidirectional state and command flow
4. **[04-gnss-data-pipeline.md](04-gnss-data-pipeline.md)** - GPS data processing and transformations
5. **[05-network-communication.md](05-network-communication.md)** - UDP-based data reception
6. **[06-simulator-capabilities.md](06-simulator-capabilities.md)** - Built-in GPS simulator with vehicle physics
7. **[07-command-handling.md](07-command-handling.md)** - Command/Query separation pattern
8. **[08-client-integration.md](08-client-integration.md)** - How frontend applications connect
9. **[09-domain-model.md](09-domain-model.md)** - Core domain concepts and value objects
10. **[10-simulator-domain.md](10-simulator-domain.md)** - Simulator bounded contexts
11. **[11-testing-strategy.md](11-testing-strategy.md)** - Integration testing approach

### Architecture Decision Records (Why Choices Were Made)

See [adr/README.md](adr/README.md) for the complete list of architectural decisions.

Key decisions documented:
- Why event-driven architecture (GPS-driven, not timer-based)
- Why backend-driven timing (not frontend timers)
- Why bidirectional communication (not request/response)
- Why command/query separation (CQRS pattern)
- Why simulator domain separation (3 bounded contexts)
- Why value objects (type safety, domain language)
- Why UDP communication (even in simulator)
- Why transport abstraction (future-proof)

## Quick Navigation

### Understanding the System
Start here if you're new to the backend system:
1. [System Overview](01-system-overview.md) - Architecture and components
2. [Main Processing Loop](02-main-processing-loop.md) - How the system operates
3. [Real-Time Communication](03-real-time-communication.md) - How components communicate

### Understanding GPS Processing
For GPS/GNSS subsystem:
1. [GNSS Data Pipeline](04-gnss-data-pipeline.md) - How GPS data is processed
2. [Network Communication](05-network-communication.md) - How data arrives
3. [Domain Model](09-domain-model.md) - GPS domain concepts

### Understanding the Simulator
For simulator subsystem:
1. [Simulator Capabilities](06-simulator-capabilities.md) - What the simulator does
2. [Simulator Domain](10-simulator-domain.md) - How it's organized internally
3. [Command Handling](07-command-handling.md) - How to control the simulator

### Understanding Integration
For connecting frontend applications:
1. [Client Integration](08-client-integration.md) - How to connect
2. [Real-Time Communication](03-real-time-communication.md) - Communication patterns
3. [Testing Strategy](11-testing-strategy.md) - How the system is tested

## Relationship to Other Documentation

This documentation complements (but does not replace) other docs:

| Documentation | Purpose | Audience |
|---------------|---------|----------|
| [docs/system/](.) (THIS) | As-built system (domain concepts) | Developers, AI, architects |
| [docs/architecture/](../architecture/) | Planned patterns (before implementation) | Planning phase, understanding rationale |
| [docs/workflow/](../workflow/) | Task-based work (during implementation) | Active development, AI-assisted coding |
| [CLAUDE.md](../../CLAUDE.md) | Full project context | AI tools (Claude Code) |
| Code comments | Implementation details | Code-level understanding |

## Reading Recommendations

### For New Team Members
1. Start with [System Overview](01-system-overview.md)
2. Read [Main Processing Loop](02-main-processing-loop.md)
3. Skim ADRs in [adr/](adr/) to understand key decisions
4. Deep-dive into specific subsystems as needed

### For AI Context Engineering
All files are optimized for AI consumption (150-300 lines, high-level concepts). Load relevant files based on task:
- GPS work → Load GNSS, Network, Domain Model docs
- Simulator work → Load Simulator, Command, Domain docs
- Communication work → Load Real-Time, Client Integration docs

### For Understanding Decisions
Go straight to [adr/](adr/) directory to see why choices were made, trade-offs considered, and when decisions were implemented.

## Contributing

When the system evolves:
1. Update relevant system docs to reflect new behavior
2. Add new ADRs for significant architectural decisions
3. Keep docs high-level (domain concepts, not code)
4. Maintain 150-300 line target per file
5. Cross-reference related docs

## Version Info

- **Documentation created**: 2025 (post Workflow 003 completion)
- **System version**: Workflows 001-003 completed
- **Backend**: .NET 8 (AgOpenGPS.Api)
- **Client library**: .NET Standard 2.0 (AgOpenGPS.Api.Client)
- **Frontend**: .NET Framework 4.8 (FormGPS) - partial integration
- **Tests**: 41/44 integration tests passing
