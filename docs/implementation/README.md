# Backend Implementation Documentation

This area captures what has actually been implemented in the AgOpenGPS API backend.\
When descriptions conflict, defer to the referenced source code - code wins.

## Structure

- `sections/` - living documentation for key subsystems:
  - [System Architecture](sections/system-architecture.md)
  - [GNSS Capabilities](sections/gnss-capabilities.md)
  - [Communication Protocols](sections/communication-protocols.md)
  - [Operational Workflows](sections/operational-workflows.md)
  - [Simulator Architecture](sections/simulator-architecture.md)
- `adrs/` - concise architecture decision records that explain why core choices were made:
  - [ADR 0001: GNSS-Orchestrated Backend Pipeline](adrs/adr-0001-gnss-backend.md)

Other conceptual material remains under `docs/architecture/` and future work plans live in `docs/workflow/`.\
Use those for intent; use this folder for the authoritative view of what is deployed.
