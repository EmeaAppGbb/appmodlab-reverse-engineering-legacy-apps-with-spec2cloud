# TransFleet Architecture Specification

> **Version:** 1.0  
> **Date:** April 2026  
> **System:** TransFleet Fleet Management Platform  
> **Framework:** .NET Framework 4.7.2  

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [System Context (Level 1)](#2-system-context-level-1)
3. [Container Diagram (Level 2)](#3-container-diagram-level-2)
4. [Component Diagram — TransFleet.Core (Level 3)](#4-component-diagram--transfleetcore-level-3)
5. [Technology Stack](#5-technology-stack)
6. [Communication Patterns](#6-communication-patterns)
7. [Data Flow — Key Business Processes](#7-data-flow--key-business-processes)
8. [Domain Model](#8-domain-model)
9. [Known Technical Debt](#9-known-technical-debt)

---

## 1. Executive Summary

TransFleet is a monolithic fleet management system built on .NET Framework 4.7.2 using a layered architecture. It manages vehicle fleets, driver assignments, DOT/FMCSA compliance, fuel transactions, maintenance scheduling, geofencing, and real-time telematics. The system exposes a REST API (ASP.NET Web API 2) for client applications, a WCF/SOAP endpoint for telematics device communication, and runs background jobs (Hangfire/Quartz.NET) for compliance checking, maintenance alerts, and data archival.

---

## 2. System Context (Level 1)

The following describes the system context — TransFleet and all external actors and systems it interacts with.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          EXTERNAL SYSTEMS & ACTORS                         │
│                                                                             │
│  ┌──────────────┐   ┌──────────────────┐   ┌───────────────────────────┐   │
│  │  Fleet        │   │  Telematics       │   │  DOT / FMCSA              │   │
│  │  Managers     │   │  Devices (OBD-II, │   │  ELD Reporting System     │   │
│  │  (Web/Mobile) │   │  GPS Trackers)    │   │  (Federal Motor Carrier   │   │
│  └──────┬───────┘   └────────┬──────────┘   │   Safety Administration)  │   │
│         │ REST/JSON          │ SOAP/WCF     └─────────┬─────────────────┘   │
│         │                    │                        │ HTTPS/REST           │
│  ┌──────┴───────┐   ┌───────┴──────────┐   ┌────────┴──────────────────┐   │
│  │  Dispatch     │   │  Vehicle          │   │  Fuel Card Processor      │   │
│  │  Operators    │   │  Maintenance      │   │  (WEX / Comdata /         │   │
│  │              │   │  Technicians      │   │   Fuelman)                │   │
│  └──────┬───────┘   └──────────────────┘   └────────┬──────────────────┘   │
│         │ REST/JSON                                  │ HTTPS/REST           │
└─────────┼────────────────────────────────────────────┼──────────────────────┘
          │                                            │
          ▼                                            ▼
    ┌─────────────────────────────────────────────────────────┐
    │                                                         │
    │               T R A N S F L E E T                       │
    │           Fleet Management Platform                     │
    │                                                         │
    │   • Vehicle & Fleet CRUD                                │
    │   • Driver Management & Assignment                      │
    │   • DOT HOS Compliance Tracking                         │
    │   • Fuel Transaction Processing & Fraud Detection       │
    │   • Maintenance Scheduling & Work Orders                │
    │   • Geofencing & Alert Management                       │
    │   • Real-time GPS Telematics Ingestion                  │
    │   • Vehicle Health Scoring & Utilization Reporting       │
    │                                                         │
    └─────────────────────────────────────────────────────────┘
                          │
                          ▼
                ┌───────────────────┐
                │  SQL Server 2014+ │
                │  (TransFleet DB)  │
                └───────────────────┘
```

### 2.1 External Systems

| External System | Integration Type | Direction | Purpose |
|---|---|---|---|
| **DOT / FMCSA ELD System** | HTTPS/REST (stub) | Outbound | Submit Hours-of-Service (HOS) reports, inspection reports; query carrier compliance status |
| **Fuel Card Processor** | HTTPS/REST (stub) | Bidirectional | Authorize fuel transactions, settle payments, query card balances |
| **Telematics Devices** | WCF/SOAP | Inbound | Receive real-time GPS positions, speed, heading, odometer, fuel level, engine temperature, ignition status |
| **Fleet Managers / Dispatchers** | REST/JSON (Web API) | Bidirectional | Manage vehicles, drivers, fleets, compliance reports, fuel reports, geofences |

### 2.2 External Integration Details

#### DOT/FMCSA Integration (`IDOTReportingAdapter`)
- **Endpoint:** Configured via `DOTReportingEndpoint` app setting (e.g., `https://fmcsa.example.gov/api`)
- **Carrier ID:** Configured via `CarrierNumber` app setting (e.g., `MC123456`)
- **Operations:**
  - `SubmitHOSReport(DOTHOSReport)` → `DOTSubmissionResponse` — Submit driver HOS daily logs
  - `SubmitInspectionReport(DOTInspectionReport)` → `DOTInspectionResponse` — Submit vehicle inspection results
  - `GetComplianceStatus(carrierNumber)` → `DOTComplianceStatus` — Query carrier safety rating
- **Status:** Stub implementation returning hardcoded "Satisfactory" responses

#### Fuel Card Processor Integration (`IFuelCardAdapter`)
- **Endpoint:** Configured via `FuelCardServiceEndpoint` app setting (e.g., `https://fuelcard.example.com/api`)
- **Operations:**
  - `AuthorizeTransaction(FuelCardAuthorizationRequest)` → `FuelCardAuthorizationResponse` — Pre-authorize fuel purchase
  - `SettleTransaction(authCode, amount)` → `FuelCardSettlementResponse` — Settle after fueling completes
  - `GetCardBalance(cardNumber)` → `FuelCardBalanceResponse` — Query available balance and credit limit
- **Status:** Stub implementation returning simulated approvals

#### Telematics Devices (WCF `ITelematicsService`)
- **Endpoint:** `http://localhost:5000/TelematicsService.svc`
- **Protocol:** SOAP/WCF with `[DataContract]` serialization
- **Operations:**
  - `ReceiveVehicleData(VehicleTelematicsData)` — Ingest GPS + sensor data from vehicle OBD-II units
  - `GetVehicleStatus(vehicleId)` → `VehicleStatus` — Query latest known vehicle status (online if <15 min since last contact)
  - `SendCommand(vehicleId, VehicleCommand)` — Remote vehicle commands (DisableEngine, LockDoors, FlashLights) — **unimplemented stub**

---

## 3. Container Diagram (Level 2)

The system is composed of 5 .NET projects deployed as a single monolith, plus a SQL Server database.

```
┌──────────────────────────────────────────────────────────────────────────┐
│                         TransFleet Monolith                              │
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐     │
│  │                    TransFleet.WebApi                             │     │
│  │              ASP.NET Web API 2 + OWIN + Autofac                 │     │
│  │                                                                  │     │
│  │  Controllers: Vehicles, Drivers, Fleets, Compliance,            │     │
│  │               Fuel, Maintenance, Geofences, GPS                 │     │
│  │  Swagger: Swashbuckle 5.6.0                                     │     │
│  │  Real-time: SignalR 2.4.1 (referenced, not implemented)         │     │
│  └───────────────────────┬──────────────────────────────────────────┘     │
│                          │ depends on                                     │
│  ┌───────────────────────┼──────────────────────────────────────────┐     │
│  │                       ▼                                          │     │
│  │                TransFleet.WcfServices                            │     │
│  │           WCF Service Host (SOAP/XML)                            │     │
│  │                                                                  │     │
│  │  Service: TelematicsService                                      │     │
│  │  Contracts: ITelematicsService                                   │     │
│  │  Data Contracts: VehicleTelematicsData, VehicleStatus,           │     │
│  │                  VehicleCommand                                   │     │
│  └───────────────────────┬──────────────────────────────────────────┘     │
│                          │ depends on                                     │
│  ┌───────────────────────┼──────────────────────────────────────────┐     │
│  │                       ▼                                          │     │
│  │                 TransFleet.Core                                   │     │
│  │          Business Logic & Domain Layer                            │     │
│  │                                                                  │     │
│  │  Services: Vehicle, Driver, Compliance, Fuel,                    │     │
│  │            Maintenance, Geofence                                 │     │
│  │  Domain Rules: DOTCompliance, FuelCardValidation, Maintenance    │     │
│  │  Specifications: Vehicle, Driver (composable query predicates)   │     │
│  │  Value Objects: Money, Duration, Distance                        │     │
│  │  Integration Adapters: DOTReporting, FuelCard                    │     │
│  └───────────────────────┬──────────────────────────────────────────┘     │
│                          │ depends on                                     │
│  ┌───────────────────────┼──────────────────────────────────────────┐     │
│  │                       ▼                                          │     │
│  │                 TransFleet.Data                                   │     │
│  │          Entity Framework 6 Data Access Layer                    │     │
│  │                                                                  │     │
│  │  DbContext: TransFleetDbContext (10 DbSets)                      │     │
│  │  Patterns: Generic Repository<T> + UnitOfWork                    │     │
│  │  Config: Lazy loading enabled, proxy creation enabled            │     │
│  └──────────────────────────────────────────────────────────────────┘     │
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────────┐     │
│  │                 TransFleet.Jobs                                   │     │
│  │          Background Job Processing                               │     │
│  │                                                                  │     │
│  │  ComplianceCheckJob  — DOT HOS violation scanning (daily)        │     │
│  │  MaintenanceAlertJob — Overdue maintenance alerts (every 6 hrs)  │     │
│  │  DataArchivalJob     — GPS/HOS data archival (90-day threshold)  │     │
│  │                                                                  │     │
│  │  Depends on: TransFleet.Core (services), TransFleet.Data (UoW)   │     │
│  └──────────────────────────────────────────────────────────────────┘     │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
                               │
                               │ EF6 / ADO.NET
                               ▼
                    ┌─────────────────────┐
                    │    SQL Server        │
                    │    TransFleet DB     │
                    │                     │
                    │  10 core tables     │
                    │  (see §8 Domain     │
                    │   Model)            │
                    └─────────────────────┘
```

### 3.1 Project Dependency Graph

```
TransFleet.WebApi ──────────► TransFleet.Core ──────────► TransFleet.Data
       │                           ▲                           ▲
       └──────────────────────────►│                           │
                                   │                           │
TransFleet.WcfServices ───────────►│                           │
       │                           │                           │
       └───────────────────────────┼──────────────────────────►│
                                   │                           │
TransFleet.Jobs ──────────────────►└──────────────────────────►│
```

> **Note:** `TransFleet.WebApi` and `TransFleet.WcfServices` both reference `TransFleet.Core` and `TransFleet.Data`. Some controllers (`DriversController`, `FleetsController`, `GPSController`) bypass the service layer and access `IUnitOfWork` directly — this is a known technical debt item.

---

## 4. Component Diagram — TransFleet.Core (Level 3)

TransFleet.Core is the heart of the system, containing all business logic, domain rules, specifications, value objects, and external integration adapters.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            TransFleet.Core                                  │
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                         SERVICES                                     │   │
│  │                                                                      │   │
│  │  ┌─────────────────────┐  ┌──────────────────┐  ┌────────────────┐  │   │
│  │  │  IVehicleService    │  │  IDriverService   │  │ IFuelService   │  │   │
│  │  │  ─────────────────  │  │  ───────────────  │  │ ────────────── │  │   │
│  │  │  VehicleService     │  │  DriverService    │  │ FuelService    │  │   │
│  │  │  (498 lines)        │  │  (112 lines)      │  │ (186 lines)   │  │   │
│  │  │                     │  │                    │  │                │  │   │
│  │  │  • CRUD + VIN       │  │  • CRUD + license  │  │ • Process txn │  │   │
│  │  │    uniqueness       │  │    uniqueness      │  │ • Fraud detect │  │   │
│  │  │  • Driver assign    │  │  • Doc expiry      │  │ • Efficiency   │  │   │
│  │  │  • Health scoring   │  │    tracking        │  │   reporting   │  │   │
│  │  │  • Utilization rpt  │  │  • Termination     │  │ • Fleet cost  │  │   │
│  │  │  • Maint cost rpt   │  │    workflow        │  │   aggregation │  │   │
│  │  │  • Default maint    │  │                    │  │                │  │   │
│  │  │    schedule create  │  │                    │  │                │  │   │
│  │  └─────────┬───────────┘  └─────────┬─────────┘  └───────┬────────┘  │   │
│  │            │                        │                    │            │   │
│  │  ┌─────────┴───────────┐  ┌─────────┴─────────┐  ┌──────┴─────────┐ │   │
│  │  │ IComplianceService  │  │ IMaintenanceService│  │ IGeofenceService│ │   │
│  │  │ ─────────────────── │  │ ──────────────────│  │ ───────────────│ │   │
│  │  │ ComplianceService   │  │ MaintenanceService │  │ GeofenceService│ │   │
│  │  │ (229 lines)         │  │ (189 lines)        │  │ (169 lines)   │ │   │
│  │  │                     │  │                     │  │               │ │   │
│  │  │ • HOS check         │  │ • Schedule CRUD     │  │ • CRUD        │ │   │
│  │  │ • Compliance rpt    │  │ • Overdue detection  │  │ • Point-in-  │ │   │
│  │  │ • Remaining hours   │  │ • Service completion │  │   polygon    │ │   │
│  │  │ • Violation detect  │  │ • WorkOrder CRUD     │  │ • Violation  │ │   │
│  │  │ • Duty status record│  │ • WorkOrder complete │  │   detection  │ │   │
│  │  └─────────────────────┘  └─────────────────────┘  └──────────────┘ │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                           │                                 │
│                              uses ────────┘                                 │
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                      DOMAIN RULES (static)                           │   │
│  │                                                                      │   │
│  │  ┌─────────────────────────┐  ┌─────────────────────────────────┐   │   │
│  │  │  DOTComplianceRules     │  │  FuelCardValidationRules        │   │   │
│  │  │  • MaxDrivingHrs: 11/d  │  │  • MaxTxnAmount: $500          │   │   │
│  │  │  • MaxOnDutyHrs: 14/d   │  │  • MaxGallons: 100/txn         │   │   │
│  │  │  • MaxWeeklyHrs: 60/7d  │  │  • MaxTxnPerDay: 5             │   │   │
│  │  │  • RequiredOff: 10 hrs  │  │  • SuspiciousInterval: 30 min  │   │   │
│  │  │  • RestBreakAfter: 8hr  │  │  • IsFuelTypeCompatible()      │   │   │
│  │  │  • MinBreak: 30 min     │  │  • CalculateFuelEfficiency()    │   │   │
│  │  │  • IsDriverInCompliance │  │                                 │   │   │
│  │  │  • HasRequiredRestBreak │  └─────────────────────────────────┘   │   │
│  │  └─────────────────────────┘                                        │   │
│  │  ┌─────────────────────────┐                                        │   │
│  │  │  MaintenanceRules       │                                        │   │
│  │  │  • OilChange: 5,000 mi  │                                        │   │
│  │  │  • TireRotation: 7,500  │                                        │   │
│  │  │  • BrakeInspect: 6 mo   │                                        │   │
│  │  │  • AnnualInspect: 365d  │                                        │   │
│  │  │  • IsMaintenanceOverdue │                                        │   │
│  │  └─────────────────────────┘                                        │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │               SPECIFICATIONS (Composable Query Predicates)           │   │
│  │                                                                      │   │
│  │  Base:  Specification<T> (And, Or, Not combinators)                  │   │
│  │         ISpecification<T> → ToExpression() / IsSatisfiedBy()         │   │
│  │                                                                      │   │
│  │  Vehicle Specs:                    Driver Specs:                      │   │
│  │  • ActiveVehicleSpecification      • ActiveDriverSpecification       │   │
│  │  • VehicleByFleetSpecification     • DriverWithExpiredLicenseSpec    │   │
│  │  • VehicleNeedsMaintenanceSpec     • DriverWithExpiredMedCertSpec    │   │
│  │  • AvailableVehicleSpecification   • DriverByCDLClassSpecification  │   │
│  │  • VehicleByYearRangeSpec                                            │   │
│  │  • VehicleByFuelTypeSpec                                             │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                     VALUE OBJECTS (Immutable)                         │   │
│  │                                                                      │   │
│  │  Money(amount, currency="USD")  — Financial calculations, equality   │   │
│  │  Duration(TimeSpan)             — HOS time tracking, non-negative    │   │
│  │  Distance(value, unit=Miles)    — Miles↔Kilometers conversion        │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                  INTEGRATION ADAPTERS (Ports)                         │   │
│  │                                                                      │   │
│  │  ┌─────────────────────────────┐  ┌──────────────────────────────┐  │   │
│  │  │  IDOTReportingAdapter       │  │  IFuelCardAdapter            │  │   │
│  │  │  ───────────────────────    │  │  ────────────────────        │  │   │
│  │  │  DOTReportingAdapter        │  │  FuelCardAdapter             │  │   │
│  │  │                             │  │                              │  │   │
│  │  │  → SubmitHOSReport()        │  │  → AuthorizeTransaction()   │  │   │
│  │  │  → SubmitInspectionReport() │  │  → SettleTransaction()      │  │   │
│  │  │  → GetComplianceStatus()    │  │  → GetCardBalance()         │  │   │
│  │  │                             │  │                              │  │   │
│  │  │  DTOs: DOTHOSReport,        │  │  DTOs: FuelCardAuthRequest, │  │   │
│  │  │    DOTHOSEntry,             │  │    FuelCardAuthResponse,    │  │   │
│  │  │    DOTSubmissionResponse,   │  │    FuelCardSettleResponse,  │  │   │
│  │  │    DOTInspectionReport,     │  │    FuelCardBalanceResponse  │  │   │
│  │  │    DOTInspectionResponse,   │  │                              │  │   │
│  │  │    DOTComplianceStatus      │  │                              │  │   │
│  │  └─────────────────────────────┘  └──────────────────────────────┘  │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.1 Service → Dependency Matrix

| Service | Domain Rules Used | Repository Entities Accessed | Integration Adapters |
|---|---|---|---|
| **VehicleService** | `MaintenanceRules` | Vehicle, Driver, MaintenanceSchedule, WorkOrder, GPSPosition, FuelTransaction | — |
| **DriverService** | — | Driver, Vehicle | — |
| **ComplianceService** | `DOTComplianceRules` | HOSLog, Driver | — |
| **FuelService** | `FuelCardValidationRules` | FuelTransaction, Vehicle | — |
| **MaintenanceService** | `MaintenanceRules` | MaintenanceSchedule, Vehicle, WorkOrder | — |
| **GeofenceService** | — | Geofence, GPSPosition, Vehicle | — |

> **Note:** Integration adapters (`IDOTReportingAdapter`, `IFuelCardAdapter`) are defined in Core but are not currently wired into the service layer. They exist as stubs awaiting integration.

---

## 5. Technology Stack

### 5.1 By Layer

| Layer | Project | Technology | Version | Purpose |
|---|---|---|---|---|
| **Presentation / API** | TransFleet.WebApi | ASP.NET Web API 2 | 5.2.7 | REST endpoint hosting |
| | | OWIN | 3.1.0 | Middleware pipeline |
| | | Autofac + Autofac.WebApi2 | 4.9.4 / 4.3.1 | Dependency injection |
| | | Swashbuckle | 5.6.0 | Swagger/OpenAPI documentation |
| | | SignalR | 2.4.1 | Real-time notifications (referenced, unused) |
| | | Newtonsoft.Json | 12.0.3 | JSON serialization |
| **Legacy Service** | TransFleet.WcfServices | WCF (System.ServiceModel) | 4.9.0 | SOAP/XML telematics endpoint |
| | | System.Runtime.Serialization | — | Data contract serialization |
| **Business Logic** | TransFleet.Core | C# 8.0 / .NET 4.7.2 | — | Services, rules, specifications, value objects |
| **Data Access** | TransFleet.Data | Entity Framework 6 | 6.4.4 | ORM, change tracking, migrations |
| | | System.Data.Entity | — | DbContext, DbSet |
| **Background Jobs** | TransFleet.Jobs | Hangfire.Core | 1.7.28 | Persistent background job queuing |
| | | Quartz.NET | 3.3.3 | Cron-based job scheduling |
| **Database** | — | SQL Server 2014+ / LocalDB | — | Relational data store |

### 5.2 Connection Configuration

```xml
<!-- Web.config -->
<connectionStrings>
  <add name="TransFleetConnection"
       connectionString="Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=TransFleet;
                         Integrated Security=True;MultipleActiveResultSets=True"
       providerName="System.Data.SqlClient" />
</connectionStrings>

<appSettings>
  <add key="FuelCardServiceEndpoint" value="https://fuelcard.example.com/api" />
  <add key="DOTReportingEndpoint" value="https://fmcsa.example.gov/api" />
  <add key="CarrierNumber" value="MC123456" />
</appSettings>
```

### 5.3 Data Access Patterns

| Pattern | Implementation | Notes |
|---|---|---|
| **Generic Repository** | `IRepository<T>` / `Repository<T>` wrapping `DbSet<T>` | Operations: GetById, GetAll, Find, Add, Remove, Update |
| **Unit of Work** | `IUnitOfWork` wrapping `TransFleetDbContext` | Creates `Repository<T>` instances on demand; atomic `SaveChanges()` |
| **Lazy Loading** | Enabled (`LazyLoadingEnabled = true`) | Risk of N+1 queries on navigation properties |
| **Proxy Creation** | Enabled (`ProxyCreationEnabled = true`) | Dynamic proxy generation for change tracking |
| **Specification Pattern** | `Specification<T>` with And/Or/Not combinators | Composable LINQ expressions for type-safe queries |

---

## 6. Communication Patterns

### 6.1 REST API (TransFleet.WebApi)

All REST endpoints follow standard HTTP conventions with JSON payloads. Controllers inherit from `ApiController`.

| Controller | Route Prefix | Key Endpoints | Auth |
|---|---|---|---|
| **VehiclesController** | `/api/vehicles` | `GET /` `GET /{id}` `GET /fleet/{fleetId}` `GET /status/{status}` `POST /` `PUT /{id}` `DELETE /{id}` `POST /{id}/assign/{driverId}` `POST /{id}/unassign` `GET /{id}/health` `GET /{id}/utilization` `GET /{id}/maintenance-cost` | None |
| **DriversController** | `/api/drivers` | `GET /` `GET /{id}` `POST /` `PUT /{id}` | None |
| **FleetsController** | `/api/fleets` | `GET /` `GET /{id}` `POST /` `PUT /{id}` | None |
| **ComplianceController** | `/api/compliance` | `GET /driver/{id}/check` `GET /driver/{id}/report` `GET /driver/{id}/remaining-hours` `GET /driver/{id}/violations` `POST /driver/{id}/duty-status` | None |
| **FuelController** | `/api/fuel` | `POST /transactions` `GET /vehicle/{id}/transactions` `GET /fleet/{id}/suspicious` `GET /vehicle/{id}/efficiency` `GET /fleet/{id}/cost` | None |
| **MaintenanceController** | `/api/maintenance` | `GET /schedule/{id}` `GET /vehicle/{id}/schedules` `GET /overdue/fleet/{id}` `POST /schedules` `PUT /schedules/{id}` `POST /schedules/{id}/complete` `POST /workorders` `PUT /workorders/{id}` `POST /workorders/{id}/complete` `GET /fleet/{id}/open-workorders` | None |
| **GeofencesController** | `/api/geofences` | `GET /{id}` `GET /fleet/{id}` `POST /` `PUT /{id}` `DELETE /{id}` `GET /vehicle/{vehicleId}/in/{geofenceId}` `GET /fleet/{id}/alerts` | None |
| **GPSController** | `/api/gps` | `POST /positions` `GET /vehicle/{id}/positions` `GET /vehicle/{id}/latest` | None |

**Response Pattern:**
- `200 OK` — Successful retrieval
- `201 Created` — Successful creation (with location header)
- `400 BadRequest` — Invalid model state
- `404 NotFound` — Entity not found
- `500 InternalServerError` — Unhandled exception (generic message)

### 6.2 WCF/SOAP (TransFleet.WcfServices)

| Operation | Contract | Direction | Data Contracts |
|---|---|---|---|
| `ReceiveVehicleData` | `[OperationContract]` | Inbound (device → system) | `VehicleTelematicsData` (VehicleId, Lat, Long, Speed, Heading, Timestamp, Odometer, FuelLevel, EngineTemp, IgnitionOn) |
| `GetVehicleStatus` | `[OperationContract]` | Outbound (system → client) | Returns `VehicleStatus` (VehicleId, Status, LastKnownLat/Long, LastContactTime, IsOnline) |
| `SendCommand` | `[OperationContract]` | Outbound (system → device) | `VehicleCommand` (CommandType: DisableEngine/LockDoors/FlashLights, Parameters, IssueTime) — **stub only** |

**Online Detection Logic:** Vehicle is considered online if `(DateTime.UtcNow - lastGPSTimestamp) < 15 minutes`.

### 6.3 Background Jobs (TransFleet.Jobs)

| Job | Scheduler | Frequency | Process |
|---|---|---|---|
| **ComplianceCheckJob** | Hangfire/Quartz | Daily | Iterates all active drivers → checks HOS compliance → logs violations from past 7 days |
| **MaintenanceAlertJob** | Hangfire/Quartz | Every 6 hours | Iterates all fleets → finds overdue maintenance schedules → logs alerts (Console.WriteLine) |
| **DataArchivalJob** | Hangfire/Quartz | Configurable | Queries GPS positions older than 90 days → counts/logs (archive logic partially implemented) |

**Job Execution Pattern:**
```
async Task Execute()
{
    try {
        Log start timestamp
        [Business logic]
        Log completion timestamp
    } catch (Exception ex) {
        Log error
        throw  // Allow scheduler retry
    }
}
```

> **Note:** All job logging is via `Console.WriteLine`. No structured logging framework is configured.

---

## 7. Data Flow — Key Business Processes

### 7.1 Vehicle-Driver Assignment

```
Fleet Manager                  VehiclesController          VehicleService              Repository<T>
     │                              │                          │                          │
     │  POST /api/vehicles/         │                          │                          │
     │  {vehicleId}/assign/         │                          │                          │
     │  {driverId}                  │                          │                          │
     │─────────────────────────────►│                          │                          │
     │                              │   AssignDriver(          │                          │
     │                              │     vehicleId, driverId) │                          │
     │                              │─────────────────────────►│                          │
     │                              │                          │  GetById(vehicleId)      │
     │                              │                          │─────────────────────────►│
     │                              │                          │◄─────────────────────────│
     │                              │                          │  GetById(driverId)       │
     │                              │                          │─────────────────────────►│
     │                              │                          │◄─────────────────────────│
     │                              │                          │                          │
     │                              │                          │  ── BUSINESS RULES ──    │
     │                              │                          │  ✓ Vehicle.Status == "Active"
     │                              │                          │  ✓ Driver.Status == "Active"
     │                              │                          │  ✓ Driver.LicenseExpiry > now
     │                              │                          │  ✓ Driver.MedicalCertExpiry > now (if CDL)
     │                              │                          │  ✓ Driver not assigned to another vehicle
     │                              │                          │                          │
     │                              │                          │  Update(vehicle)         │
     │                              │                          │  {CurrentDriverId=driverId}
     │                              │                          │─────────────────────────►│
     │                              │                          │  SaveChanges()           │
     │                              │                          │─────────────────────────►│
     │                              │◄─────────────────────────│                          │
     │  200 OK                      │                          │                          │
     │◄─────────────────────────────│                          │                          │
```

**Assignment Preconditions:**
1. Vehicle must exist and be in `Active` status
2. Driver must exist and be in `Active` status
3. Driver's license must not be expired
4. If driver holds CDL, medical certificate must be current
5. Driver must not be currently assigned to another vehicle

**Deactivation Rules:** Vehicle deactivation automatically unassigns any current driver and requires all open work orders to be completed or cancelled first.

### 7.2 Fuel Transaction Processing

```
External System               FuelController              FuelService               Validation Rules
     │                              │                          │                          │
     │  POST /api/fuel/             │                          │                          │
     │  transactions                │                          │                          │
     │  {VehicleId, DriverId,       │                          │                          │
     │   Gallons, Amount,           │                          │                          │
     │   PricePerGallon, ...}       │                          │                          │
     │─────────────────────────────►│                          │                          │
     │                              │  ProcessFuelTransaction  │                          │
     │                              │  (transaction)           │                          │
     │                              │─────────────────────────►│                          │
     │                              │                          │  Lookup vehicle          │
     │                              │                          │──────────►               │
     │                              │                          │                          │
     │                              │                          │  IsTransactionValid(     │
     │                              │                          │    txn, vehicle)         │
     │                              │                          │─────────────────────────►│
     │                              │                          │  ✓ Amount ≤ $500         │
     │                              │                          │  ✓ Gallons ≤ 100         │
     │                              │                          │  ✓ Fuel type compatible  │
     │                              │                          │◄─────────────────────────│
     │                              │                          │                          │
     │                              │              ┌───────────┤  If valid:               │
     │                              │              │           │  Find previous txn       │
     │                              │              │           │  IsSuspicious(cur, prev) │
     │                              │              │           │─────────────────────────►│
     │                              │              │           │  ✓ Time gap ≥ 30 min     │
     │                              │              │           │  ✓ State change feasible │
     │                              │              │           │◄─────────────────────────│
     │                              │              │           │                          │
     │                              │              │  Set status: "Approved" or "Flagged" │
     │                              │              └───────────┤                          │
     │                              │                          │  Add(transaction)        │
     │                              │                          │  SaveChanges()           │
     │                              │                          │                          │
     │                              │                          │  If odometer > current:  │
     │                              │                          │    Update vehicle odometer│
     │                              │                          │    SaveChanges()         │
     │                              │                          │                          │
     │                              │◄─────────────────────────│                          │
     │  201 Created                 │                          │                          │
     │◄─────────────────────────────│                          │                          │
```

**Transaction Validation Pipeline:**

| Step | Rule | Threshold | Result on Failure |
|---|---|---|---|
| 1. Amount check | `transaction.Amount ≤ MaxFuelTransactionAmount` | $500.00 | Status = `Flagged` |
| 2. Volume check | `transaction.Gallons ≤ MaxGallonsPerTransaction` | 100 gallons | Status = `Flagged` |
| 3. Fuel compatibility | `IsFuelTypeCompatible(txn, vehicle)` | — | Status = `Flagged` (currently always passes) |
| 4. Time proximity | Previous txn within 30 minutes | 30 min | Status = `Flagged` |
| 5. Geographic feasibility | Different state within 60 minutes | 60 min | Status = `Flagged` |

### 7.3 DOT/FMCSA Compliance Checking

```
┌───────────────────────────────────────────────────────────────────────────┐
│                    Compliance Check Flow (Two Entry Points)               │
└───────────────────────────────────────────────────────────────────────────┘

  ═══ ENTRY POINT 1: Real-time API Check ═══

  Fleet Manager        ComplianceController      ComplianceService       DOTComplianceRules
       │                      │                        │                        │
       │ GET /api/compliance/ │                        │                        │
       │ driver/{id}/check    │                        │                        │
       │─────────────────────►│                        │                        │
       │                      │ CheckDriverCompliance  │                        │
       │                      │ (driverId, today)      │                        │
       │                      │───────────────────────►│                        │
       │                      │                        │ Query HOSLogs          │
       │                      │                        │ (past 7 days)          │
       │                      │                        │──────►                 │
       │                      │                        │                        │
       │                      │                        │ IsDriverInCompliance   │
       │                      │                        │ (logs, checkDate)      │
       │                      │                        │───────────────────────►│
       │                      │                        │  ✓ Driving ≤ 11 hrs/d │
       │                      │                        │  ✓ On-duty ≤ 14 hrs/d │
       │                      │                        │  ✓ Weekly ≤ 60 hrs/7d │
       │                      │                        │  ✓ Rest break taken   │
       │                      │                        │    after 8 hrs driving │
       │                      │                        │    (≥ 30 min break)   │
       │                      │                        │◄──────────────────────│
       │                      │◄───────────────────────│                        │
       │  { isCompliant: T/F }│                        │                        │
       │◄─────────────────────│                        │                        │


  ═══ ENTRY POINT 2: Background Job (Daily Batch) ═══

  Scheduler            ComplianceCheckJob         ComplianceService       DOTComplianceRules
       │                      │                        │                        │
       │  Execute()           │                        │                        │
       │─────────────────────►│                        │                        │
       │                      │ Get all Active drivers │                        │
       │                      │──────►                 │                        │
       │                      │                        │                        │
       │                      │ For each driver:       │                        │
       │                      │ CheckDriverCompliance  │                        │
       │                      │───────────────────────►│                        │
       │                      │                        │───────────────────────►│
       │                      │                        │◄──────────────────────│
       │                      │                        │                        │
       │                      │ If non-compliant:      │                        │
       │                      │   GetViolations        │                        │
       │                      │   (driverId, -7 days)  │                        │
       │                      │───────────────────────►│                        │
       │                      │                        │  Generate violation    │
       │                      │                        │  records for each:     │
       │                      │                        │  • ExcessiveDrivingHrs │
       │                      │                        │  • ExcessiveOnDutyHrs  │
       │                      │                        │  • MissingRestBreak    │
       │                      │◄───────────────────────│                        │
       │                      │                        │                        │
       │                      │ Console.WriteLine(     │                        │
       │                      │   violation details)   │                        │
       │                      │                        │                        │
```

**DOT HOS Regulation Constants (49 CFR Part 395):**

| Rule | Limit | Window |
|---|---|---|
| Maximum driving hours | 11 hours | Per day |
| Maximum on-duty hours (driving + on-duty) | 14 hours | Per day |
| Maximum weekly driving hours | 60 hours | 7-day period |
| Maximum extended weekly driving hours | 70 hours | 8-day period |
| Required off-duty time | 10 hours | Before new shift |
| Mandatory rest break trigger | After 8 hours continuous driving | — |
| Minimum rest break duration | 30 minutes | — |

**Duty Status Values:** `OffDuty`, `SleeperBerth`, `Driving`, `OnDuty`

### 7.4 Telematics Data Ingestion (WCF)

```
Telematics Device          TelematicsService (WCF)         Repository<T>
     │                              │                          │
     │  SOAP: ReceiveVehicleData    │                          │
     │  {VehicleId: 42,             │                          │
     │   Lat: 40.7128,              │                          │
     │   Long: -74.0060,            │                          │
     │   Speed: 65.5,               │                          │
     │   Heading: 180.0,            │                          │
     │   Timestamp: ...,            │                          │
     │   Odometer: 125430,          │                          │
     │   FuelLevel: 0.75,           │                          │
     │   EngineTemp: 195.0,         │                          │
     │   IgnitionOn: true}          │                          │
     │─────────────────────────────►│                          │
     │                              │  Validate vehicle exists │
     │                              │─────────────────────────►│
     │                              │◄─────────────────────────│
     │                              │                          │
     │                              │  Create GPSPosition:     │
     │                              │  {VehicleId, Lat, Long,  │
     │                              │   Speed, Heading,        │
     │                              │   Timestamp}             │
     │                              │  Add(gpsPosition)        │
     │                              │─────────────────────────►│
     │                              │                          │
     │                              │  If odometer > current:  │
     │                              │    Update vehicle        │
     │                              │    odometer reading      │
     │                              │─────────────────────────►│
     │                              │                          │
     │                              │  SaveChanges()           │
     │                              │─────────────────────────►│
     │  (void)                      │                          │
     │◄─────────────────────────────│                          │
```

### 7.5 Maintenance Schedule Lifecycle

```
Vehicle Created ──► Default schedules auto-created (OilChange, TireRotation, AnnualInspection)
                              │
                              ▼
                    ┌──────────────────┐
                    │  Status: Active  │◄──────── Service completed: schedule
                    │                  │          resets NextServiceDate/Mileage
                    └────────┬─────────┘
                             │
              Odometer or date threshold exceeded
                             │
                             ▼
                    ┌──────────────────┐         MaintenanceAlertJob
                    │  Status: Overdue │────────► logs alert every 6 hours
                    │                  │
                    └────────┬─────────┘
                             │
                     Work order created
                             │
                             ▼
                    ┌──────────────────┐
                    │  WorkOrder: Open │
                    └────────┬─────────┘
                             │
                     Work in progress
                             │
                             ▼
                    ┌──────────────────┐
                    │ WorkOrder:       │
                    │ InProgress       │
                    └────────┬─────────┘
                             │
                     CompleteWorkOrder(actualCost, notes)
                             │
                             ▼
                    ┌──────────────────┐
                    │ WorkOrder:       │
                    │ Completed        │
                    └──────────────────┘
```

**Overdue Detection Logic (`MaintenanceRules.IsMaintenanceOverdue`):**
- **Mileage-based:** `vehicle.OdometerReading >= schedule.NextServiceMileage`
- **Date-based:** `DateTime.UtcNow >= schedule.NextServiceDate`
- Either condition triggers overdue status

---

## 8. Domain Model

### 8.1 Entity Relationship Diagram

```
┌──────────────┐       ┌──────────────┐       ┌──────────────┐
│   Client     │       │    Fleet     │       │   Vehicle    │
│──────────────│ 1   * │──────────────│ 1   * │──────────────│
│ ClientId (PK)│───────│ FleetId (PK) │───────│ VehicleId(PK)│
│ CompanyName  │       │ Name         │       │ VIN (unique) │
│ ContactName  │       │ ClientId(FK) │       │ Make/Model   │
│ Email/Phone  │       │ Description  │       │ Year         │
│ Address      │       │ Status       │       │ LicensePlate │
│ Status       │       │              │       │ FleetId (FK) │
│ CreatedDate  │       │              │       │ Status       │
└──────────────┘       └──────┬───────┘       │ OdometerReading
                              │               │ FuelType     │
                              │               │ CurrentDriverId
                              │               │  (FK, nullable)
                              │               │ PurchasePrice│
                       ┌──────┴───────┐       └───┬──┬──┬────┘
                       │  Geofence    │      0..1 │  │  │
                       │──────────────│           │  │  │
                       │ GeofenceId   │           │  │  │
                       │ FleetId (FK) │   ┌───────┘  │  │
                       │ Polygon      │   │          │  │
                       │ AlertType    │   ▼          │  │
                       │ IsActive     │ ┌────────────┴──┴──────┐
                       └──────────────┘ │     Driver           │
                                        │─────────────────────│
                                        │ DriverId (PK)        │
                                        │ FirstName/LastName    │
                                        │ LicenseNumber (uniq)  │
                                        │ LicenseExpiry         │
                                        │ CDLClass (A/B/C)      │
                                        │ MedicalCertExpiry     │
                                        │ Status                │
                                        │ HireDate              │
                                        └──────┬──────────────┘
                                               │
        ┌──────────────────────────────────────┼─────────────────────┐
        │                                      │                     │
        ▼                                      ▼                     ▼
┌──────────────────┐             ┌──────────────────┐  ┌──────────────────┐
│ FuelTransaction  │             │     HOSLog       │  │   GPSPosition    │
│──────────────────│             │──────────────────│  │──────────────────│
│ TransactionId(PK)│             │ LogId (PK, long) │  │ PositionId(long) │
│ VehicleId (FK)   │             │ DriverId (FK)    │  │ VehicleId (FK)   │
│ DriverId (FK)    │             │ DutyStatus       │  │ Lat/Long (18,8)  │
│ FuelCardId       │             │ StartTime/EndTime│  │ Speed/Heading    │
│ Gallons (18,3)   │             │ Lat/Long (18,8)  │  │ Altitude         │
│ Amount (18,2)    │             │ VehicleId (FK)   │  │ Timestamp        │
│ PricePerGal(18,3)│             │ OdometerReading  │  │ Satellites       │
│ Location/City/   │             │ Remarks          │  │ GPSQuality       │
│   State          │             │ RecordStatus     │  └──────────────────┘
│ OdometerReading  │             │ RecordOrigin     │     High-volume
│ AuthorizationCode│             │   (Auto/Manual/  │     time-series
│ Status           │             │    Edited)       │     (100M+ rows)
└──────────────────┘             └──────────────────┘

┌──────────────────┐             ┌──────────────────┐
│ MaintenanceSchedule│           │    WorkOrder     │
│──────────────────│             │──────────────────│
│ ScheduleId (PK)  │             │ WorkOrderId (PK) │
│ VehicleId (FK)   │             │ VehicleId (FK)   │
│ ServiceType      │             │ Type (Repair/    │
│ IntervalMiles    │             │  Inspection/     │
│ IntervalDays     │             │  Recall/Modify)  │
│ LastService*     │             │ Priority (Low/   │
│ NextService*     │             │  Med/High/Crit)  │
│ Status           │             │ Status (Open/    │
└──────────────────┘             │  InProgress/     │
                                 │  Completed/      │
                                 │  Cancelled)      │
                                 │ EstimatedCost    │
                                 │ ActualCost       │
                                 └──────────────────┘
```

### 8.2 Decimal Precision Configuration

| Entity | Field | Precision | Business Reason |
|---|---|---|---|
| FuelTransaction | Amount | (18,2) | Financial — dollar amounts |
| FuelTransaction | Gallons | (18,3) | Volume — sub-gallon precision |
| FuelTransaction | PricePerGallon | (18,3) | Unit cost — fractional cents |
| Vehicle | PurchasePrice | (18,2) | Asset valuation |
| WorkOrder | EstimatedCost / ActualCost | (18,2) | Budgeting |
| GPSPosition | Latitude / Longitude | (18,8) | Geographic — ±1mm precision |
| GPSPosition | Speed / Heading | (18,2) | Velocity and direction |
| HOSLog | Latitude / Longitude | (18,8) | Compliance location tracking |

### 8.3 Status Enumerations (String-Based)

| Entity | Status Values |
|---|---|
| Vehicle | `Active`, `Maintenance`, `Retired`, `Decommissioned` |
| Driver | `Active`, `OnLeave`, `Terminated`, `Suspended` |
| Fleet / Client | `Active`, `Inactive` |
| MaintenanceSchedule | `Active`, `Completed`, `Overdue`, `Cancelled` |
| FuelTransaction | `Approved`, `Pending`, `Declined`, `Flagged` |
| WorkOrder | `Open`, `InProgress`, `Completed`, `Cancelled` |
| HOSLog.DutyStatus | `OffDuty`, `SleeperBerth`, `Driving`, `OnDuty` |
| HOSLog.RecordStatus | `Active`, `Changed`, `Inactive` |
| HOSLog.RecordOrigin | `Auto`, `Manual`, `Edited` |

> ⚠️ All statuses are stored as strings. No enum types are used in the database schema, creating risk of typos and inconsistent data.

---

## 9. Known Technical Debt

### 9.1 Critical Issues

| ID | Category | Description |
|---|---|---|
| TD-01 | God Class | `VehicleService` is 498 lines with 11 public methods spanning CRUD, assignment, health scoring, utilization reporting, and maintenance cost calculation |
| TD-02 | Security | **No authentication or authorization** on any endpoint — all API operations are publicly accessible |
| TD-03 | Broken Logic | `GeofenceService.IsPointInGeofence()` **always returns `true`** — geofencing is completely non-functional |
| TD-04 | Stub Integration | `DOTReportingAdapter` returns hardcoded "Satisfactory" responses — no actual FMCSA submission |
| TD-05 | Stub Integration | `FuelCardAdapter` returns simulated approvals — no actual payment processing |
| TD-06 | Unimplemented | `TelematicsService.SendCommand()` logs to console only — no vehicle command transmission |
| TD-07 | Performance | `Repository.GetAll()` materializes entire tables — dangerous for GPSPosition (100M+ rows) |
| TD-08 | Anemic Model | All entities are pure data bags with no behavior — business logic lives entirely in services |

### 9.2 Architectural Concerns

| ID | Category | Description |
|---|---|---|
| TD-09 | Layer Violation | `DriversController`, `FleetsController`, `GPSController` bypass the service layer and access `IUnitOfWork` directly |
| TD-10 | Static Rules | `DOTComplianceRules`, `FuelCardValidationRules`, `MaintenanceRules` are static classes — not injectable or mockable for testing |
| TD-11 | No Time Abstraction | `DateTime.UtcNow` used directly throughout — makes time-dependent logic untestable |
| TD-12 | String Enums | All status values are magic strings with no type safety |
| TD-13 | N+1 Queries | Lazy loading enabled with multiple nested loops (e.g., `GetOverdueSchedules` iterates vehicles then schedules) |
| TD-14 | No Logging | All diagnostic output is via `Console.WriteLine` — no structured logging framework |
| TD-15 | Hardcoded Rules | Federal DOT regulations and business policies are hardcoded constants, not configurable |
| TD-16 | No Exception Hierarchy | All business errors thrown as `InvalidOperationException` — no custom domain exceptions |

---

*This document was generated from static analysis of the TransFleet source code. All diagrams reflect the actual codebase structure and dependencies as implemented.*
