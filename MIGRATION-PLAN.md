# TransFleet Modernization — Phased Migration Plan

> **Version:** 1.0  
> **Date:** April 2026  
> **Status:** Draft  
> **Source:** [SPEC2CLOUD-ANALYSIS.md](SPEC2CLOUD-ANALYSIS.md), [MODERNIZATION-PATHS.md](MODERNIZATION-PATHS.md)  
> **Total Duration:** 14–20 months (7 phases, 20 sprints)  
> **Team:** 4–6 Senior .NET Engineers, 1 Architect, 1 SRE, 1 DBA (part-time), 1 QA

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Migration Principles & Strategy](#2-migration-principles--strategy)
3. [Team Structure & RACI](#3-team-structure--raci)
4. [Phase 0 — Foundation (Sprints 1–3)](#4-phase-0--foundation-sprints-13)
5. [Phase 1 — Platform Baseline (Sprints 4–5)](#5-phase-1--platform-baseline-sprints-45)
6. [Phase 2 — Service Extraction Wave 1 (Sprints 6–9)](#6-phase-2--service-extraction-wave-1-sprints-69)
7. [Phase 3 — Service Extraction Wave 2 (Sprints 10–14)](#7-phase-3--service-extraction-wave-2-sprints-1014)
8. [Phase 4 — Service Extraction Wave 3 (Sprints 15–17)](#8-phase-4--service-extraction-wave-3-sprints-1517)
9. [Phase 5 — Event-Driven GPS Pipeline (Sprints 16–18)](#9-phase-5--event-driven-gps-pipeline-sprints-1618)
10. [Phase 6 — Database Modernization (Sprints 17–19)](#10-phase-6--database-modernization-sprints-1719)
11. [Phase 7 — Decommission Monolith (Sprint 20)](#11-phase-7--decommission-monolith-sprint-20)
12. [Dependency Map](#12-dependency-map)
13. [Priority Matrix](#13-priority-matrix)
14. [Rollback Strategies](#14-rollback-strategies)
15. [Testing Strategy](#15-testing-strategy)
16. [Data Migration — GPS 100M+ Records](#16-data-migration--gps-100m-records)
17. [Timeline & Milestones](#17-timeline--milestones)
18. [Risk Register](#18-risk-register)
19. [Appendix A — Domain Events Catalog](#appendix-a--domain-events-catalog)
20. [Appendix B — Definition of Done](#appendix-b--definition-of-done)

---

## 1. Executive Summary

TransFleet is a .NET Framework 4.7.2 fleet management monolith with **47 cataloged technical debt items** (12 critical), **zero security controls**, no structured logging, and several non-functional features. This plan decomposes the modernization into **7 phases across 20 two-week sprints (40 weeks)**, using the **Strangler Fig pattern** to incrementally extract 7 bounded contexts into .NET 8 microservices on Azure Container Apps.

### Key Numbers

| Metric | Value |
|--------|-------|
| Source projects | 5 (.NET FW 4.7.2) |
| Target microservices | 7 (.NET 8, Azure Container Apps) |
| Technical debt items | 47 (12 critical, 18 high) |
| GPS records to migrate | 100M+ rows |
| Bounded contexts | 7 (Driver, Maintenance, Fuel, Compliance, Telematics, Geofence, Vehicle) |
| Domain events | 14 event types |
| Estimated effort | 52–74 person-weeks |
| Calendar duration | 14–20 months |

### Critical Path

```
Phase 0 (Foundation) → Phase 1 (Platform) → Phase 2 (Wave 1: Driver + Maintenance + Fuel)
    → Phase 3 (Wave 2: Compliance + Telematics) → Phase 4 (Wave 3: Geofence + Vehicle)
        → Phase 5 (Event-Driven GPS) → Phase 6 (DB Modernization) → Phase 7 (Decommission)
```

---

## 2. Migration Principles & Strategy

### 2.1 Guiding Principles

1. **Strangler Fig over Big Bang** — extract services incrementally; the monolith remains operational until each service is validated
2. **Fix Critical Blockers First** — security, geofence bug, pagination, and service-layer bypasses must be resolved before migration
3. **Database-per-Service** — each microservice owns its data; no shared database in target state
4. **Event-Driven Integration** — replace synchronous cross-service calls with Azure Service Bus domain events
5. **Shift Left on Security** — authentication and authorization before any production migration
6. **Zero-Downtime Migration** — dual-write patterns, feature flags, and canary deployments throughout

### 2.2 Sprint Cadence

| Attribute | Value |
|-----------|-------|
| Sprint length | 2 weeks |
| Sprint ceremonies | Planning (2h), Daily standup (15m), Review (1h), Retro (1h) |
| Definition of Done | See [Appendix B](#appendix-b--definition-of-done) |
| Release cadence | End of each sprint to staging; production after QA sign-off |

---

## 3. Team Structure & RACI

### 3.1 Team Composition

| Role | ID | Count | Phases |
|------|----|-------|--------|
| Solution Architect | ARCH | 1 | All phases — design authority, bounded context boundaries |
| Senior .NET Engineer | ENG-1..4 | 3–4 | Service extraction, .NET 8 migration, EF Core, gRPC |
| SRE / DevOps Engineer | SRE | 1 | Infrastructure, CI/CD, monitoring, Bicep |
| Database Engineer | DBA | 1 (part-time) | Data migration, Azure SQL, ADX, index design |
| QA Engineer | QA | 1 | Integration testing, regression, compliance verification |

### 3.2 RACI for Key Decisions

| Decision | ARCH | ENG | SRE | DBA | QA |
|----------|:----:|:---:|:---:|:---:|:--:|
| Bounded context boundaries | **R/A** | C | I | C | I |
| API contract design | A | **R** | I | I | C |
| Infrastructure provisioning | C | I | **R/A** | C | I |
| Data migration strategy | A | C | C | **R** | C |
| Test strategy & sign-off | C | C | I | I | **R/A** |
| Production go/no-go | **A** | C | **R** | C | **R** |

---

## 4. Phase 0 — Foundation (Sprints 1–3)

> **Goal:** Establish cross-cutting infrastructure and fix critical blockers before any service extraction.  
> **Duration:** 6 weeks (3 sprints)  
> **Team Focus:** All hands

### Sprint 1 — Security & Observability

| Task ID | Task | Owner | Acceptance Criteria | Effort |
|---------|------|-------|---------------------|--------|
| S1-01 | Add JWT bearer auth (OWIN middleware) to all 8 REST controllers | ENG-1 | All endpoints return 401 without valid token; role claims propagated to controllers; Swagger UI supports "Authorize" button | 3d |
| S1-02 | Provision Azure AD tenant; register client apps; define roles (Admin, FleetManager, Dispatcher, Driver, Technician) | SRE | 5 roles created in Azure AD; test tokens obtainable via Postman/CLI | 2d |
| S1-03 | Replace all `Console.WriteLine` with Serilog + Application Insights sink | ENG-2 | Zero `Console.WriteLine` calls remain; structured logs visible in App Insights within 5 min of emission; correlation IDs on all HTTP requests | 2d |
| S1-04 | Add `/health` and `/ready` endpoints | ENG-2 | Health endpoint returns 200 with DB connectivity check; ready endpoint checks downstream dependencies | 0.5d |
| S1-05 | Add global exception filter (`ExceptionFilterAttribute`) | ENG-1 | Business rule violations return 400 with structured error body; unhandled exceptions return 500 with correlation ID; no stack traces leaked to clients | 1d |
| S1-06 | Add request/response logging action filter | ENG-2 | All API calls logged with method, path, status code, duration, and correlation ID | 0.5d |

**Sprint 1 Exit Criteria:** All API endpoints require authentication; structured logging operational in App Insights; health check endpoints responding.

### Sprint 2 — Critical Bug Fixes & Data Access

| Task ID | Task | Owner | Acceptance Criteria | Effort |
|---------|------|-------|---------------------|--------|
| S2-01 | Fix `IsPointInGeofence()` — implement ray-casting algorithm | ENG-3 | Point-in-polygon returns correct results for 10+ test polygons (convex, concave, edge cases); existing geofence tests pass | 2d |
| S2-02 | Change `Repository.Find()` to return `IQueryable<T>` | ENG-2 | All callers updated; queries compose server-side; verified with SQL profiler | 2d |
| S2-03 | Add pagination to `Repository.GetAll()` — `GetPaged(page, pageSize)` | ENG-2 | Default page size 50; GPS endpoint paginated; no `ToList()` on full tables | 1d |
| S2-04 | Fix 3 controllers bypassing service layer (DriversController, FleetsController, GPSController) | ENG-1 | All 8 controllers delegate to service layer; create `FleetService` and `GPSService` where missing; business rules enforced consistently | 3d |
| S2-05 | Add GPS coordinate validation (lat ±90, lon ±180) | ENG-3 | Invalid coordinates rejected with 400; applied in GPSController, TelematicsService | 0.5d |
| S2-06 | Replace string-based status enums with C# enums | ENG-4 | `VehicleStatus`, `DriverStatus`, `DutyStatus`, `TransactionStatus`, `WorkOrderStatus` as enums; compile-time safety; EF stores as string for backward compat | 2d |
| S2-07 | Add database indexes on VIN, LicensePlate, LicenseNumber, VehicleId+Timestamp | DBA | Indexes created via EF migration; query plan verification on key queries | 1d |

**Sprint 2 Exit Criteria:** Geofence spatial logic functional; no OOM risk on GPS queries; all controllers route through service layer; status values type-safe.

### Sprint 3 — Hardened Monolith & Shared Infrastructure

| Task ID | Task | Owner | Acceptance Criteria | Effort |
|---------|------|-------|---------------------|--------|
| S3-01 | Add `ITimeProvider` abstraction replacing `DateTime.UtcNow` (15+ locations) | ENG-1 | All services inject `ITimeProvider`; time-dependent tests pass with fixed clock | 2d |
| S3-02 | Create custom exception hierarchy (`DomainException`, `NotFoundException`, `ValidationException`) | ENG-3 | Services throw domain-specific exceptions; global filter maps to correct HTTP status codes | 1d |
| S3-03 | Convert static rule classes to injectable services (`IDOTComplianceRules`, `IFuelCardValidationRules`, `IMaintenanceRules`) | ENG-4 | Rules registered in DI; testable with mocked dependencies; constants configurable via `IOptions<T>` | 2d |
| S3-04 | Provision Azure Service Bus namespace (Standard tier) with topics for domain events | SRE | Topics created for all 14 event types (see [Appendix A](#appendix-a--domain-events-catalog)); dead-letter queues configured | 1d |
| S3-05 | Provision Azure Container Registry (ACR), Container Apps environment, and Azure SQL Elastic Pool | SRE | Bicep templates for all resources; deployed to dev subscription; documented in README | 2d |
| S3-06 | Create .NET 8 solution template with shared projects: `TransFleet.Shared.Contracts` (DTOs, events), `TransFleet.Shared.Testing` (test helpers) | ARCH + ENG-2 | Template generates a service with health checks, Serilog, auth, Swagger, EF Core, Service Bus integration out of the box | 3d |
| S3-07 | Set up CI/CD pipeline (GitHub Actions): build → test → containerize → push to ACR → deploy to dev | SRE | Pipeline runs on PR and merge to main; deploys to dev Container Apps environment; includes integration test stage | 2d |
| S3-08 | Write baseline integration tests for all existing monolith API endpoints | QA | 100% endpoint coverage (all 8 controllers); tests assert current behavior as regression baseline; run in CI | 3d |

**Sprint 3 Exit Criteria:** Monolith hardened with auth, logging, bug fixes, and type safety. Azure infrastructure provisioned. CI/CD pipeline operational. Baseline integration tests green.

### Phase 0 Rollback Strategy

| Risk | Rollback |
|------|----------|
| Auth breaks existing clients | Feature flag `ENABLE_AUTH=false` bypasses JWT validation; remove after all clients updated |
| Serilog causes performance regression | Revert to `Console.WriteLine` via git revert; Serilog uses async sink to minimize impact |
| Repository `IQueryable` change breaks queries | EF6 `IQueryable` behavior is superset of `IEnumerable`; low risk; revert specific callers if needed |
| Enum conversion breaks deserialization | Store enums as strings (`HasConversion<string>()`); database values unchanged |

---

## 5. Phase 1 — Platform Baseline (Sprints 4–5)

> **Goal:** Establish .NET 8 project templates, EF Core patterns, and deploy the first microservice skeleton to Container Apps.  
> **Duration:** 4 weeks (2 sprints)  
> **Team Focus:** ARCH, ENG-1, ENG-2, SRE

### Sprint 4 — .NET 8 Foundation & WCF Migration Prep

| Task ID | Task | Owner | Acceptance Criteria | Effort |
|---------|------|-------|---------------------|--------|
| S4-01 | Create .NET 8 Driver Service project from template; port `Driver` entity with EF Core Fluent API configuration | ENG-1 | Clean build; entity mapped with `DriverConfiguration : IEntityTypeConfiguration<Driver>`; indexes on `LicenseNumber` (unique) | 2d |
| S4-02 | Implement `DriverDbContext` with async operations, disabled lazy loading, `SaveChangesInterceptor` for audit | ENG-1 | `CreatedDate`/`ModifiedDate` auto-set; all queries async; no proxy creation | 1d |
| S4-03 | Port `DriverService` business logic to .NET 8; refactor to use `ITimeProvider` and injectable rules | ENG-1 | All license/medical validation, termination workflow; unit tests pass | 2d |
| S4-04 | Implement Driver REST API controllers with FluentValidation | ENG-1 | `GET/POST/PUT /api/drivers`, `GET /api/drivers/{id}`; validation on all inputs; Swagger docs | 1d |
| S4-05 | Define `.proto` files for Telematics gRPC service contracts | ENG-3 | `telematics.v1.proto` covering `StreamTelemetry`, `GetVehicleStatus`, `SendVehicleCommand`; reviewed by ARCH | 2d |
| S4-06 | Create Bicep module for per-service Azure SQL database in Elastic Pool | SRE | Parameterized Bicep; deploys Driver DB to dev; connection string in Key Vault | 1d |
| S4-07 | Create Dockerfile per service (multi-stage build) | SRE | Driver Service builds and runs in container; image size < 200MB; health check in Dockerfile | 1d |

### Sprint 5 — First Service Deployment & Strangler Pattern

| Task ID | Task | Owner | Acceptance Criteria | Effort |
|---------|------|-------|---------------------|--------|
| S5-01 | Deploy Driver Service to Container Apps (dev environment) | SRE | Service running with 1 replica; health probe passing; logs in App Insights; Dapr sidecar enabled | 1d |
| S5-02 | Implement Azure Service Bus publisher in Driver Service (outbox pattern) | ENG-2 | `DriverCreated`, `DriverUpdated`, `DriverTerminated` events published reliably; outbox table prevents message loss | 2d |
| S5-03 | Configure Azure API Management (APIM) routing: `/api/drivers/*` → Driver Service; all other routes → monolith | SRE + ARCH | APIM routes driver requests to new service; all other traffic to monolith; verified with integration tests | 2d |
| S5-04 | Implement data sync: one-time ETL of `Drivers` table from monolith to Driver DB | DBA | Row count matches; all fields mapped; checksums verified | 1d |
| S5-05 | Implement dual-write: monolith publishes `DriverUpdated` events via Service Bus when drivers modified through legacy endpoints | ENG-4 | Driver Service consumes events and stays in sync; conflict resolution: last-write-wins with timestamp | 2d |
| S5-06 | Run Driver Service integration tests against Container Apps | QA | All driver API tests pass against new service; response schema identical to monolith endpoints | 2d |
| S5-07 | Traffic splitting: route 10% of driver traffic to new service → validate → ramp to 100% | SRE | Canary deployment via APIM; error rate < 0.1% at each ramp stage; latency within 20% of monolith | 1d |

**Phase 1 Exit Criteria:** Driver Service running on Container Apps serving 100% of driver traffic. Strangler Fig pattern proven. CI/CD pipeline deploying automatically. Event bus operational.

### Phase 1 Rollback Strategy

| Risk | Rollback |
|------|----------|
| Driver Service errors in production | APIM routing: redirect 100% traffic back to monolith instantly (< 1 min); monolith retains full driver functionality |
| Data sync divergence | Reconciliation job compares Driver Service DB with monolith; alerts on mismatches; manual resolution playbook |
| Event bus failures | Outbox pattern ensures at-least-once delivery; dead-letter queue monitoring; manual replay tool |

---

## 6. Phase 2 — Service Extraction Wave 1 (Sprints 6–9)

> **Goal:** Extract Maintenance and Fuel services (moderate complexity, well-defined boundaries).  
> **Duration:** 8 weeks (4 sprints)  
> **Team Focus:** ENG-1..3 on services, SRE on infra, QA on testing

### Sprint 6–7 — Maintenance & Work Orders Service

| Task ID | Task | Owner | Acceptance Criteria | Effort |
|---------|------|-------|---------------------|--------|
| S6-01 | Port `MaintenanceSchedule` and `WorkOrder` entities to .NET 8 + EF Core | ENG-2 | Entities with rich behavior (interval calculation in entity); EF Core config with indexes | 2d |
| S6-02 | Port `MaintenanceService` — fix N+1 in `GetOverdueSchedules()` with JOIN query | ENG-2 | Single query replaces per-vehicle loop; unit tests cover overdue detection, work order lifecycle | 3d |
| S6-03 | Port `MaintenanceRules` as injectable `IMaintenanceRules` with `IOptions<MaintenanceSettings>` | ENG-2 | Intervals configurable per environment; default values match current constants | 1d |
| S6-04 | Implement `MaintenanceAlertJob` as Container Apps scheduled job (cron: every 6h) | ENG-2 | Job runs on schedule; sends notifications (Azure Communication Services or SendGrid); replaces Console.WriteLine stub | 2d |
| S6-05 | Implement event consumers: `VehicleCreated` (auto-create schedules), `VehicleOdometerUpdated` (mileage tracking) | ENG-2 | Service reacts to vehicle events; maintains local vehicle projection (VehicleId, Odometer) | 2d |
| S6-06 | Implement event publishers: `MaintenanceOverdue`, `WorkOrderCompleted` | ENG-2 | Events published on state changes; outbox pattern | 1d |
| S7-01 | Deploy Maintenance Service to Container Apps; configure APIM routing | SRE | `/api/maintenance/*` routes to new service | 1d |
| S7-02 | Data migration: ETL `MaintenanceSchedules` and `WorkOrders` tables | DBA | Row counts match; FK references to VehicleId preserved as local projection | 1d |
| S7-03 | Integration tests for Maintenance Service | QA | All maintenance API tests pass; work order lifecycle verified end-to-end | 2d |
| S7-04 | Traffic cutover: 10% → 50% → 100% via APIM | SRE | Error rate < 0.1%; p99 latency < 500ms | 1d |

### Sprint 8–9 — Fuel Management Service

| Task ID | Task | Owner | Acceptance Criteria | Effort |
|---------|------|-------|---------------------|--------|
| S8-01 | Port `FuelTransaction` entity and `FuelService` to .NET 8 | ENG-3 | Transaction processing, fraud detection, efficiency reporting all ported; unit tests | 3d |
| S8-02 | Port `FuelCardValidationRules` as injectable service with configurable limits | ENG-3 | $500 max, 100 gal max, 30-min window all configurable via `IOptions<FuelCardSettings>` | 1d |
| S8-03 | Implement real `IFuelCardAdapter` — integrate with fuel card processor sandbox API | ENG-3 | `AuthorizeTransaction`, `SettleTransaction`, `GetCardBalance` call real API; Polly retry + circuit breaker | 3d |
| S8-04 | Implement event consumers: `VehicleOdometerUpdated` (for MPG calc); local vehicle projection | ENG-3 | Fuel service maintains VehicleId → FuelType, Odometer projection | 1d |
| S8-05 | Implement event publishers: `FuelTransactionProcessed`, `FuelTransactionFlagged` | ENG-3 | Published on transaction completion/flagging; consumed by Vehicle Service | 1d |
| S9-01 | Deploy Fuel Service to Container Apps; configure APIM | SRE | `/api/fuel/*` routes to new service | 1d |
| S9-02 | Data migration: ETL `FuelTransactions` table | DBA | Verified with checksums; transaction amounts match to the cent | 1d |
| S9-03 | Integration + contract tests for Fuel Service | QA | API contract tests (Pact); fuel card adapter integration tests against sandbox; fraud detection edge cases | 2d |
| S9-04 | Traffic cutover | SRE | Canary deployment; zero financial discrepancies | 1d |

**Phase 2 Exit Criteria:** 3 microservices (Driver, Maintenance, Fuel) serving production traffic. Event bus proven with cross-service events. Monolith routes for these contexts deactivated.

### Phase 2 Rollback Strategy

| Risk | Rollback |
|------|----------|
| Maintenance Service data inconsistency | APIM redirect to monolith; monolith still has full maintenance tables and logic |
| Fuel card integration failures | Circuit breaker trips → Fuel Service returns 503; APIM falls back to monolith stub (degraded mode) |
| Event ordering issues | Service Bus sessions guarantee FIFO per entity; dead-letter monitoring with alerting |

---

## 7. Phase 3 — Service Extraction Wave 2 (Sprints 10–14)

> **Goal:** Extract high-complexity services (Compliance, Telematics) requiring external integrations and WCF migration.  
> **Duration:** 10 weeks (5 sprints)  
> **Team Focus:** ENG-1..4 (split across services), ARCH (compliance review)

### Sprint 10–11 — DOT Compliance Service

| Task ID | Task | Owner | Acceptance Criteria | Effort |
|---------|------|-------|---------------------|--------|
| S10-01 | Port `ComplianceService` to .NET 8; deduplicate `CheckDriverCompliance()` / `GetViolations()` logic | ENG-1 | Single compliance evaluation engine; no duplicated HOS logic; 100% unit test coverage on DOT rules | 3d |
| S10-02 | Port `DOTComplianceRules` as configurable service via Azure App Configuration | ENG-1 | All 6 federal constants configurable without redeployment; 8-day/70-hour cycle implemented (unused constant) | 2d |
| S10-03 | Implement real FMCSA ELD API integration (replacing `DOTReportingAdapter` stub) | ENG-1 | `SubmitHOSReport`, `SubmitInspectionReport`, `GetComplianceStatus` call real API; Polly resilience; sandbox verified | 4d |
| S10-04 | Legal review: verify HOS calculation correctness against 49 CFR Part 395 | ARCH | Written sign-off from legal/compliance team that implementation matches federal regulations | 2d |
| S10-05 | Implement `ComplianceCheckJob` as Container Apps scheduled job (daily) | ENG-1 | Daily batch scan of all active drivers; violations published as events | 1d |
| S11-01 | Deploy Compliance Service; parallel-run with monolith for 2 weeks | SRE + QA | Both old and new compliance checks run; results compared; < 0.01% divergence | 2d |
| S11-02 | Data migration: ETL `HOSLogs` table | DBA | All historical HOS data preserved; timestamps verified across timezone boundaries | 1d |
| S11-03 | Compliance-specific regression tests — 90-day parallel run validation | QA | Automated comparison of old vs. new compliance results for every active driver; divergences investigated and resolved | 3d |
| S11-04 | Traffic cutover after parallel validation | SRE | Only after QA sign-off on parallel-run results | 1d |

### Sprint 12–14 — Telematics & GPS Service (includes WCF→gRPC)

| Task ID | Task | Owner | Acceptance Criteria | Effort |
|---------|------|-------|---------------------|--------|
| S12-01 | Implement gRPC Telematics Service in .NET 8 from `.proto` definitions (Sprint 4) | ENG-3 | `StreamTelemetry` (bidirectional streaming), `GetVehicleStatus` (unary), `SendVehicleCommand` (unary) | 3d |
| S12-02 | Set up Azure Event Hubs for GPS ingestion (32 partitions, Standard tier) | SRE | Event Hub `gps-ingest` provisioned; throughput tested at 10K events/sec | 2d |
| S12-03 | Implement Event Hubs producer: gRPC → Event Hubs pipeline for incoming telemetry | ENG-3 | GPS data flows from gRPC stream to Event Hubs; back-pressure handling | 2d |
| S12-04 | Implement consumer group 1: Telematics hot store (Azure SQL — latest 24h per vehicle) | ENG-3 | Last N positions per vehicle queryable via REST; auto-cleanup of positions > 24h | 2d |
| S12-05 | Implement consumer group 2: Geofence evaluator (real-time violation check) | ENG-4 | Each GPS position evaluated against active geofences; violations published as events | 2d |
| S13-01 | Implement `SendCommand` via Azure IoT Hub (replacing stub) | ENG-3 | Engine disable, door lock commands dispatched to devices; acknowledgment tracking; timeout handling | 3d |
| S13-02 | Implement WCF→gRPC bridge proxy for legacy devices | ENG-4 | WCF SOAP envelope → gRPC call translation; devices don't need firmware update immediately | 2d |
| S13-03 | Deploy Telematics Service to Container Apps (min 2 replicas, scale to 20) | SRE | Autoscaling based on Event Hub partition count; health probes on gRPC | 1d |
| S13-04 | Implement REST API for GPS queries: `GET /api/gps/vehicle/{id}/positions`, `GET /api/gps/vehicle/{id}/latest` | ENG-3 | Paginated responses from hot store; latest position < 100ms p99 | 1d |
| S14-01 | Dual-write validation: WCF writes to old SQL, gRPC writes to Event Hubs; compare | QA | Zero data loss over 1-week dual-write period; row counts match ± tolerance | 3d |
| S14-02 | Implement `DataArchivalJob` (currently a no-op): archive to ADX warm tier | ENG-3 | Positions > 24h moved to ADX; positions > 90d moved to cold Blob storage | 2d |
| S14-03 | Traffic cutover: redirect devices to gRPC endpoint; maintain WCF bridge | SRE | Device traffic flowing through gRPC; WCF bridge handles legacy devices; zero position data loss | 2d |

**Phase 3 Exit Criteria:** 5 microservices running (Driver, Maintenance, Fuel, Compliance, Telematics). WCF bridge operational. Real external integrations for FMCSA and fuel card. Event-driven GPS ingestion pipeline live.

### Phase 3 Rollback Strategy

| Risk | Rollback |
|------|----------|
| Compliance calculation regression | Instant APIM redirect to monolith; 90-day parallel run validates before full cutover |
| gRPC incompatibility with devices | WCF→gRPC bridge proxy maintains backward compatibility; bridge is permanent until all devices upgraded |
| Event Hubs data loss | Event Hubs retention set to 7 days; consumer checkpoints enable replay; WCF dual-write provides fallback |
| IoT Hub command failures | Circuit breaker → commands queued; manual dispatch via operations console |

---

## 8. Phase 4 — Service Extraction Wave 3 (Sprints 15–17)

> **Goal:** Extract remaining services (Geofence, Vehicle). Vehicle is the most complex due to God class decomposition.  
> **Duration:** 6 weeks (3 sprints)  
> **Team Focus:** Full team

### Sprint 15 — Geofencing & Alerts Service

| Task ID | Task | Owner | Acceptance Criteria | Effort |
|---------|------|-------|---------------------|--------|
| S15-01 | Port `GeofenceService` to .NET 8; use Azure SQL spatial types (`geography`) replacing GeoJSON strings | ENG-4 | `STContains()` spatial queries; spatial index on geofence polygons; ray-casting replaced with SQL spatial | 3d |
| S15-02 | Implement real-time geofence evaluation consuming `VehiclePositionUpdated` events from Telematics Service | ENG-4 | Near-real-time (<5s latency) entry/exit detection; `GeofenceViolation` events published | 2d |
| S15-03 | Implement push notifications via Azure SignalR Service for fleet manager alerts | ENG-4 | Real-time geofence violation alerts in browser; configurable per fleet/geofence | 2d |
| S15-04 | Deploy Geofence Service; data migration of `Geofences` table (convert GeoJSON → spatial) | SRE + DBA | Geofence polygons verified post-conversion; spatial queries return correct results | 2d |
| S15-05 | Integration tests including spatial edge cases | QA | Point on boundary, point at vertex, concave polygons, anti-meridian crossing | 1d |

### Sprint 16–17 — Vehicle Management Service (God Class Decomposition)

| Task ID | Task | Owner | Acceptance Criteria | Effort |
|---------|------|-------|---------------------|--------|
| S16-01 | Decompose `VehicleService` (498 lines, 13 methods) into 5 focused services | ARCH + ENG-1 | `VehicleCrudService`, `VehicleAssignmentService`, `VehicleHealthService`, `VehicleUtilizationService`, `VehicleMaintenanceCostService` — each < 150 lines | 4d |
| S16-02 | Port `Vehicle`, `Fleet`, `Client` entities with EF Core; proper navigation properties | ENG-2 | Rich entities with behavior; `Fleet.AddVehicle()`, `Vehicle.AssignDriver()` etc.; inverse collections present | 2d |
| S16-03 | Port all `VehicleSpecifications` to EF Core query filters / LINQ expressions | ENG-2 | Specification pattern replaced with composable `IQueryable` extensions; server-side evaluation | 1d |
| S16-04 | Implement event consumers for all cross-context events (Vehicle is the highest-coupling service) | ENG-1 | Local projections for: Driver (assignment), Maintenance (health score), Fuel (odometer), GPS (utilization) | 3d |
| S17-01 | Deploy Vehicle Service to Container Apps; configure APIM for vehicle + fleet routes | SRE | `/api/vehicles/*` and `/api/fleets/*` routes to new service | 1d |
| S17-02 | Data migration: ETL `Vehicles`, `Fleets`, `Clients` tables | DBA | All vehicle data migrated; fleet-vehicle relationships intact; checksums verified | 2d |
| S17-03 | Comprehensive integration tests — vehicle lifecycle, driver assignment, health scoring | QA | Full CRUD + assignment + health + utilization + cost endpoints tested; results match monolith baseline | 3d |
| S17-04 | Traffic cutover with extended canary (10% → 25% → 50% → 100% over 2 weeks) | SRE | Extended ramp due to complexity; error rate < 0.05% at each stage | 2d |

**Phase 4 Exit Criteria:** All 7 microservices deployed and serving production traffic. Monolith receiving zero API traffic (only background jobs may still run temporarily). God class fully decomposed.

### Phase 4 Rollback Strategy

| Risk | Rollback |
|------|----------|
| God class decomposition breaks vehicle logic | Extended canary (2 weeks); instant APIM redirect to monolith; monolith retains full VehicleService |
| Spatial type migration corrupts geofence polygons | Pre-migration snapshot of Geofences table; validation script compares old GeoJSON with new spatial results |
| Vehicle event consumer misses updates | Reconciliation job runs hourly; compares Vehicle Service projections with source-of-truth services; alerts on drift |

---

## 9. Phase 5 — Event-Driven GPS Pipeline (Sprints 16–18)

> **Goal:** Implement full data tiering (hot/warm/cold) for 100M+ GPS records.  
> **Duration:** 6 weeks (3 sprints, overlaps with Phase 4)  
> **Team Focus:** ENG-3, DBA, SRE

*Note: This phase runs in parallel with Sprints 16–17 of Phase 4.*

### Sprint 16 (parallel) — Warm & Cold Tier Setup

| Task ID | Task | Owner | Acceptance Criteria | Effort |
|---------|------|-------|---------------------|--------|
| S16-G1 | Provision Azure Data Explorer (ADX) cluster; create `GPSPositions` table with time-based partitioning | SRE + DBA | ADX cluster operational; ingestion pipeline from Event Hubs configured | 2d |
| S16-G2 | Implement consumer group 3: ADX ingestor (warm tier, 1–90 days) | ENG-3 | GPS positions flow from Event Hubs → ADX; queryable within 30 seconds of ingestion | 2d |
| S16-G3 | Configure ADX retention: auto-export to Azure Blob Storage (Parquet) after 90 days | DBA | Cold tier data accessible via ADX external tables; Parquet files in Blob | 1d |

### Sprint 17 (parallel) — Historical Data Backfill

| Task ID | Task | Owner | Acceptance Criteria | Effort |
|---------|------|-------|---------------------|--------|
| S17-G1 | Backfill warm tier: migrate last 90 days of GPS data from monolith SQL to ADX | DBA | Row count matches; timestamp-based checksums; queries return equivalent results | 3d |
| S17-G2 | Backfill cold tier: export 90+ day GPS data to Parquet in Blob Storage | DBA | All historical data preserved; Parquet files partitioned by month/vehicle | 2d |
| S17-G3 | Validate data completeness: automated comparison queries (monolith SQL vs. ADX+Blob) | QA | < 0.001% row discrepancy; latency/longitude precision within 6 decimal places | 2d |

### Sprint 18 — GPS Cutover & Decommission

| Task ID | Task | Owner | Acceptance Criteria | Effort |
|---------|------|-------|---------------------|--------|
| S18-G1 | Cut over GPS reads: all GPS queries go to Telematics Service (hot) + ADX (warm/cold) | SRE | Monolith GPS queries redirected; response time < 200ms for latest position; < 2s for 90-day history | 1d |
| S18-G2 | Stop dual-write to monolith `GPSPositions` table | ENG-3 | Monolith no longer receives GPS writes; all ingestion through Event Hubs | 0.5d |
| S18-G3 | 30-day observation period: monitor data completeness, query performance, ingestion lag | QA + SRE | Dashboard tracking: ingestion lag < 5s, query error rate < 0.01%, no data gaps | Ongoing |
| S18-G4 | Drop `GPSPositions` table from monolith SQL Server (after 30-day observation) | DBA | Table dropped; disk space reclaimed; monolith database size reduced significantly | 0.5d |

---

## 10. Phase 6 — Database Modernization (Sprints 17–19)

> **Goal:** Complete database-per-service separation; optimize each service's database.  
> **Duration:** 6 weeks (3 sprints, overlaps with Phase 4/5)  
> **Team Focus:** DBA, SRE, QA

### Sprint 17 (parallel) — Schema Finalization

| Task ID | Task | Owner | Acceptance Criteria | Effort |
|---------|------|-------|---------------------|--------|
| S17-D1 | Finalize indexes per service database (based on production query patterns from App Insights) | DBA | Top 10 slow queries per service have covering indexes; query plans verified | 2d |
| S17-D2 | Enable SQL Server temporal tables for audit on `Vehicles`, `Drivers`, `WorkOrders` | DBA | History tables track all changes; `AS OF` queries functional | 1d |
| S17-D3 | Configure row-level security using `SESSION_CONTEXT` for FleetId/ClientId isolation | DBA | Multi-tenant queries automatically filtered; cross-tenant access prevented | 2d |

### Sprint 18 (parallel) — Data Integrity & Performance

| Task ID | Task | Owner | Acceptance Criteria | Effort |
|---------|------|-------|---------------------|--------|
| S18-D1 | Run cross-service referential integrity validation (e.g., VehicleId in Maintenance DB exists in Vehicle DB) | QA + DBA | Zero orphaned references; reconciliation report clean | 2d |
| S18-D2 | Load test each service database at 3x expected production traffic | QA | p99 < 100ms for simple queries; p99 < 500ms for complex aggregations; no connection pool exhaustion | 2d |
| S18-D3 | Configure Azure SQL automated backups, geo-replication for critical databases (Compliance, Vehicle) | SRE | RPO < 5 min; RTO < 1 hour; geo-secondary in paired region | 1d |

### Sprint 19 — Final Validation & Encryption

| Task ID | Task | Owner | Acceptance Criteria | Effort |
|---------|------|-------|---------------------|--------|
| S19-D1 | Enable Always Encrypted for PII columns (driver license numbers, SSN if stored) | DBA | Encrypted at rest and in transit; application-level decryption verified | 2d |
| S19-D2 | Final reconciliation: compare all service databases with monolith snapshot | QA + DBA | 100% data parity; sign-off from each service team lead | 2d |
| S19-D3 | Performance baseline: capture metrics for all service databases under production load | SRE | Baseline documented; alerting thresholds set at 2x baseline | 1d |

---

## 11. Phase 7 — Decommission Monolith (Sprint 20)

> **Goal:** Safely decommission the legacy monolith after all services are proven in production.  
> **Duration:** 2 weeks (1 sprint)  
> **Prerequisite:** All 7 services running for 30+ days with zero critical incidents

### Sprint 20 — Decommission

| Task ID | Task | Owner | Acceptance Criteria | Effort |
|---------|------|-------|---------------------|--------|
| S20-01 | Remove monolith from APIM routing (zero traffic) | SRE | APIM configuration has no routes to monolith; all traffic to microservices | 0.5d |
| S20-02 | Shut down monolith IIS application | SRE | Process terminated; compute resources released; monitoring confirms zero traffic | 0.5d |
| S20-03 | Final backup of monolith SQL Server database | DBA | Full backup stored in Azure Blob with 1-year retention | 0.5d |
| S20-04 | Drop remaining tables from monolith database (after 30-day observation per table) | DBA | Only empty/unused tables remain; storage reclaimed | 1d |
| S20-05 | Archive monolith source code (tag `legacy-final` in Git) | ARCH | Git tag created; README updated with decommission date; branch protected | 0.5d |
| S20-06 | Update all documentation: ARCHITECTURE.md, README.md, runbooks | ARCH + QA | Documentation reflects microservices architecture; no references to monolith endpoints | 1d |
| S20-07 | Decommission celebration & retrospective | All | Lessons-learned document published; celebration held 🎉 | 0.5d |

---

## 12. Dependency Map

### 12.1 Task-Level Dependencies

```
S1 (Auth+Logging) ──────────────────────────────────┐
S2 (Bug Fixes) ─────────────────────────────────────┤
S3 (Infra+Template) ─────┐                          │
                          ▼                          ▼
               S4 (Driver .NET 8 port) ──► S5 (Driver Deploy) ──┐
                                                                 │
                          ┌──────────────────────────────────────┘
                          ▼
               S6-7 (Maintenance) ──► deployed ──┐
               S8-9 (Fuel) ─────────► deployed ──┤
                                                  ▼
               S10-11 (Compliance) ──► deployed (after 90-day parallel) ──┐
               S12-14 (Telematics) ──► deployed (WCF bridge) ────────────┤
                                                                          ▼
               S15 (Geofence) ──► deployed ──┐  (depends on Telematics GPS events)
                                              ▼
               S16-17 (Vehicle) ──► deployed (depends on ALL other services)
                                              │
               S16-18 (GPS Pipeline) ────────►│  (parallel with Vehicle)
               S17-19 (DB Modernize) ────────►│  (parallel)
                                              ▼
               S20 (Decommission Monolith)
```

### 12.2 Service-Level Dependencies

| Service | Depends On (Must Deploy First) | Publishes Events For |
|---------|-------------------------------|---------------------|
| Driver | Phase 0 | Vehicle, Compliance |
| Maintenance | Driver (event bus patterns) | Vehicle |
| Fuel | Driver | Vehicle |
| Compliance | Driver | Driver, Vehicle |
| Telematics | WCF→gRPC bridge (Phase 3) | Geofence, Vehicle, Maintenance, Fuel |
| Geofence | Telematics (GPS event stream) | Vehicle |
| Vehicle | ALL other services | (aggregates from all) |

---

## 13. Priority Matrix

### 13.1 Business Value vs. Technical Risk

Tasks are ordered by a composite score: `Priority = BusinessValue × 0.6 + TechnicalRisk × 0.4`

| Rank | Task / Service | Business Value | Technical Risk | Priority Score | Sprint |
|------|---------------|:-:|:-:|:-:|:-:|
| 1 | **Authentication & Authorization** | 🔴 Critical (10) | 🟢 Low (3) | **8.8** | S1 |
| 2 | **Fix Geofence spatial logic** | 🔴 Critical (10) | 🟢 Low (2) | **6.8** | S2 |
| 3 | **Structured Logging** | 🟠 High (8) | 🟢 Low (2) | **5.6** | S1 |
| 4 | **Repository pagination** | 🔴 Critical (9) | 🟢 Low (2) | **6.2** | S2 |
| 5 | **DOT Compliance Service** | 🔴 Critical (10) | 🟠 High (8) | **9.2** | S10–11 |
| 6 | **Driver Service extraction** | 🟡 Medium (6) | 🟢 Low (3) | **4.8** | S4–5 |
| 7 | **Fuel Service (real integration)** | 🟠 High (8) | 🟡 Medium (5) | **6.8** | S8–9 |
| 8 | **Telematics/GPS Service** | 🟠 High (8) | 🟠 High (8) | **8.0** | S12–14 |
| 9 | **Vehicle Service (God class)** | 🟠 High (7) | 🔴 Very High (9) | **7.8** | S16–17 |
| 10 | **GPS data migration (100M+)** | 🟠 High (8) | 🟠 High (8) | **8.0** | S16–18 |

### 13.2 Priority Rules

1. **Security before features** — auth is Sprint 1 regardless of other priorities
2. **Critical bugs before migration** — geofence, pagination, service-layer bypass
3. **Low-risk services first** — Driver proves the pattern before tackling Compliance or Telematics
4. **External integrations early** — engage FMCSA and fuel card vendors during Wave 1
5. **God class last** — Vehicle Service depends on all others publishing events
6. **Data migration in parallel** — GPS pipeline runs alongside service extraction

---

## 14. Rollback Strategies

### 14.1 Architecture-Level Rollback

The Strangler Fig pattern provides inherent rollback capability at every stage:

```
                          APIM / API Gateway
                         /                  \
                        /                    \
              ┌──────────────┐        ┌──────────────┐
              │ Microservice │        │  Monolith     │
              │ (new)        │        │  (legacy)     │
              └──────────────┘        └──────────────┘
                                              ▲
                                              │
                                     ROLLBACK: redirect
                                     traffic back via APIM
                                     (< 1 minute)
```

### 14.2 Per-Phase Rollback Summary

| Phase | Rollback Mechanism | RTO | Data Impact |
|-------|-------------------|-----|-------------|
| **Phase 0** (Foundation) | Git revert + redeploy monolith | < 30 min | None — additive changes only |
| **Phase 1** (Driver) | APIM redirect to monolith | < 1 min | Dual-write keeps both DBs in sync |
| **Phase 2** (Maint + Fuel) | APIM redirect per service | < 1 min | Per-service; monolith retains full data |
| **Phase 3** (Compliance) | APIM redirect; 90-day parallel run catches issues early | < 1 min | Monolith compliance checks still running during parallel period |
| **Phase 3** (Telematics) | WCF bridge continues accepting traffic; gRPC is additive | < 5 min | Event Hubs retention (7d) enables replay |
| **Phase 4** (Geofence) | APIM redirect | < 1 min | Spatial type conversion reversible from backup |
| **Phase 4** (Vehicle) | Extended canary; APIM redirect | < 1 min | Monolith VehicleService untouched |
| **Phase 5** (GPS Pipeline) | Continue writing to monolith SQL (dual-write) | < 5 min | 30-day observation before dropping old table |
| **Phase 6** (DB Modernize) | Azure SQL point-in-time restore | < 1 hour | RPO < 5 min with continuous backup |
| **Phase 7** (Decommission) | Restore monolith from backup + redeploy | < 2 hours | Full backup retained for 1 year |

### 14.3 Rollback Decision Criteria

Automatic rollback triggers (any one):

| Metric | Threshold | Action |
|--------|-----------|--------|
| Error rate (5xx) | > 1% for 5 minutes | APIM redirects traffic to monolith |
| p99 latency | > 3x baseline for 10 minutes | Alert + manual decision |
| Data divergence | > 0.01% mismatch in reconciliation | Stop traffic ramp; investigate |
| Event bus lag | > 5 minutes consumer lag | Alert + manual decision |
| Compliance divergence | Any mismatch in HOS calculations | Immediate rollback + investigation |

---

## 15. Testing Strategy

### 15.1 Testing Pyramid

```
                    ╱╲
                   ╱  ╲
                  ╱ E2E╲          5% — Smoke tests against production
                 ╱──────╲         (health, auth, critical paths)
                ╱Contract╲
               ╱──────────╲       15% — Pact contract tests between services
              ╱ Integration ╲
             ╱──────────────╲     30% — Service + DB integration tests
            ╱   Unit Tests    ╲
           ╱──────────────────╲   50% — Business rule + domain logic
          ╱────────────────────╲
```

### 15.2 Unit Tests

| Scope | Framework | Coverage Target | Key Areas |
|-------|-----------|----------------|-----------|
| Business rules | xUnit + FluentAssertions | **100%** for DOT compliance, fuel fraud, maintenance rules | Every federal regulation constant; every fraud detection threshold; every interval calculation |
| Domain entities | xUnit | **90%+** | Behavior methods (e.g., `Driver.Terminate()`, `Vehicle.AssignDriver()`); value objects |
| Services | xUnit + Moq/NSubstitute | **80%+** | All public methods; edge cases (null vehicle, expired license, max limits) |
| Specifications/Query filters | xUnit | **90%+** | Each specification with positive and negative cases |

**Unit Test Guidelines:**
- Use `ITimeProvider` with `FakeTimeProvider` for all time-dependent tests
- Use `InMemoryDatabase` for EF Core query filter tests (NOT for business logic — mock repos instead)
- DOT compliance rules must have tests for each federal limit (11h, 14h, 60h, 10h off-duty, 30min break, 8-day/70h)
- Fuel fraud detection must test boundary conditions (exactly $500, exactly 30 min, same-state edge)

### 15.3 Integration Tests

| Scope | Framework | Approach |
|-------|-----------|----------|
| Service + Database | xUnit + Testcontainers (SQL Server) | Each service tested with real SQL Server in Docker; EF Core migrations applied; test data seeded |
| Service + Event Bus | xUnit + Azure Service Bus emulator | Verify event publishing and consumption; test dead-letter scenarios |
| Service + External APIs | xUnit + WireMock | Mock FMCSA and fuel card APIs; test Polly retry/circuit breaker behavior |
| Cross-service flows | xUnit + Docker Compose | Multi-service integration: e.g., create vehicle → maintenance schedule auto-created |

### 15.4 Contract Tests (Pact)

| Provider | Consumer | Contract |
|----------|----------|----------|
| Driver Service | Vehicle Service | `GET /api/drivers/{id}` returns `{driverId, name, cdlClass, status}` |
| Driver Service | Compliance Service | `GET /api/drivers/{id}` returns `{driverId, licenseExpiry}` |
| Vehicle Service | Maintenance Service | `VehicleCreated` event contains `{vehicleId, vin, fleetId}` |
| Vehicle Service | Fuel Service | `VehicleOdometerUpdated` event contains `{vehicleId, odometer}` |
| Telematics Service | Geofence Service | `VehiclePositionUpdated` event contains `{vehicleId, lat, lon, timestamp}` |
| Fuel Service | Vehicle Service | `FuelTransactionProcessed` event contains `{vehicleId, gallons, amount}` |

**Contract Test Process:**
1. Consumer generates Pact file defining expected interactions
2. Provider verifies Pact file in CI pipeline
3. Pact Broker stores contracts; prevents breaking changes from being deployed

### 15.5 End-to-End Tests

| Scenario | Steps | Validation |
|----------|-------|------------|
| **Vehicle lifecycle** | Create vehicle → assign driver → record GPS → check health score | Health score reflects maintenance status |
| **DOT compliance flow** | Record duty status → check compliance → detect violation → report | Violation matches federal rules; report submitted to FMCSA |
| **Fuel fraud detection** | Submit transaction → submit another within 30 min same state → verify flagged | Transaction flagged as suspicious; alert event published |
| **Geofence violation** | Create geofence → send GPS position outside boundary → verify alert | SignalR notification received by fleet manager dashboard |
| **Maintenance scheduling** | Create vehicle → advance odometer → check overdue → complete work order | Next service recalculated; health score updated |

**E2E Test Environment:**
- Dedicated staging environment on Container Apps
- Synthetic data (not production data)
- Runs nightly and on-demand before production releases
- Includes chaos testing (kill random service instances; verify resilience)

### 15.6 Regression Test Strategy During Migration

| Phase | Strategy |
|-------|----------|
| Phase 0 (Foundation) | Run baseline integration tests after each change; no behavior should change |
| Phase 1–4 (Service extraction) | Run monolith baseline tests + new service tests; both must pass simultaneously |
| Phase 3 (Compliance) | 90-day parallel run: old and new compliance services compared for every driver, every day |
| Phase 5 (GPS) | Dual-write with data comparison: query results from old SQL vs. new ADX must match |
| Phase 7 (Decommission) | Final full E2E regression suite run; sign-off from each team lead |

---

## 16. Data Migration — GPS 100M+ Records

### 16.1 Challenge

The `GPSPositions` table contains **100M+ rows** in a single SQL Server table with no partitioning, no archival, and no indexes beyond the primary key. This is the single largest data migration challenge in the entire modernization.

### 16.2 Data Tiering Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                      GPS Data Lifecycle                              │
│                                                                     │
│  INGEST           HOT (0-24h)        WARM (1-90d)      COLD (90d+) │
│  ───────          ──────────         ───────────       ──────────── │
│                                                                     │
│  Device ──►       Azure SQL           Azure Data       Azure Blob   │
│  gRPC/MQTT ──►    (latest N per       Explorer         Storage      │
│  Event Hubs ──►   vehicle)            (ADX)            (Parquet)    │
│                                                                     │
│  Latency:         < 100ms query       < 2s query       Batch only  │
│  Volume:          ~50K rows           ~45M rows        ~55M+ rows  │
│  Retention:       Auto-cleanup        90-day policy    Indefinite  │
└─────────────────────────────────────────────────────────────────────┘
```

### 16.3 Migration Steps

| Step | Action | Duration | Rollback |
|------|--------|----------|----------|
| **Step 1** | Deploy Event Hubs + new Telematics Service alongside existing WCF | 1 week | Remove new service; no impact on existing |
| **Step 2** | Dual-write: WCF continues writing to monolith SQL; new service writes to Event Hubs + hot store | 1 week | Stop new service writes; monolith unaffected |
| **Step 3** | Backfill warm tier: migrate last 90 days from monolith SQL → ADX using Azure Data Factory | 2–3 days | Drop ADX table; re-run if needed |
| **Step 4** | Backfill cold tier: export 90+ day data to Parquet in Blob Storage using ADF | 2–3 days | Delete Parquet files; re-export |
| **Step 5** | Validate: automated row count + checksum comparison (monolith vs. ADX+Blob) | 1 week | Fix discrepancies; re-run affected steps |
| **Step 6** | Cut over reads: GPS queries served from Telematics Service (hot) + ADX (warm/cold) | 1 day | Redirect reads back to monolith SQL |
| **Step 7** | Cut over writes: devices redirect to gRPC; WCF bridge for legacy devices | 1 day | Revert device config; WCF still operational |
| **Step 8** | 30-day observation: monitor for data gaps, latency, completeness | 30 days | Dual-write continues during observation |
| **Step 9** | Drop `GPSPositions` table from monolith SQL | 1 day | Restore from backup (full backup taken pre-drop) |

### 16.4 Performance Estimates

| Operation | Approach | Estimated Duration | Throughput |
|-----------|----------|-------------------|------------|
| Backfill 90-day warm (ADX) | ADF pipeline → ADX ingestion | 4–8 hours | ~500K rows/min |
| Backfill cold (Blob Parquet) | ADF pipeline → Blob export | 2–4 hours | ~1M rows/min (sequential write) |
| Validation checksums | SQL vs. ADX aggregation queries | 1–2 hours | Per-day batches |
| Hot store seeding | Latest 1000 positions per vehicle from SQL | 30 min | Single query with TOP per vehicle |

### 16.5 Data Validation Queries

```sql
-- Monolith: count by day for last 90 days
SELECT CAST(Timestamp AS DATE) AS Day, COUNT(*) AS RowCount
FROM GPSPositions
WHERE Timestamp > DATEADD(DAY, -90, GETUTCDATE())
GROUP BY CAST(Timestamp AS DATE)
ORDER BY Day;

-- ADX: same query for comparison
GPSPositions
| where Timestamp > ago(90d)
| summarize RowCount = count() by bin(Timestamp, 1d)
| order by Timestamp asc;

-- Acceptance: < 0.001% discrepancy per day
```

---

## 17. Timeline & Milestones

### 17.1 Gantt Overview (20 Sprints / 40 Weeks)

```
Sprint  1  2  3  4  5  6  7  8  9  10 11 12 13 14 15 16 17 18 19 20
Week    1--3--5--7--9--11-13-15-17-19-21-23-25-27-29-31-33-35-37-39

Phase 0 ████████████                                               Foundation
Phase 1              ████████                                      Platform
Phase 2                       ████████████████                     Wave 1
Phase 3                                         ██████████████████ Wave 2
Phase 4                                                     ██████████████ Wave 3
Phase 5                                                     ██████████████ GPS
Phase 6                                                        █████████████ DB
Phase 7                                                                 ████ Decomm

Milestones:
  ▼ M1 (S3): Hardened monolith                    ▲ Week 6
  ▼ M2 (S5): First microservice in production     ▲ Week 10
  ▼ M3 (S9): 3 services live                      ▲ Week 18
  ▼ M4 (S11): Compliance service live              ▲ Week 22
  ▼ M5 (S14): Telematics live, WCF decommissioned ▲ Week 28
  ▼ M6 (S17): All 7 services live                  ▲ Week 34
  ▼ M7 (S18): GPS migration complete               ▲ Week 36
  ▼ M8 (S20): Monolith decommissioned              ▲ Week 40
```

### 17.2 Milestone Details

| ID | Milestone | Sprint | Week | Gate Criteria |
|----|-----------|--------|------|---------------|
| **M1** | Hardened Monolith | S3 | 6 | Auth on all endpoints; structured logging; critical bugs fixed; baseline tests green; Azure infra provisioned |
| **M2** | First Microservice Live | S5 | 10 | Driver Service on Container Apps serving 100% traffic; event bus operational; CI/CD pipeline proven; canary deployment validated |
| **M3** | Wave 1 Complete | S9 | 18 | Driver + Maintenance + Fuel services live; 3 service databases operational; cross-service events flowing |
| **M4** | Compliance Live | S11 | 22 | DOT Compliance Service live after 90-day parallel run; real FMCSA integration; legal sign-off |
| **M5** | Telematics Live | S14 | 28 | gRPC endpoint receiving device traffic; Event Hubs processing GPS; WCF→gRPC bridge for legacy devices; IoT Hub for commands |
| **M6** | All Services Live | S17 | 34 | All 7 microservices serving production traffic; monolith receiving zero API traffic; Vehicle God class fully decomposed |
| **M7** | GPS Migration Complete | S18 | 36 | 100M+ GPS records in hot/warm/cold tiers; monolith GPS table read-only; ADX queries validated |
| **M8** | Monolith Decommissioned | S20 | 40 | Monolith shut down; databases archived; documentation updated; legacy code tagged |

### 17.3 Key Decision Points

| Decision Point | Sprint | Decision | Decision Maker |
|----------------|--------|----------|----------------|
| Proceed with extraction after Phase 0? | S3 | Go/No-Go based on test results and bug fix validation | ARCH + Product Owner |
| Driver Service pattern validated? | S5 | Confirm Strangler Fig approach works; adjust if needed | ARCH |
| Compliance parallel-run acceptable? | S11 | Sign-off on HOS accuracy; legal review complete | ARCH + Legal |
| WCF bridge sufficient for legacy devices? | S14 | Determine if firmware updates needed; timeline for WCF retirement | ARCH + Device Vendor |
| GPS data migration complete? | S18 | Validate 100% data completeness; approve old table drop | DBA + ARCH |
| Ready to decommission monolith? | S20 | All services stable for 30+ days; zero critical incidents | All leads |

---

## 18. Risk Register

| ID | Risk | Likelihood | Impact | Phase | Mitigation | Contingency |
|----|------|:---:|:---:|:---:|-----------|------------|
| R-01 | God Class decomposition breaks vehicle logic | High | Critical | 4 | Extract gradually; comprehensive integration tests; extended 2-week canary | Rollback to monolith VehicleService via APIM |
| R-02 | GPS data migration causes data loss (100M+ rows) | Medium | Critical | 5 | Dual-write pattern; automated checksum validation; 30-day observation | Continue serving from monolith SQL; re-run migration |
| R-03 | OBD-II device firmware incompatible with gRPC | Medium | High | 3 | WCF→gRPC bridge proxy; coordinate with vendor 6+ months in advance | Maintain WCF bridge indefinitely |
| R-04 | Federal DOT compliance regression | Medium | Critical | 3 | 100% unit test coverage on DOT rules; 90-day parallel run; legal review | Instant rollback; legal notification |
| R-05 | Event bus data consistency (lost/duplicate events) | Medium | High | 2–4 | Outbox pattern; idempotent consumers; dead-letter monitoring; reconciliation jobs | Manual event replay from outbox table |
| R-06 | Team lacks .NET 8 / Container Apps / gRPC experience | High | Medium | All | 2-week training sprint (Phase 0); start with Driver (lowest complexity); pair programming | Engage Microsoft FastTrack or external consultants |
| R-07 | Integration stubs mask real API issues (DOT, Fuel Card) | High | High | 3 | Engage with FMCSA and fuel card APIs early (Phase 2); sandbox testing | Maintain stubs with manual processes as fallback |
| R-08 | Shared Kernel creates tight coupling | Medium | High | 2–4 | Eventual consistency via events; local read projections; accept ~seconds delay | Fall back to synchronous API calls with circuit breaker |
| R-09 | Performance regression (N network hops) | Medium | Medium | 2–4 | Dapr service invocation; Redis caching; App Insights latency monitoring | Optimize hot paths; combine services if latency unacceptable |
| R-10 | Azure cost overrun (7 DBs + Event Hubs + ADX) | Medium | Medium | 5–6 | Elastic Pools; consumption-tier Container Apps; reserved capacity for prod | Consolidate databases; downgrade ADX tier |
| R-11 | WCF bridge becomes permanent dependency | Medium | Medium | 3 | Set firm deadline for device firmware updates; track adoption | Accept WCF bridge as permanent infrastructure |
| R-12 | Scope creep during God class decomposition | High | Medium | 4 | Fixed sprint scope; decompose VehicleService in pre-planned splits only | Defer non-essential refactoring to post-migration backlog |

---

## Appendix A — Domain Events Catalog

| Event | Publisher | Consumers | Schema |
|-------|-----------|-----------|--------|
| `VehicleCreated` | Vehicle Service | Maintenance, Telematics | `{vehicleId, vin, fleetId, fuelType}` |
| `VehicleUpdated` | Vehicle Service | All services with Vehicle projection | `{vehicleId, changedFields{}}` |
| `VehicleDeactivated` | Vehicle Service | Maintenance, Telematics | `{vehicleId}` |
| `VehicleOdometerUpdated` | Telematics Service | Maintenance, Fuel | `{vehicleId, newOdometer, timestamp}` |
| `VehiclePositionUpdated` | Telematics Service | Geofencing, Vehicle | `{vehicleId, lat, lon, speed, heading, timestamp}` |
| `DriverCreated` | Driver Service | Vehicle, Compliance | `{driverId, name, cdlClass, licenseExpiry}` |
| `DriverTerminated` | Driver Service | Vehicle, Compliance | `{driverId}` |
| `DriverSuspended` | Driver Service | Vehicle | `{driverId}` |
| `MaintenanceOverdue` | Maintenance Service | Vehicle | `{vehicleId, scheduleId, serviceType}` |
| `WorkOrderCompleted` | Maintenance Service | Vehicle | `{vehicleId, workOrderId, actualCost}` |
| `FuelTransactionProcessed` | Fuel Service | Vehicle | `{vehicleId, gallons, amount, odometerReading}` |
| `FuelTransactionFlagged` | Fuel Service | Vehicle, Driver | `{transactionId, vehicleId, driverId, reason}` |
| `ComplianceViolationDetected` | Compliance Service | Driver, Vehicle | `{driverId, violationType, severity}` |
| `GeofenceViolation` | Geofence Service | Vehicle, Fleet Manager | `{vehicleId, geofenceId, violationType, timestamp}` |

---

## Appendix B — Definition of Done

A sprint task is **Done** when ALL of the following are true:

- [ ] Code reviewed and approved by at least 1 peer
- [ ] Unit tests written and passing (coverage meets threshold for the area)
- [ ] Integration tests passing in CI pipeline
- [ ] Contract tests (Pact) passing for any API changes
- [ ] No new critical or high-severity issues introduced (SonarQube gate)
- [ ] Swagger/OpenAPI docs updated for any API changes
- [ ] Structured logging added for key operations (no `Console.WriteLine`)
- [ ] Health check endpoint operational
- [ ] Deployed to staging environment via CI/CD
- [ ] QA sign-off on staging
- [ ] Monitoring dashboards updated (if new service)
- [ ] Runbook updated (if operational change)
- [ ] Feature flag configured (if applicable)
- [ ] Rollback procedure documented and tested

---

*This migration plan was generated from deep analysis of SPEC2CLOUD-ANALYSIS.md (47 technical debt items, 7 bounded contexts) and MODERNIZATION-PATHS.md (7-phase roadmap, 52–74 person-week estimate). All estimates assume a team familiar with .NET and Azure; adjust upward by 30–50% if significant training is required.*
