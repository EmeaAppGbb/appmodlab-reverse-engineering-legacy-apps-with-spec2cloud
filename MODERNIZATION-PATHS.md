# TransFleet Modernization Paths

> **Version:** 1.0  
> **Date:** April 2026  
> **Source System:** TransFleet Fleet Management — .NET Framework 4.7.2 Monolith  
> **Target Platform:** Azure Container Apps / .NET 8+  
> **Methodology:** Based on deep static analysis of 5 projects, 40+ source files, 3,500+ LOC

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Modernization Strategy Overview](#2-modernization-strategy-overview)
3. [Phase 0 — Foundation & Preparation](#3-phase-0--foundation--preparation)
4. [Phase 1 — Microservices Decomposition](#4-phase-1--microservices-decomposition)
5. [Phase 2 — .NET Framework to .NET 8 Migration](#5-phase-2--net-framework-to-net-8-migration)
6. [Phase 3 — WCF to gRPC/REST Migration](#6-phase-3--wcf-to-grpcrest-migration)
7. [Phase 4 — EF6 to EF Core Migration](#7-phase-4--ef6-to-ef-core-migration)
8. [Phase 5 — Azure Container Apps Deployment](#8-phase-5--azure-container-apps-deployment)
9. [Phase 6 — Event-Driven Architecture for Telematics](#9-phase-6--event-driven-architecture-for-telematics)
10. [Phase 7 — Database Modernization](#10-phase-7--database-modernization)
11. [Risk Register](#11-risk-register)
12. [Migration Sequence & Dependencies](#12-migration-sequence--dependencies)
13. [Effort Summary](#13-effort-summary)

---

## 1. Executive Summary

TransFleet is a legacy fleet management monolith with **47 cataloged technical debt items** (12 critical), **zero security controls**, no structured logging, and several non-functional features (geofencing always returns `true`, all external integrations are stubs). The system manages vehicles, drivers, DOT compliance, fuel transactions, maintenance, GPS telematics, and geofencing across 5 tightly coupled .NET Framework 4.7.2 projects.

This document provides a phased modernization roadmap with 7 major workstreams, each with detailed effort estimates, risk assessments, and prerequisite dependencies. The recommended approach uses the **Strangler Fig pattern** — incrementally extracting bounded contexts into independent .NET 8 microservices deployed on Azure Container Apps, while maintaining the monolith in parallel until full decomposition is complete.

### Total Estimated Effort

| Dimension | Estimate |
|-----------|----------|
| **Total Duration** | 14–20 months (with parallel workstreams) |
| **Team Size** | 4–6 senior engineers + 1 architect + 1 SRE |
| **Total Person-Months** | 65–95 person-months |
| **Critical Path** | Phase 0 → Phase 2 → Phase 4 → Phase 1 (services) → Phase 5 (deploy) |

---

## 2. Modernization Strategy Overview

### 2.1 Guiding Principles

1. **Strangler Fig over Big Bang** — Extract services incrementally; never do a full rewrite
2. **Fix Critical Bugs First** — Geofence logic, security gaps, and pagination must be addressed before migration
3. **Shared Kernel for Cross-Cutting Entities** — Vehicle, Driver, and GPSPosition are referenced by 4+ contexts; use a shared NuGet package initially
4. **Event-Driven Integration** — Replace synchronous cross-service calls with Azure Service Bus events
5. **Database-per-Service** — Each microservice owns its data; no shared database in target state
6. **Shift Left on Security** — Add authentication/authorization in Phase 0 before any production migration

### 2.2 Current vs. Target Architecture

```
 ┌─── CURRENT STATE ──────────────────────────────────────────────────┐
 │                                                                     │
 │  ┌─────────────────────────────────────────────────────────────┐   │
 │  │              TransFleet Monolith (.NET FW 4.7.2)            │   │
 │  │                                                              │   │
 │  │  WebApi ─► Core ─► Data ─► SQL Server (single database)     │   │
 │  │  WcfServices ──┘     │                                       │   │
 │  │  Jobs ───────────────┘                                       │   │
 │  │                                                              │   │
 │  │  • No auth  • Console.WriteLine  • WCF  • EF6  • Stubs     │   │
 │  └─────────────────────────────────────────────────────────────┘   │
 │                                                                     │
 └─────────────────────────────────────────────────────────────────────┘

                              ▼ ▼ ▼

 ┌─── TARGET STATE ───────────────────────────────────────────────────┐
 │                                                                     │
 │  ┌──────────────┐    ┌───────────────┐    ┌────────────────────┐   │
 │  │ Azure Front  │    │ Azure API     │    │ Azure AD / Entra   │   │
 │  │ Door (WAF)   │───►│ Management    │───►│ ID (OAuth 2.0)     │   │
 │  └──────────────┘    └───────┬───────┘    └────────────────────┘   │
 │                              │                                      │
 │          ┌───────────────────┼───────────────────────┐             │
 │          ▼                   ▼                       ▼             │
 │  ┌──────────────┐   ┌──────────────┐   ┌──────────────────┐      │
 │  │   Vehicle    │   │  Compliance  │   │    Fuel           │      │
 │  │   Service    │   │   Service    │   │   Service         │      │
 │  │  (.NET 8)    │   │  (.NET 8)    │   │  (.NET 8)         │      │
 │  │  Container   │   │  Container   │   │  Container        │      │
 │  └──────┬───────┘   └──────┬───────┘   └──────┬────────────┘      │
 │         │                  │                   │                    │
 │  ┌──────┴───────┐   ┌──────┴───────┐   ┌──────┴────────────┐      │
 │  │ Azure SQL    │   │ Azure SQL    │   │ Azure SQL          │      │
 │  │ (Vehicle DB) │   │ (Comply DB)  │   │ (Fuel DB)          │      │
 │  └──────────────┘   └──────────────┘   └────────────────────┘      │
 │                                                                     │
 │  ┌──────────────┐   ┌──────────────┐   ┌──────────────────────┐   │
 │  │ Maintenance  │   │   Driver     │   │   Telematics          │   │
 │  │   Service    │   │   Service    │   │   Service (gRPC)      │   │
 │  │  (.NET 8)    │   │  (.NET 8)    │   │  (.NET 8)             │   │
 │  │  Container   │   │  Container   │   │  Container            │   │
 │  └──────┬───────┘   └──────┬───────┘   └──────┬───────────────┘   │
 │         │                  │                   │                    │
 │  ┌──────┴───────┐   ┌──────┴───────┐   ┌──────┴───────────────┐   │
 │  │ Azure SQL    │   │ Azure SQL    │   │ Azure Data Explorer   │   │
 │  │ (Maint DB)   │   │ (Driver DB)  │   │ + Azure SQL (hot)     │   │
 │  └──────────────┘   └──────────────┘   └───────────────────────┘   │
 │                                                                     │
 │  ┌──────────────┐   ┌──────────────┐   ┌──────────────────────┐   │
 │  │  Geofence    │   │ Azure Event  │   │ Azure Service Bus     │   │
 │  │  Service     │   │ Hubs (GPS    │   │ (Domain Events)       │   │
 │  │ (.NET 8)     │   │ ingestion)   │   │                       │   │
 │  └──────────────┘   └──────────────┘   └──────────────────────┘   │
 │                                                                     │
 │  ◄──── Azure Container Apps Environment ────►                      │
 │  ◄──── Azure Monitor + App Insights ────►                          │
 │  ◄──── Azure Key Vault (secrets) ────►                             │
 └─────────────────────────────────────────────────────────────────────┘
```

---

## 3. Phase 0 — Foundation & Preparation

> **Goal:** Establish the cross-cutting infrastructure and fix critical blockers before any service extraction.

### 3.1 Add Authentication & Authorization

| Attribute | Details |
|-----------|---------|
| **Current State** | Zero authentication on all 8 REST controllers and WCF endpoint. Vehicle commands (engine disable, door lock) are unprotected. |
| **Target State** | Azure AD / Entra ID OAuth 2.0 bearer tokens on all API endpoints; role-based access (FleetManager, Dispatcher, Driver, Admin, Technician) |
| **Approach** | Add OWIN JWT bearer middleware to existing WebApi project first (monolith); migrate to ASP.NET Core authentication when services extract |
| **Effort** | 2–3 weeks |
| **Risk** | 🟡 Medium — may break existing integrations if clients don't support token auth |
| **Prerequisites** | Azure AD tenant provisioned; client apps registered |

### 3.2 Introduce Structured Logging & Observability

| Attribute | Details |
|-----------|---------|
| **Current State** | `Console.WriteLine` only across all jobs and services. No request tracing, no health checks, no APM. |
| **Target State** | Serilog with Azure Application Insights sink; correlation IDs on all requests; health check endpoints; structured log context (VehicleId, DriverId, FleetId) |
| **Approach** | 1. Add Serilog + `Serilog.Sinks.ApplicationInsights` NuGet packages. 2. Replace all `Console.WriteLine` calls. 3. Add `ActionFilterAttribute` for request/response logging. 4. Add `/health` and `/ready` endpoints. |
| **Effort** | 1–2 weeks |
| **Risk** | 🟢 Low — additive change with no breaking impact |
| **Prerequisites** | Application Insights resource provisioned |

### 3.3 Fix Critical Bugs

| Bug | Location | Fix | Effort |
|-----|----------|-----|--------|
| `IsPointInGeofence()` always returns `true` | `GeofenceService.cs:149-154` | Implement ray-casting algorithm for point-in-polygon detection | 2–3 days |
| `GetAll()` loads entire table (OOM on GPS 100M+ rows) | `Repository.cs:37-40` | Add `IQueryable<T>` support and pagination to repository | 3–5 days |
| 3 controllers bypass service layer | `DriversController`, `FleetsController`, `GPSController` | Route through existing services; create `FleetService` and `GPSService` if needed | 1 week |
| `Find()` returns `IEnumerable<T>` (forces materialization) | `Repository.cs:42-44` | Change to `IQueryable<T>` return type | 1–2 days |

| **Total Phase 0 Effort** | **4–6 weeks** |
|---------------------------|----------------|
| **Risk Level** | 🟡 Medium |
| **Prerequisites** | Azure subscription, Azure AD tenant |

---

## 4. Phase 1 — Microservices Decomposition

### 4.1 Bounded Context Map

Seven bounded contexts emerge from the codebase analysis. The extraction order follows increasing complexity and coupling:

```
   Extraction Order (Strangler Fig)
   ═══════════════════════════════

   ┌─────────────────────────────────────────────────────────────────┐
   │                                                                 │
   │  🥇 Driver ──► 🥈 Maintenance ──► 🥉 Fuel ──► ④ Compliance    │
   │     (Low)        (Medium)           (Medium)     (High)         │
   │                                                                 │
   │  ──► ⑤ Telematics/GPS ──► ⑥ Geofencing ──► ⑦ Vehicle          │
   │        (High)                (Medium)         (Very High)       │
   │                                                                 │
   └─────────────────────────────────────────────────────────────────┘
```

### 4.2 Service 1: Driver Management Service

| Attribute | Details |
|-----------|---------|
| **Bounded Context** | Driver Management |
| **Source Components** | `DriverService` (112 lines), `DriversController`, `Driver` entity, `DriverSpecifications` |
| **Owned Entities** | `Driver` |
| **API Surface** | `GET/POST/PUT /api/drivers`, `GET /api/drivers/{id}` |
| **Business Rules** | License uniqueness, license/medical cert expiry validation, CDL class tracking, termination with vehicle unassignment |
| **Cross-Context Dependencies** | Vehicle (driver assignment/unassignment) — resolve via domain events |
| **Shared Kernel** | Publishes `DriverTerminated`, `DriverSuspended` events; consumes `VehicleDriverAssigned` |
| **Database** | Own Azure SQL database with `Drivers` table |
| **Complexity** | 🟢 Low — well-defined, fewest dependencies |
| **Effort** | 2–3 weeks |
| **Risk** | 🟢 Low |
| **Prerequisites** | Phase 0 complete; Azure Service Bus provisioned |

### 4.3 Service 2: Maintenance & Work Orders Service

| Attribute | Details |
|-----------|---------|
| **Bounded Context** | Maintenance & Work Orders |
| **Source Components** | `MaintenanceService` (189 lines), `MaintenanceController`, `MaintenanceRules`, `MaintenanceAlertJob` |
| **Owned Entities** | `MaintenanceSchedule`, `WorkOrder` |
| **API Surface** | `GET/POST/PUT /api/maintenance/schedules`, `POST /api/maintenance/schedules/{id}/complete`, `GET/POST/PUT /api/maintenance/workorders`, `GET /api/maintenance/overdue/fleet/{id}` |
| **Business Rules** | Interval-based scheduling (mileage + time), overdue detection, work order lifecycle (Open→InProgress→Completed), service completion with next-service recalculation |
| **Cross-Context Dependencies** | Vehicle (odometer reading for mileage-based scheduling) — consume `VehicleOdometerUpdated` events |
| **Background Jobs** | `MaintenanceAlertJob` (6-hour cycle) → migrate to Azure Container Apps job or Hangfire on Container Apps |
| **Complexity** | 🟡 Medium — clear data ownership, but depends on vehicle odometer |
| **Effort** | 3–4 weeks |
| **Risk** | 🟡 Medium — N+1 query in `GetOverdueSchedules()` must be fixed during extraction |
| **Prerequisites** | Driver Service deployed; event bus operational |

### 4.4 Service 3: Fuel Management Service

| Attribute | Details |
|-----------|---------|
| **Bounded Context** | Fuel Management |
| **Source Components** | `FuelService` (186 lines), `FuelController`, `FuelCardValidationRules`, `FuelCardAdapter` |
| **Owned Entities** | `FuelTransaction` |
| **API Surface** | `POST /api/fuel/transactions`, `GET /api/fuel/vehicle/{id}/transactions`, `GET /api/fuel/fleet/{id}/suspicious`, `GET /api/fuel/vehicle/{id}/efficiency`, `GET /api/fuel/fleet/{id}/cost` |
| **Business Rules** | Transaction validation ($500 max, 100 gal max), fraud detection (30-min same-state window), fuel efficiency (MPG), fleet cost analytics |
| **External Integration** | `IFuelCardAdapter` → Fuel card processor (currently stub — must implement real integration) |
| **Cross-Context Dependencies** | Vehicle (fuel type, odometer) — consume `VehicleOdometerUpdated` events; maintain local vehicle projection |
| **Complexity** | 🟡 Medium — external integration adds risk |
| **Effort** | 3–4 weeks |
| **Risk** | 🟡 Medium — fuel card adapter is currently a stub; real integration needed |
| **Prerequisites** | Fuel card processor API credentials; Vehicle Service operational for data sync |

### 4.5 Service 4: DOT Compliance Service

| Attribute | Details |
|-----------|---------|
| **Bounded Context** | DOT/FMCSA Compliance |
| **Source Components** | `ComplianceService` (229 lines), `ComplianceController`, `DOTComplianceRules`, `ComplianceCheckJob` |
| **Owned Entities** | `HOSLog` |
| **API Surface** | `GET /api/compliance/driver/{id}/check`, `GET /api/compliance/driver/{id}/report`, `GET /api/compliance/driver/{id}/remaining-hours`, `GET /api/compliance/driver/{id}/violations`, `POST /api/compliance/driver/{id}/duty-status` |
| **Business Rules** | Federal DOT HOS regulations (49 CFR Part 395): 11hr driving/day, 14hr on-duty/day, 60hr/7-day week, 10hr off-duty, 30-min rest break after 8hr. Currently hardcoded — should be configurable. |
| **External Integration** | `IDOTReportingAdapter` → FMCSA ELD system (currently stub — critical to implement) |
| **Background Jobs** | `ComplianceCheckJob` (daily batch) |
| **Key Migration Tasks** | 1. Make DOT constants configurable via Azure App Configuration. 2. Implement real FMCSA ELD API. 3. Fix duplicated compliance logic in `CheckDriverCompliance()` / `GetViolations()`. 4. Implement 8-day/70-hour cycle (unused `MaxDrivingHoursPerWeek8Day` constant). |
| **Complexity** | 🟠 High — federal regulation correctness is critical; real FMCSA integration required |
| **Effort** | 4–6 weeks |
| **Risk** | 🟠 High — federal compliance errors have legal consequences |
| **Prerequisites** | FMCSA ELD API access; Driver Service for driver data; legal review of HOS implementation |

### 4.6 Service 5: Telematics & GPS Service

| Attribute | Details |
|-----------|---------|
| **Bounded Context** | Telematics & GPS |
| **Source Components** | `TelematicsService` (WCF, 95 lines), `GPSController`, `DataArchivalJob` |
| **Owned Entities** | `GPSPosition` (100M+ rows) |
| **API Surface** | gRPC streaming for device ingestion (replacing WCF); REST API `POST /api/gps/positions`, `GET /api/gps/vehicle/{id}/positions`, `GET /api/gps/vehicle/{id}/latest` |
| **Ingestion** | Replace WCF SOAP endpoint with gRPC bidirectional streaming or Azure Event Hubs for high-throughput GPS ingestion |
| **Data Volume** | 100M+ GPS rows — requires time-series storage (Azure Data Explorer) with hot/warm/cold tiering |
| **Vehicle Commands** | `SendCommand()` is currently a stub — implement via Azure IoT Hub device-to-cloud messaging |
| **Background Jobs** | `DataArchivalJob` (currently a no-op) → implement real archival to Azure Data Explorer or cold storage |
| **Complexity** | 🟠 High — WCF migration, high data volume, real-time requirements |
| **Effort** | 5–7 weeks |
| **Risk** | 🟠 High — data migration of 100M+ GPS rows; must maintain ingestion during migration |
| **Prerequisites** | WCF→gRPC migration (Phase 3); Azure Event Hubs provisioned; time-series database selected |

### 4.7 Service 6: Geofencing & Alerts Service

| Attribute | Details |
|-----------|---------|
| **Bounded Context** | Geofencing & Alerts |
| **Source Components** | `GeofenceService` (169 lines), `GeofencesController` |
| **Owned Entities** | `Geofence` |
| **API Surface** | `GET/POST/PUT/DELETE /api/geofences`, `GET /api/geofences/vehicle/{vehicleId}/in/{geofenceId}`, `GET /api/geofences/fleet/{id}/alerts` |
| **Critical Fix Required** | `IsPointInGeofence()` always returns `true` — must implement real ray-casting/winding-number algorithm before migration |
| **Spatial Optimization** | Replace string-based GeoJSON polygon with Azure SQL spatial types (`geography`/`geometry`) or PostGIS for efficient spatial queries |
| **Real-Time Component** | Consume `VehiclePositionUpdated` events from Telematics Service; evaluate geofence violations in near-real-time; publish `GeofenceViolation` events |
| **Push Notifications** | Implement via Azure SignalR Service for real-time fleet manager alerts |
| **Complexity** | 🟡 Medium — after fixing the broken spatial logic |
| **Effort** | 3–4 weeks |
| **Risk** | 🟡 Medium — spatial logic must be thoroughly tested |
| **Prerequisites** | Telematics Service operational (GPS event stream); Phase 0 geofence bug fix |

### 4.8 Service 7: Vehicle Management Service

| Attribute | Details |
|-----------|---------|
| **Bounded Context** | Vehicle Management |
| **Source Components** | `VehicleService` (498 lines — God Class), `VehiclesController`, `FleetsController`, `Fleet` entity, `Client` entity, `VehicleSpecifications` |
| **Owned Entities** | `Vehicle`, `Fleet`, `Client` |
| **God Class Decomposition** | Before extraction, split `VehicleService` into: |
| | • `VehicleCrudService` — CRUD, VIN/plate validation, status lifecycle |
| | • `VehicleAssignmentService` — driver assignment/unassignment with qualification checks |
| | • `VehicleHealthService` — health scoring based on maintenance and work orders |
| | • `VehicleUtilizationService` — GPS-based utilization reporting, distance calculations |
| | • `VehicleMaintenanceCostService` — maintenance cost analytics |
| **API Surface** | Full vehicle CRUD + `/health`, `/utilization`, `/maintenance-cost` endpoints |
| **Cross-Context Dependencies** | This is the most coupled service. It currently directly accesses: Driver, MaintenanceSchedule, WorkOrder, GPSPosition, FuelTransaction entities. Must be refactored to consume events and maintain local projections. |
| **Complexity** | 🔴 Very High — God class, highest coupling, touches all other contexts |
| **Effort** | 6–8 weeks |
| **Risk** | 🔴 High — decomposing the God class while maintaining behavioral compatibility |
| **Prerequisites** | All other services deployed and publishing events; Shared Kernel NuGet package established |

### 4.9 Shared Kernel Strategy

Entities referenced across 3+ bounded contexts need a Shared Kernel approach:

| Entity | Referenced By | Strategy |
|--------|---------------|----------|
| `Vehicle` | Vehicle, Maintenance, Fuel, Telematics, Geofencing, Compliance | Each service maintains a local **read projection** of Vehicle (VehicleId, VIN, Status, OdometerReading, FuelType). Vehicle Service is the authoritative source. Sync via `VehicleCreated`, `VehicleUpdated`, `VehicleDeactivated` events. |
| `Driver` | Vehicle, Compliance, Fuel | Local projection (DriverId, Name, LicenseNumber, Status, CDLClass). Driver Service is authoritative. Sync via `DriverCreated`, `DriverUpdated`, `DriverTerminated` events. |
| `GPSPosition` | Telematics, Geofencing, Vehicle (utilization), Fuel | Telematics Service owns the raw data. Other services consume `VehiclePositionUpdated` events with latest position only (not full history). |

### 4.10 Domain Events Catalog

| Event | Publisher | Consumers | Payload |
|-------|-----------|-----------|---------|
| `VehicleCreated` | Vehicle Service | Maintenance (auto-create schedules), Telematics | VehicleId, VIN, FleetId, FuelType |
| `VehicleUpdated` | Vehicle Service | All services with Vehicle projection | VehicleId, changed fields |
| `VehicleDeactivated` | Vehicle Service | Maintenance (cancel schedules), Telematics (stop tracking) | VehicleId |
| `VehicleOdometerUpdated` | Telematics Service | Maintenance (mileage-based scheduling), Fuel (efficiency calc) | VehicleId, NewOdometer, Timestamp |
| `VehiclePositionUpdated` | Telematics Service | Geofencing (violation check), Vehicle (utilization) | VehicleId, Lat, Lon, Speed, Heading, Timestamp |
| `DriverCreated` | Driver Service | Vehicle, Compliance | DriverId, Name, CDLClass, LicenseExpiry |
| `DriverTerminated` | Driver Service | Vehicle (unassign), Compliance (close HOS) | DriverId |
| `DriverSuspended` | Driver Service | Vehicle (unassign) | DriverId |
| `MaintenanceOverdue` | Maintenance Service | Vehicle (health score) | VehicleId, ScheduleId, ServiceType |
| `WorkOrderCompleted` | Maintenance Service | Vehicle (health score) | VehicleId, WorkOrderId, ActualCost |
| `FuelTransactionProcessed` | Fuel Service | Vehicle (odometer update if higher) | VehicleId, Gallons, Amount, OdometerReading |
| `FuelTransactionFlagged` | Fuel Service | Vehicle (alert), Driver (alert) | TransactionId, VehicleId, DriverId, Reason |
| `ComplianceViolationDetected` | Compliance Service | Driver (alert), Vehicle (restrict) | DriverId, ViolationType, Severity |
| `GeofenceViolation` | Geofence Service | Vehicle (alert), Fleet Manager (notification) | VehicleId, GeofenceId, ViolationType, Timestamp |

---

## 5. Phase 2 — .NET Framework to .NET 8 Migration

### 5.1 Migration Strategy

| Attribute | Details |
|-----------|---------|
| **Current** | .NET Framework 4.7.2, C# 8.0 |
| **Target** | .NET 8 LTS (with path to .NET 9/10) |
| **Approach** | Migrate per-service during extraction (not a monolith upgrade). Each microservice is created as a new .NET 8 project, porting business logic from the monolith. |
| **Effort** | 3–5 weeks (core migration work; per-service effort is included in Phase 1 estimates) |
| **Risk** | 🟡 Medium |
| **Prerequisites** | Phase 0 complete; .NET 8 SDK installed; team trained on ASP.NET Core |

### 5.2 Key Migration Steps per Service

| Step | Action | Notes |
|------|--------|-------|
| 1 | Create new .NET 8 `webapi` project with `dotnet new webapi` | Minimal API or Controller-based |
| 2 | Port domain entities from `TransFleet.Data` | Replace `[DataAnnotation]` attributes with EF Core Fluent API |
| 3 | Port business logic from `TransFleet.Core` services | Refactor static rule classes into injectable services |
| 4 | Replace `Web.config` with `appsettings.json` + environment variables | Use Azure App Configuration for shared settings |
| 5 | Replace Autofac with built-in `Microsoft.Extensions.DependencyInjection` | Or keep Autofac with `Autofac.Extensions.DependencyInjection` |
| 6 | Replace `System.Web.Http.ApiController` with `Microsoft.AspNetCore.Mvc.ControllerBase` | Different base class, different attributes |
| 7 | Add `ITimeProvider` (.NET 8 built-in) replacing `DateTime.UtcNow` | Enables testable time-dependent logic |
| 8 | Replace string-based status values with C# enums | `VehicleStatus`, `DriverStatus`, `DutyStatus`, etc. |
| 9 | Add model validation with FluentValidation | Replace missing input validation |
| 10 | Add Polly resilience policies for external calls | Retry, circuit breaker, timeout |

### 5.3 Breaking Changes to Address

| .NET FW Feature | .NET 8 Replacement | Impact |
|-----------------|-------------------|--------|
| `System.Web` | `Microsoft.AspNetCore.*` | All controllers, routing, middleware |
| `Web.config` / `ConfigurationManager` | `IConfiguration` / `appsettings.json` | All config access patterns |
| `HttpResponseMessage` returns | `IActionResult` / `ActionResult<T>` | Controller return types |
| OWIN middleware | ASP.NET Core middleware pipeline | Auth, CORS, error handling |
| `GlobalConfiguration.Configuration` | `WebApplicationBuilder` / `Program.cs` | App startup |
| `System.Data.Entity` (EF6) | `Microsoft.EntityFrameworkCore` | See Phase 4 |
| Autofac `ContainerBuilder` | Built-in DI or `Autofac.Extensions.DependencyInjection` | DI registration |
| Hangfire/Quartz on IIS | Container Apps Jobs or Hangfire on Container Apps | Background job hosting |
| `System.ServiceModel` (WCF) | `Grpc.AspNetCore` or REST | See Phase 3 |
| `Microsoft.AspNet.SignalR` | `Microsoft.AspNetCore.SignalR` + Azure SignalR Service | Real-time communication |

### 5.4 .NET Upgrade Assistant

Use Microsoft's [.NET Upgrade Assistant](https://learn.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-overview) for automated analysis:

```bash
# Analyze the solution for migration feasibility
dotnet tool install -g upgrade-assistant
upgrade-assistant analyze TransFleet.sln

# Per-project upgrade (if upgrading monolith first)
upgrade-assistant upgrade TransFleet.Core\TransFleet.Core.csproj
```

> **Recommendation:** Since we are extracting microservices (not upgrading the monolith in-place), use the Upgrade Assistant primarily for **analysis** to identify incompatible APIs, then manually create new .NET 8 projects.

---

## 6. Phase 3 — WCF to gRPC/REST Migration

### 6.1 Current WCF Service Analysis

| Attribute | Current State |
|-----------|---------------|
| **Service** | `ITelematicsService` / `TelematicsService` |
| **Protocol** | SOAP/WCF with `[DataContract]` serialization |
| **Endpoint** | `http://localhost:5000/TelematicsService.svc` |
| **Operations** | `ReceiveVehicleData` (high-freq inbound), `GetVehicleStatus` (query), `SendCommand` (stub) |
| **Data Contracts** | `VehicleTelematicsData`, `VehicleStatus`, `VehicleCommand` |
| **Volume** | High-frequency GPS ingestion from OBD-II devices |

### 6.2 Migration Decision Matrix

| Operation | Recommended Target | Rationale |
|-----------|-------------------|-----------|
| `ReceiveVehicleData` | **Azure Event Hubs** + gRPC streaming | High-volume ingestion needs event streaming, not request-response. Event Hubs handles 100K+ events/sec with partitioning. |
| `GetVehicleStatus` | **REST API** (ASP.NET Core) | Simple request-response query; REST is simpler for status lookups |
| `SendCommand` | **Azure IoT Hub** (Cloud-to-Device messages) | Purpose-built for device command dispatch with acknowledgment, retry, and offline queuing |

### 6.3 gRPC Service Definition (for direct device communication)

```protobuf
syntax = "proto3";

package transfleet.telematics.v1;

service TelematicsService {
  // Replaces WCF ReceiveVehicleData — bidirectional streaming
  rpc StreamTelemetry(stream VehicleTelemetryMessage) 
      returns (stream TelemetryAck);
  
  // Replaces WCF GetVehicleStatus
  rpc GetVehicleStatus(VehicleStatusRequest) 
      returns (VehicleStatusResponse);
  
  // Replaces WCF SendCommand (stub → real implementation)
  rpc SendVehicleCommand(VehicleCommandRequest) 
      returns (VehicleCommandResponse);
}

message VehicleTelemetryMessage {
  int32 vehicle_id = 1;
  double latitude = 2;
  double longitude = 3;
  double speed = 4;
  double heading = 5;
  google.protobuf.Timestamp timestamp = 6;
  int32 odometer = 7;
  double fuel_level = 8;
  double engine_temp = 9;
  bool ignition_on = 10;
}
```

### 6.4 Migration Effort

| Task | Effort | Risk |
|------|--------|------|
| Define `.proto` files for all WCF contracts | 2–3 days | 🟢 Low |
| Implement gRPC server in .NET 8 | 1 week | 🟢 Low |
| Set up Azure Event Hubs for high-volume ingestion | 3–5 days | 🟡 Medium |
| Implement IoT Hub integration for vehicle commands | 1–2 weeks | 🟡 Medium |
| Migrate OBD-II device firmware to use gRPC/MQTT | 2–4 weeks | 🔴 High — depends on device vendor |
| Run WCF and gRPC in parallel (transition period) | Included above | 🟡 Medium |
| **Total** | **5–8 weeks** | **🟠 High** |

| **Prerequisites** | Azure Event Hubs namespace; Azure IoT Hub; device vendor coordination for firmware update; `.proto` contract review |

---

## 7. Phase 4 — EF6 to EF Core Migration

### 7.1 Current EF6 Analysis

| Attribute | Current State | Issue |
|-----------|---------------|-------|
| **ORM** | Entity Framework 6.4.4 | End of support imminent |
| **Approach** | Code-First with mixed Data Annotations + Fluent API | Inconsistent mapping style |
| **Repository** | Generic `Repository<T>` wrapping `DbSet<T>` | Returns `IEnumerable<T>` (forces materialization) |
| **Unit of Work** | `UnitOfWork` wrapping `TransFleetDbContext` | Creates new repo instances on every access (no caching) |
| **Lazy Loading** | Enabled by default | N+1 query pattern throughout |
| **Entities** | 10 core entities, all anemic (zero behavior) | Business logic scattered in services |
| **Indexes** | None configured for VIN, LicensePlate, etc. | Slow queries on common lookup fields |
| **Async** | Not used — all queries synchronous | Thread pool starvation under load |

### 7.2 Migration Strategy

Since each microservice gets its own database, EF Core migration happens per-service during extraction:

| Step | Action | Notes |
|------|--------|-------|
| 1 | Create new `DbContext` per service (e.g., `DriverDbContext`, `MaintenanceDbContext`) | Each context manages only its owned entities |
| 2 | Use EF Core Fluent API exclusively | Drop mixed Data Annotations approach |
| 3 | Configure proper indexes | Add indexes on VIN, LicensePlate, LicenseNumber, VehicleId+Timestamp, etc. |
| 4 | Return `IQueryable<T>` from repositories | Enable query composition and server-side filtering |
| 5 | Implement async data access (`ToListAsync`, `FirstOrDefaultAsync`) | Prevent thread pool starvation |
| 6 | Disable lazy loading by default | Use explicit `.Include()` for eager loading |
| 7 | Add pagination to all list endpoints | `Skip()`/`Take()` with cursor-based pagination for GPS data |
| 8 | Replace generic `Repository<T>` with specific repositories | `IVehicleRepository`, `IDriverRepository`, etc. with domain-specific queries |
| 9 | Add `IEntityTypeConfiguration<T>` classes | One configuration file per entity |
| 10 | Implement `SaveChangesInterceptor` for audit trails | Automatic `CreatedDate`/`ModifiedDate`/`CreatedBy`/`ModifiedBy` |

### 7.3 Key EF6 → EF Core Differences

| EF6 Feature | EF Core Equivalent | Migration Notes |
|-------------|-------------------|-----------------|
| `DbModelBuilder` (Fluent API) | `ModelBuilder` | API is similar but not identical |
| `HasRequired()` / `HasOptional()` | Navigation property nullability | EF Core infers from CLR type |
| `Map(m => m.MapKey())` | `.HasForeignKey()` | Shadow foreign keys syntax changed |
| `Database.SetInitializer()` | `Database.EnsureCreated()` / Migrations | Different initialization model |
| `LazyLoadingEnabled = true` | `UseLazyLoadingProxies()` (opt-in) | Default is OFF in EF Core ✅ |
| `ComplexType` | `OwnsOne()` | Value objects map differently |
| `[Index]` attribute (EF6.2+) | `HasIndex()` or `[Index]` (EF Core 7+) | Fluent API preferred |
| `Database.SqlQuery<T>()` | `FromSqlRaw()` / `FromSqlInterpolated()` | Raw SQL API changed |

### 7.4 Sample Migration — Driver Entity

**Before (EF6):**
```csharp
// Data Annotations + Fluent API mix
[Table("Drivers")]
public class Driver
{
    [Key]
    public int DriverId { get; set; }
    [Required, MaxLength(100)]
    public string FirstName { get; set; }
    // ... all anemic properties
}
```

**After (EF Core 8):**
```csharp
// Rich domain entity with behavior
public class Driver
{
    public int DriverId { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string LicenseNumber { get; private set; }
    public DateTime LicenseExpiry { get; private set; }
    public CDLClass CdlClass { get; private set; }  // Enum, not string
    public DriverStatus Status { get; private set; } // Enum, not string
    
    public bool IsLicenseExpired(TimeProvider clock) 
        => LicenseExpiry < clock.GetUtcNow().DateTime;
    
    public void Terminate(TimeProvider clock)
    {
        if (Status == DriverStatus.Terminated)
            throw new DomainException("Driver already terminated");
        Status = DriverStatus.Terminated;
        TerminationDate = clock.GetUtcNow().DateTime;
    }
}

// Separate Fluent API configuration
public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("Drivers");
        builder.HasKey(d => d.DriverId);
        builder.Property(d => d.FirstName).IsRequired().HasMaxLength(100);
        builder.HasIndex(d => d.LicenseNumber).IsUnique();
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
    }
}
```

### 7.5 Effort & Risk

| Attribute | Details |
|-----------|---------|
| **Effort** | 2–3 weeks per service (included in Phase 1 per-service estimates) |
| **Total Effort** | 14–21 weeks across 7 services (parallelizable) |
| **Risk** | 🟡 Medium — behavioral changes in EF Core (e.g., cascade delete defaults, tracking behavior) |
| **Prerequisites** | .NET 8 migration (Phase 2) |

---

## 8. Phase 5 — Azure Container Apps Deployment

### 8.1 Deployment Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Azure Container Apps Environment                     │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                        Ingress (HTTPS)                           │  │
│  │  Azure Front Door ──► APIM ──► Container App Ingress            │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐     │
│  │  Vehicle     │ │ Driver      │ │ Compliance  │ │ Fuel        │     │
│  │  Service     │ │ Service     │ │ Service     │ │ Service     │     │
│  │  (min: 1     │ │ (min: 1     │ │ (min: 1     │ │ (min: 1     │     │
│  │   max: 10)   │ │  max: 5)    │ │  max: 5)    │ │  max: 5)    │     │
│  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘     │
│                                                                         │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐                      │
│  │ Maintenance  │ │ Geofence    │ │ Telematics  │                      │
│  │ Service      │ │ Service     │ │ Service     │                      │
│  │ (min: 1      │ │ (min: 1     │ │ (min: 2     │ ◄── Higher min      │
│  │  max: 5)     │ │  max: 5)    │ │  max: 20)   │     for GPS volume  │
│  └─────────────┘ └─────────────┘ └─────────────┘                      │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                   Container Apps Jobs                            │  │
│  │  • ComplianceCheckJob (daily cron)                               │  │
│  │  • MaintenanceAlertJob (every 6 hours cron)                      │  │
│  │  • DataArchivalJob (daily cron)                                  │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                   Dapr Sidecars (Optional)                       │  │
│  │  • Service invocation (service-to-service calls)                 │  │
│  │  • Pub/Sub (Azure Service Bus binding)                           │  │
│  │  • State management (Redis for caching)                          │  │
│  └──────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

### 8.2 Container Apps Configuration per Service

| Service | CPU | Memory | Min Replicas | Max Replicas | Scale Trigger | Health Probe |
|---------|-----|--------|-------------|-------------|---------------|-------------|
| Vehicle | 0.5 | 1 Gi | 1 | 10 | HTTP (concurrent requests) | `/health` |
| Driver | 0.25 | 0.5 Gi | 1 | 5 | HTTP | `/health` |
| Compliance | 0.5 | 1 Gi | 1 | 5 | HTTP | `/health` |
| Fuel | 0.5 | 1 Gi | 1 | 5 | HTTP | `/health` |
| Maintenance | 0.25 | 0.5 Gi | 1 | 5 | HTTP | `/health` |
| Geofence | 0.5 | 1 Gi | 1 | 5 | HTTP + Event Hub messages | `/health` |
| Telematics | 1.0 | 2 Gi | 2 | 20 | Event Hub partition count | `/health` |

### 8.3 Infrastructure as Code (Bicep)

```bicep
// Container Apps Environment
resource containerAppEnv 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: 'transfleet-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'azure-monitor'
    }
    daprAIConnectionString: appInsights.properties.ConnectionString
  }
}

// Example: Driver Service Container App
resource driverService 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'driver-service'
  location: location
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: { external: false, targetPort: 8080 }
      secrets: [{ name: 'db-connection', value: driverDbConnectionString }]
      dapr: { enabled: true, appId: 'driver-service', appPort: 8080 }
    }
    template: {
      containers: [{
        name: 'driver-service'
        image: '${acrLoginServer}/transfleet/driver-service:latest'
        resources: { cpu: json('0.25'), memory: '0.5Gi' }
        env: [{ name: 'ConnectionStrings__DriverDb', secretRef: 'db-connection' }]
      }]
      scale: { minReplicas: 1, maxReplicas: 5 }
    }
  }
}
```

### 8.4 Effort & Risk

| Attribute | Details |
|-----------|---------|
| **Effort** | 3–4 weeks for infrastructure setup + CI/CD; per-service deployment is 1–2 days each |
| **Risk** | 🟡 Medium — new deployment model; team needs Container Apps experience |
| **Prerequisites** | Azure subscription with Container Apps enabled; ACR (Azure Container Registry); GitHub Actions or Azure DevOps CI/CD pipelines; .NET 8 services containerized (Dockerfile per service) |

### 8.5 CI/CD Pipeline

| Stage | Tool | Action |
|-------|------|--------|
| Build | GitHub Actions | `dotnet build`, `dotnet test`, `dotnet publish` |
| Containerize | Docker | Build multi-stage Dockerfile per service |
| Push | ACR | Push to Azure Container Registry |
| Deploy (Dev) | Bicep + `az containerapp update` | Deploy to dev environment |
| Integration Test | GitHub Actions | Run API integration tests against dev |
| Deploy (Staging) | Bicep | Blue-green deployment to staging |
| Deploy (Prod) | Bicep + traffic splitting | Canary deployment (10% → 50% → 100%) |

---

## 9. Phase 6 — Event-Driven Architecture for Telematics

### 9.1 Problem Statement

The current system receives GPS telemetry via synchronous WCF calls, storing each position directly in SQL Server. With 100M+ GPS rows and growing, this approach cannot scale. The `DataArchivalJob` is a no-op, so data grows unbounded.

### 9.2 Target Architecture for High-Volume GPS

```
  OBD-II Devices              Azure Event Hubs              Consumers
  ─────────────               ────────────────              ─────────
                               
  ┌──────────┐    gRPC/MQTT   ┌──────────────┐  ┌──────────────────────┐
  │ Vehicle 1 │──────────────►│              │  │ Consumer Group 1:    │
  │ GPS+OBD   │               │  Event Hub   │─►│ Telematics Service   │
  └──────────┘               │  "gps-ingest" │  │ (hot store → SQL)    │
                              │              │  └──────────────────────┘
  ┌──────────┐               │  32 partitions│  ┌──────────────────────┐
  │ Vehicle 2 │──────────────►│              │─►│ Consumer Group 2:    │
  │ GPS+OBD   │               │  Throughput: │  │ Geofence Evaluator   │
  └──────────┘               │  1M events/hr│  │ (real-time alerts)   │
                              │              │  └──────────────────────┘
  ┌──────────┐               │              │  ┌──────────────────────┐
  │ Vehicle N │──────────────►│              │─►│ Consumer Group 3:    │
  │ GPS+OBD   │               └──────────────┘  │ ADX Ingestor         │
  └──────────┘                                  │ (cold → Data Explorer)│
                                                └──────────────────────┘
                                                ┌──────────────────────┐
                                               ─►│ Consumer Group 4:    │
                                                │ Vehicle Odometer     │
                                                │ Update Processor     │
                                                └──────────────────────┘
```

### 9.3 Data Tiering Strategy

| Tier | Storage | Data Age | Purpose | Query Pattern |
|------|---------|----------|---------|---------------|
| **Hot** | Azure SQL (per-vehicle latest N positions) | Last 24 hours | Real-time tracking, latest position lookups | Point queries by VehicleId |
| **Warm** | Azure Data Explorer (ADX) | 1–90 days | Historical route playback, utilization reports, compliance audits | Time-range queries, aggregations |
| **Cold** | Azure Blob Storage (Parquet) | 90+ days | Regulatory archive, long-term analytics | Batch queries via Synapse/ADX external tables |

### 9.4 Event Processing Patterns

| Pattern | Implementation | Use Case |
|---------|----------------|----------|
| **Event Sourcing** | Event Hubs + ADX for GPS data | Full position history as immutable event log |
| **CQRS** | Separate write (Event Hubs → processors) and read (ADX/SQL) models | High-write GPS ingestion with optimized read queries |
| **Event-Driven Pub/Sub** | Azure Service Bus Topics | Domain events between microservices (not GPS) |
| **Stream Processing** | Event Hubs + Container Apps consumer groups | Real-time geofence evaluation, odometer updates |

### 9.5 Effort & Risk

| Attribute | Details |
|-----------|---------|
| **Effort** | 5–7 weeks |
| **Risk** | 🟠 High — requires migration of 100M+ rows; new streaming paradigm; must maintain data ingestion during migration |
| **Prerequisites** | Azure Event Hubs namespace (Standard or Premium tier); Azure Data Explorer cluster; Telematics Service on .NET 8 (Phase 1.5 + Phase 2) |

### 9.6 Migration Plan for GPS Data

| Step | Action | Duration |
|------|--------|----------|
| 1 | Deploy Event Hubs + new Telematics Service alongside WCF | 1 week |
| 2 | Dual-write: WCF continues writing to old SQL; new service writes to Event Hubs + hot store | 1 week |
| 3 | Backfill warm tier: migrate 90-day historical GPS data to ADX | 2–3 days |
| 4 | Backfill cold tier: export 90+ day data to Parquet in Blob Storage | 2–3 days |
| 5 | Validate data completeness and query equivalence | 1 week |
| 6 | Cut over: redirect devices to new gRPC/MQTT endpoint; decommission WCF | 1 week |
| 7 | Drop old `GPSPositions` table from monolith SQL Server | After 30-day observation |

---

## 10. Phase 7 — Database Modernization

### 10.1 Current State

| Attribute | Details |
|-----------|---------|
| **Engine** | SQL Server 2014+ / LocalDB |
| **Database** | Single `TransFleet` database with 10 core tables |
| **Connection** | `(LocalDB)\MSSQLLocalDB` with Integrated Security |
| **High-Volume Table** | `GPSPositions` — 100M+ rows, no archival, no partitioning |
| **Issues** | No indexes on lookup columns, string-based enums, orphaned FK (WorkOrder → Vendor), missing inverse navigation properties |

### 10.2 Target Database Architecture (Database-per-Service)

| Service | Database Engine | Justification | Tables |
|---------|----------------|---------------|--------|
| **Vehicle** | Azure SQL Database (General Purpose, 4 vCores) | Relational data, moderate volume, ACID transactions | `Vehicles`, `Fleets`, `Clients` |
| **Driver** | Azure SQL Database (General Purpose, 2 vCores) | Small dataset, simple queries | `Drivers` |
| **Compliance** | Azure SQL Database (General Purpose, 2 vCores) | Relational HOS data, regulatory audit requirements | `HOSLogs` |
| **Fuel** | Azure SQL Database (General Purpose, 2 vCores) | Financial transactions requiring ACID | `FuelTransactions` |
| **Maintenance** | Azure SQL Database (General Purpose, 2 vCores) | Work order lifecycle, scheduling | `MaintenanceSchedules`, `WorkOrders` |
| **Telematics** | Azure Data Explorer (ADX) + Azure SQL (hot tier) | Time-series GPS data: ADX for analytics, SQL for latest-position lookups | `GPSPositions` (hot in SQL, warm/cold in ADX) |
| **Geofence** | Azure SQL Database with Spatial Types | Spatial queries benefit from `geography` column type and spatial indexes | `Geofences` |

### 10.3 Data Migration Strategy

| Phase | Action | Approach |
|-------|--------|----------|
| **Phase A: Schema per Service** | Create individual Azure SQL databases for each service | Azure CLI / Bicep; EF Core migrations generate schema |
| **Phase B: Data Split** | Copy data from monolith tables to service-owned databases | One-time ETL using Azure Data Factory or custom migration scripts |
| **Phase C: Dual Write** | Monolith writes to both old and new databases during transition | Change Data Capture (CDC) on source SQL Server, or application-level dual-write |
| **Phase D: Cut Over** | Point services to their own databases; monolith becomes read-only proxy | Feature flags to control traffic routing |
| **Phase E: Decommission** | Drop tables from monolith database; shut down monolith SQL Server | After 30-day observation period per service |

### 10.4 Azure SQL Configuration

```bicep
// Elastic Pool for cost optimization (services share resources)
resource sqlElasticPool 'Microsoft.Sql/servers/elasticPools@2023-05-01-preview' = {
  name: 'transfleet-pool'
  parent: sqlServer
  location: location
  sku: {
    name: 'GP_Gen5'
    tier: 'GeneralPurpose'
    capacity: 8  // 8 vCores shared across all service databases
  }
  properties: {
    maxSizeBytes: 107374182400  // 100 GB
    perDatabaseSettings: {
      minCapacity: json('0.25')
      maxCapacity: 4
    }
  }
}
```

### 10.5 Key Improvements per Database

| Improvement | Details |
|-------------|---------|
| **Indexes** | Add indexes on: `Vehicles.VIN` (unique), `Vehicles.LicensePlate`, `Drivers.LicenseNumber` (unique), `GPSPositions(VehicleId, Timestamp DESC)`, `FuelTransactions(VehicleId, TransactionDate)`, `HOSLogs(DriverId, StartTime)` |
| **Enum Columns** | Convert string statuses to `TINYINT` with EF Core `HasConversion<string>()` or store as int |
| **Spatial Types** | Geofence polygons stored as `geography` with spatial indexes instead of GeoJSON strings |
| **Row-Level Security** | Multi-tenant isolation using `SESSION_CONTEXT` for FleetId/ClientId |
| **Temporal Tables** | Enable SQL Server temporal tables for audit trail on `Vehicles`, `Drivers`, `WorkOrders` |
| **Encryption** | Transparent Data Encryption (TDE) enabled by default on Azure SQL; consider Always Encrypted for PII (driver license numbers, SSN if stored) |

### 10.6 Effort & Risk

| Attribute | Details |
|-----------|---------|
| **Effort** | 4–6 weeks (schema creation + data migration + validation) |
| **Risk** | 🟠 High — data migration must be lossless; referential integrity across service boundaries |
| **Prerequisites** | Service extraction (Phase 1) at least partially complete; Azure SQL server provisioned; Azure Data Explorer cluster for telematics |

---

## 11. Risk Register

| ID | Risk | Likelihood | Impact | Mitigation | Phase |
|----|------|-----------|--------|------------|-------|
| R-01 | **God Class decomposition breaks business logic** — `VehicleService` (498 lines, 13 methods) touches all contexts | High | Critical | Extract gradually using Strangler Fig; maintain comprehensive integration tests against monolith behavior | Phase 1.7 |
| R-02 | **GPS data migration causes data loss** — 100M+ rows must be migrated without downtime | Medium | Critical | Use dual-write pattern; validate row counts and checksums; keep monolith running in parallel for 30 days | Phase 6, 7 |
| R-03 | **OBD-II device firmware incompatible with gRPC** — devices may only support SOAP/WCF | Medium | High | Maintain WCF→gRPC bridge proxy; coordinate with device vendor 6+ months in advance | Phase 3 |
| R-04 | **Federal DOT compliance regression** — HOS calculation changes during migration could have legal consequences | Medium | Critical | 100% unit test coverage on DOT rules; parallel-run old and new compliance checks for 90 days; legal review | Phase 1.4 |
| R-05 | **Data consistency during dual-write** — events may be lost or duplicated during transition | Medium | High | Use outbox pattern with idempotent consumers; reconciliation batch jobs; Azure Service Bus dead-letter monitoring | Phase 1, 6 |
| R-06 | **Team lacks .NET 8 / Container Apps / gRPC experience** | High | Medium | Training sprints (2 weeks); start with Driver Service (lowest complexity); pair programming | All phases |
| R-07 | **Integration stubs mask real API compatibility issues** — DOT and Fuel Card adapters are stubs | High | High | Engage with FMCSA and fuel card APIs early; build integration test harness against sandbox environments | Phase 1.3, 1.4 |
| R-08 | **Shared Kernel entities create tight coupling** — Vehicle and Driver are referenced everywhere | Medium | High | Use eventual consistency via events; accept ~seconds of data delay between services; local read projections | Phase 1 |
| R-09 | **Performance regression after microservices split** — N network hops replace in-process calls | Medium | Medium | Use Dapr service invocation for synchronous calls; cache aggressively with Redis; monitor latency with App Insights | Phase 1, 5 |
| R-10 | **Cost overrun on Azure resources** — 7 databases + Event Hubs + ADX + Container Apps | Medium | Medium | Use Azure SQL Elastic Pools; start with consumption-tier Container Apps; reserved capacity for production | Phase 5, 7 |

---

## 12. Migration Sequence & Dependencies

```
Timeline (months)
═══════════════

Month 1-2        Month 3-4         Month 5-7          Month 8-10        Month 11-14
─────────        ─────────         ─────────          ──────────        ───────────

┌──────────┐
│ Phase 0  │   ┌──────────────┐
│Foundation│──►│ Phase 2      │   ┌──────────────────────────────────────────────┐
│• Auth    │   │ .NET 8 base  │──►│ Phase 1: Service Extraction (parallel)       │
│• Logging │   │ • Project    │   │                                              │
│• Bug fix │   │   templates  │   │ ┌────────────┐ ┌─────────────┐              │
│• Paginate│   │ • CI/CD      │   │ │🥇 Driver   │ │🥈 Maint     │              │
└──────────┘   │ • Containers │   │ │ (2-3 wks)  │ │ (3-4 wks)   │              │
               └──────────────┘   │ └────────────┘ └─────────────┘              │
                                  │ ┌────────────┐ ┌─────────────┐              │
               ┌──────────────┐   │ │🥉 Fuel     │ │④ Compliance │              │
               │ Phase 4      │──►│ │ (3-4 wks)  │ │ (4-6 wks)   │              │
               │ EF Core      │   │ └────────────┘ └─────────────┘              │
               │ (per-service)│   │                                              │
               └──────────────┘   │ ┌────────────┐ ┌─────────────┐ ┌──────────┐│
                                  │ │⑤ Telematics│ │⑥ Geofence   │ │⑦ Vehicle ││
               ┌──────────────┐   │ │ (5-7 wks)  │ │ (3-4 wks)   │ │(6-8 wks) ││
               │ Phase 3      │──►│ └────────────┘ └─────────────┘ └──────────┘│
               │ WCF→gRPC     │   └──────────────────────────────────────────────┘
               └──────────────┘
                                               ┌──────────────┐
                                               │ Phase 5      │
                                            ──►│ Container    │
                                               │ Apps Deploy  │
                                               │ (per service)│
                                               └──────────────┘
                                                          ┌──────────────┐
                                                          │ Phase 6      │
                                                       ──►│ Event-Driven │
                                                          │ GPS Pipeline │
                                                          └──────────────┘
                                                          ┌──────────────┐
                                                          │ Phase 7      │
                                                       ──►│ Database     │
                                                          │ Modernize    │
                                                          └──────────────┘
```

### Dependency Graph

| Phase | Depends On | Can Parallelize With |
|-------|-----------|---------------------|
| Phase 0 (Foundation) | Nothing | — |
| Phase 2 (.NET 8) | Phase 0 | — |
| Phase 3 (WCF→gRPC) | Phase 2 | Phase 1 (non-telematics services) |
| Phase 4 (EF Core) | Phase 2 | Phase 1, Phase 3 |
| Phase 1.1 (Driver Service) | Phase 0, Phase 2 | Phase 3, Phase 4 |
| Phase 1.2 (Maintenance Service) | Phase 1.1 (event bus patterns established) | Phase 3 |
| Phase 1.3 (Fuel Service) | Phase 1.1 | Phase 3, Phase 1.2 |
| Phase 1.4 (Compliance Service) | Phase 1.1 | Phase 1.3 |
| Phase 1.5 (Telematics Service) | Phase 3 (WCF→gRPC) | Phase 1.4 |
| Phase 1.6 (Geofence Service) | Phase 1.5 (GPS event stream) | — |
| Phase 1.7 (Vehicle Service) | Phase 1.1–1.6 (all others deployed) | — |
| Phase 5 (Container Apps) | Phase 2, each Phase 1.x service | Phase 6, Phase 7 |
| Phase 6 (Event-Driven GPS) | Phase 1.5, Phase 3 | Phase 7 |
| Phase 7 (Database Modernization) | Phase 1 (service extraction), Phase 4 (EF Core) | Phase 6 |

---

## 13. Effort Summary

### 13.1 Per-Phase Estimates

| Phase | Description | Effort (Person-Weeks) | Duration (Calendar) | Risk |
|-------|-------------|----------------------|--------------------|----|
| **Phase 0** | Foundation (auth, logging, bug fixes) | 4–6 | 4–6 weeks | 🟡 Medium |
| **Phase 1** | Microservices Decomposition (all 7 services) | 26–38 | 8–14 weeks (parallel) | 🟠 High |
| — 1.1 Driver | | 2–3 | 2–3 weeks | 🟢 Low |
| — 1.2 Maintenance | | 3–4 | 3–4 weeks | 🟡 Medium |
| — 1.3 Fuel | | 3–4 | 3–4 weeks | 🟡 Medium |
| — 1.4 Compliance | | 4–6 | 4–6 weeks | 🟠 High |
| — 1.5 Telematics | | 5–7 | 5–7 weeks | 🟠 High |
| — 1.6 Geofence | | 3–4 | 3–4 weeks | 🟡 Medium |
| — 1.7 Vehicle | | 6–8 | 6–8 weeks | 🔴 Very High |
| **Phase 2** | .NET Framework → .NET 8 | 3–5 | 3–5 weeks | 🟡 Medium |
| **Phase 3** | WCF → gRPC/REST | 5–8 | 5–8 weeks | 🟠 High |
| **Phase 4** | EF6 → EF Core | 2–3 per service | Included in Phase 1 | 🟡 Medium |
| **Phase 5** | Azure Container Apps deployment | 3–4 + 1/service | 4–6 weeks | 🟡 Medium |
| **Phase 6** | Event-driven GPS pipeline | 5–7 | 5–7 weeks | 🟠 High |
| **Phase 7** | Database modernization | 4–6 | 4–6 weeks | 🟠 High |
| **TOTAL** | | **52–74 person-weeks** | **14–20 months** | 🟠 High |

### 13.2 Team Composition

| Role | Count | Responsibility |
|------|-------|----------------|
| **Solution Architect** | 1 | Overall design, bounded context boundaries, event modeling, Azure architecture |
| **Senior .NET Engineers** | 3–4 | Service extraction, .NET 8 migration, EF Core, gRPC implementation |
| **SRE / DevOps Engineer** | 1 | Container Apps infrastructure, CI/CD, monitoring, Bicep/Terraform |
| **Database Engineer** | 1 (part-time) | Data migration, Azure SQL optimization, ADX setup, index design |
| **QA Engineer** | 1 | Integration testing, regression testing, compliance verification |

### 13.3 Quick Wins (Achievable in Sprint 1)

| # | Action | Effort | Impact |
|---|--------|--------|--------|
| 1 | Replace `Console.WriteLine` with Serilog + App Insights | 2 days | Observability across all jobs and services |
| 2 | Add JWT bearer auth middleware to WebApi | 3 days | Eliminates #1 security vulnerability |
| 3 | Fix `IsPointInGeofence()` with ray-casting algorithm | 1 day | Geofencing becomes functional |
| 4 | Add `IQueryable<T>` and pagination to Repository | 2 days | Prevents OOM on GPS table |
| 5 | Replace string statuses with C# enums | 1 day | Compile-time safety for all status values |
| 6 | Add health check endpoints (`/health`, `/ready`) | 0.5 days | Container Apps readiness probes |

---

*This document was generated based on deep static analysis of the TransFleet legacy codebase (5 projects, 40+ source files, 3,500+ LOC) and existing architecture documentation. All effort estimates assume a team familiar with .NET and Azure; adjust upward by 30–50% if the team requires significant training.*
