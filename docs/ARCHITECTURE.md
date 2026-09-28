# AnujHRMS Architecture

## Applications

- Attendance.API — ASP.NET Core REST API
- Attendance.Application — use cases and contracts
- Attendance.Domain — core business entities and rules
- Attendance.Infrastructure — EF Core/SQL Server and external services
- Attendance.DeviceConnector — iClock/ZKTeco adapters
- Attendance.Worker — scheduled synchronization and processing
- Attendance.Web — React web client
- Attendance.Mobile — Android client

## Core flow

Biometric device -> Device Connector -> RawPunch -> Attendance Engine -> Attendance

Web and Android -> API -> Application/Domain -> SQL Server

Raw punches remain immutable. Attendance is calculated from raw punches, shifts, holidays, leave and policy configuration.

## Security

Production is intended to be private/internal. Secrets must come from environment or deployment secret stores and never be committed.
