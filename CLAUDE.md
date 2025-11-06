# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AgOpenGPS is a precision agriculture mapping and section control application. The system consists of two main programs:

- **AgOpenGPS** - Main guidance application with GPS-based field mapping, AB line guidance, section control, and auto-steerings
- **AgIO** - Communication hub that interfaces with external hardware (GPS, IMU, steering controllers, section controllers)

Additional utilities: AgDiag (diagnostics), ModSim (simulator), GPS_Out (GPS output), Keypad (external keypad support)

## IMPORTANT: Parallel Migration Initiatives

This codebase has TWO INDEPENDENT cross-platform initiatives:

### Initiative 1: AgOpenGPS.Core (MVP Pattern - Separate Team)

- **Project**: `AgOpenGPS.Core/`, `AgOpenGPS.WpfApp/`, `AgOpenGPS.WpfViews/`
- **Approach**: Model-View-Presenter pattern, WPF migration
- **Team**: Separate team (not this migration)
- **Status**: In progress by others

### Initiative 2: Backend API Migration (Strangler Fig - THIS Migration)

**Your main foucs should be around this migration. Migration is coducted with Strangler Fig Pattern**

- **New Projects**: `AgOpenGPS.Api/` (.NET 8), `AgOpenGPS.Api.Client/` (.NET Standard 2.0)
- **Approach**: Backend-driven with SignalR, Strangler Fig Pattern
- **Team**: THIS migration work (cross-platform-support branch)
- **Status**: pending

**These initiatives are COMPLETELY INDEPENDENT and do not share code.**
