---
title: "Reverse Engineering with Spec2Cloud"
description: "Deep analysis of legacy enterprise apps with TransFleet ERP - extract specs from complex codebases"
authors: ["marconsilva"]
category: "Spec-Driven Development"
industry: "Cross-Industry"
services: ["Azure Container Apps"]
languages: [".NET", "C#"]
frameworks: ["ASP.NET Web API", "Entity Framework", "WCF"]
modernizationTools: ["Spec2Cloud"]
agenticTools: []
tags: ["reverse-engineering", "legacy-analysis", "specification-extraction", "bounded-contexts", "wcf"]
extensions: ["github.copilot"]
thumbnail: ""
video: ""
version: "1.0.0"
---

## Screenshots

The following screenshots document the TransFleet legacy application structure, build process, and Spec2Cloud reverse-engineering workflow.

### Project Structure
The solution contains 5 projects targeting .NET Framework 4.7.2 with SDK-style csproj files.

[View: Project Structure](assets/screenshots/01-project-structure.txt)

*Open the text file to view the full project tree and file listing.*

### Build Output
The solution compiles successfully with `dotnet build` after NuGet restore (5 DLLs produced).

[View: Build Output](assets/screenshots/02-build-output.txt)

*Open the text file to view the complete build log.*

### VehicleService — Business Logic Overview
The core "god service" with ~500 lines, 20+ business rules, and complex validation chains.

[View: VehicleService Overview](assets/screenshots/03-vehicle-service-overview.txt)

*Open the text file to view the full service analysis and business rule inventory.*

### WCF Telematics Contract
Legacy WCF service interface for vehicle telemetry ingestion and remote commands.

[View: WCF Telematics Contract](assets/screenshots/04-wcf-telematics-contract.txt)

*Open the text file to view the complete WCF contract definition.*

### Data Model Overview
Entity Framework 6 DbContext with 10 entity types and complex relationship mapping.

[View: Data Model Overview](assets/screenshots/05-data-model-overview.txt)

*Open the text file to view the entity relationships and DbContext configuration.*

### Architecture Layers
Layered architecture: WebApi → Core (services/domain/specs) → Data (EF6) + WcfServices + Jobs.

[View: Architecture Layers](assets/screenshots/06-architecture-layers.txt)

*Open the text file to view the full layered architecture diagram.*

### REST API Endpoints
8 Web API controllers exposing fleet management, compliance, fuel, and maintenance endpoints.

[View: REST API Endpoints](assets/screenshots/07-api-endpoints.txt)

*Open the text file to view the complete endpoint inventory.*

### Spec2Cloud Reverse Engineering Workflow
The 9-phase investigation process from codebase exploration to modernization blueprint.

[View: Spec2Cloud Workflow](assets/screenshots/08-spec2cloud-workflow.txt)

*Open the text file to view the full reverse-engineering workflow phases.*

## Solution Walkthrough

This walkthrough uses the **GitHub Copilot CLI** (`gh copilot`) to reverse-engineer the TransFleet legacy .NET Framework 4.7.2 codebase through 8 incremental steps. Each step runs a non-interactive Copilot CLI prompt that analyzes code and generates specification artifacts.

> **Prerequisites:** `gh copilot` v1.0.28+, Git, access to the repo.
>
> **Branch:** All work is done on `solution-final`. Check it out with `git checkout solution-final` to see the full results.

### Setup

```powershell
cd C:\code\gbb\labs\appmodlab-reverse-engineering-legacy-apps-with-spec2cloud
git checkout main && git checkout -b solution-final
New-Item -ItemType Directory -Force -Path assets/outputs
```

---

### Step 1: Explore Legacy Codebase

**Command:**
```powershell
gh copilot -- -p "Analyze the TransFleet legacy codebase structure. List all projects in the solution, their dependencies, frameworks used (.NET version, EF, WCF, Web API), key source files, and overall architecture. Give a summary of the codebase complexity and technology stack." --yolo --no-ask-user -s 2>&1 | Tee-Object -FilePath "assets/outputs/step-01-explore-codebase.txt"
```

**Key Output:** Identified 5 projects (WebApi, Core, Data, WcfServices, Jobs) all targeting .NET Framework 4.7.2 with 44 C# source files. Architecture is layered: Controllers → Services → Repository/UoW → EF6 → SQL Server. Key complexity: VehicleService god class (500+ lines, 20+ business rules), WCF telematics, 100M+ GPS rows, no test projects.

**Tag:** `step-01-explore-codebase`

---

### Step 2: Run Spec2Cloud Analysis

**Command:**
```powershell
gh copilot -- -p "Perform a comprehensive Spec2Cloud reverse engineering analysis of this TransFleet legacy codebase. Identify: 1) All bounded contexts and their responsibilities, 2) Service coupling and dependencies between components, 3) Technical debt items (god classes, missing abstractions, hardcoded rules), 4) Integration points (WCF, external systems), 5) Data access patterns and their issues, 6) Cross-cutting concerns. Provide a detailed technical debt inventory with severity ratings." --yolo --no-ask-user -s 2>&1 | Tee-Object -FilePath "assets/outputs/step-02-spec2cloud-analysis.txt"
```

**Key Output:** Generated `SPEC2CLOUD-ANALYSIS.md`. Identified 7 bounded contexts (Vehicle, Compliance, Fuel, Maintenance, Telematics, Geofencing, Driver). Cataloged 47 technical debt items: 12 Critical (no auth, broken geofencing, stub integrations, unpaginated queries on 100M+ rows), 18 High (N+1 queries, god classes, hardcoded regulations), 13 Medium, 4 Low.

**Tag:** `step-02-spec2cloud-analysis`

---

### Step 3: Generate Architecture Spec

**Command:**
```powershell
gh copilot -- -p "Generate a detailed architecture specification document for the TransFleet legacy system. Include: 1) System context diagram description showing external integrations, 2) Container diagram showing the 5 projects and their relationships, 3) Component diagram for TransFleet.Core showing services, domain rules, specifications, and adapters, 4) Technology stack details for each layer, 5) Communication patterns (REST, WCF/SOAP, background jobs), 6) Data flow descriptions for key business processes. Save this as ARCHITECTURE-SPEC.md in the repo root." --yolo --no-ask-user -s 2>&1 | Tee-Object -FilePath "assets/outputs/step-03-architecture-spec.txt"
```

**Key Output:** Generated `ARCHITECTURE-SPEC.md` (865 lines). Covers system context with 3 external integrations (DOT/FMCSA, fuel card processor, telematics devices), container/component diagrams, 30+ REST endpoints, WCF contracts, and data flow sequences for vehicle assignment, fuel processing, and compliance checking.

**Tag:** `step-03-architecture-spec`

---

### Step 4: Generate API Contracts

**Command:**
```powershell
gh copilot -- -p "Analyze all REST API endpoints in TransFleet.WebApi/Controllers and the WCF service contracts in TransFleet.WcfServices. Generate OpenAPI 3.0 specification documents. For each controller, document: HTTP method, route, request/response models, status codes, and business rules enforced. Also document the WCF ITelematicsService operations. Save the OpenAPI spec as API-CONTRACTS.yaml in the repo root." --yolo --no-ask-user -s 2>&1 | Tee-Object -FilePath "assets/outputs/step-04-api-contracts.txt"
```

**Key Output:** Generated `API-CONTRACTS.yaml` — OpenAPI 3.0 spec documenting 39 REST endpoints across 8 controllers (Vehicles 11, Maintenance 9, Geofences 7, Fuel 5, Compliance 5, Drivers 4, Fleets 4, GPS 3) plus 3 WCF operations (ReceiveVehicleData, GetVehicleStatus, SendCommand).

**Tag:** `step-04-api-contracts`

---

### Step 5: Generate Data Model Spec

**Command:**
```powershell
gh copilot -- -p "Analyze the TransFleet.Data project to generate a complete data model specification. Examine TransFleetDbContext.cs, all entity classes in the Entities folder, and the Repository/UnitOfWork patterns. Document: 1) All entity types with their properties and data types, 2) Entity relationships with FK mappings, 3) EF6 Fluent API configurations, 4) The Repository<T> and UnitOfWork implementation, 5) An ER diagram in Mermaid format. Save as DATA-MODEL-SPEC.md in the repo root." --yolo --no-ask-user -s 2>&1 | Tee-Object -FilePath "assets/outputs/step-05-data-models.txt"
```

**Key Output:** Generated `DATA-MODEL-SPEC.md`. Documented 10 entity types with all properties, 11 one-to-many relationships, 12 Fluent API precision configurations (currency 18,2; GPS 18,8; volumes 18,3), generic Repository<T> (8 CRUD methods, all synchronous), and full Mermaid ER diagram. Found 2 orphaned FKs (FuelCardId, AssignedToVendorId).

**Tag:** `step-05-data-models`

---

### Step 6: Identify Modernization Paths

**Command:**
```powershell
gh copilot -- -p "Based on the TransFleet legacy codebase analysis, recommend modernization strategies. Consider: 1) Microservices decomposition along bounded contexts, 2) .NET Framework 4.7.2 to .NET 8 migration path, 3) WCF to gRPC or REST migration, 4) EF6 to EF Core migration, 5) Azure Container Apps deployment model, 6) Event-driven architecture for high-volume GPS data, 7) Database modernization. For each recommendation, provide effort estimate, risk level, and prerequisites. Save as MODERNIZATION-PATHS.md in the repo root." --yolo --no-ask-user -s 2>&1 | Tee-Object -FilePath "assets/outputs/step-06-modernization-paths.txt"
```

**Key Output:** Generated `MODERNIZATION-PATHS.md` (978 lines). 8-phase roadmap: Foundation (4–6 wk), 7 microservices extraction via Strangler Fig (26–38 person-wk), .NET 8 migration (3–5 wk), WCF→gRPC+Event Hubs (5–8 wk), EF Core, Azure Container Apps (4–6 wk), event-driven GPS pipeline (5–7 wk), database-per-service (4–6 wk). Total: 14–20 months, team of 6–8. Extraction order: Driver → Maintenance → Fuel → Compliance → Telematics → Geofence → Vehicle.

**Tag:** `step-06-modernization-paths`

---

### Step 7: Create Migration Plan

**Command:**
```powershell
gh copilot -- -p "Create a detailed phased migration plan for the TransFleet system modernization. The plan should include: 1) Sprint-level breakdown with specific tasks and acceptance criteria, 2) Priority ordering based on business value and technical risk, 3) Dependency mapping between migration tasks, 4) Rollback strategies for each phase, 5) Testing strategy (unit, integration, contract, E2E), 6) Data migration approach for the 100M+ GPS records, 7) Timeline with milestones. Use the existing analysis in SPEC2CLOUD-ANALYSIS.md and MODERNIZATION-PATHS.md as input. Save as MIGRATION-PLAN.md in the repo root." --yolo --no-ask-user -s 2>&1 | Tee-Object -FilePath "assets/outputs/step-07-migration-plan.txt"
```

**Key Output:** Generated `MIGRATION-PLAN.md` (830 lines). 20 sprints across 7 phases (40 weeks). Priority matrix scoring business value × technical risk. Strangler Fig with APIM routing (<1 min rollback, auto-trigger at 1% error rate). Testing pyramid: unit 50%, integration 30%, contract/Pact 15%, E2E 5%. GPS data migration: 5-tier approach (dual-write → backfill hot/warm/cold → validate → cutover → 30-day observe). 8 milestones from "Hardened Monolith" (Week 6) to "Monolith Decommissioned" (Week 40).

**Tag:** `step-07-migration-plan`

---

### Step 8: Validate Specs

**Command:**
```powershell
gh copilot -- -p "Validate the generated specifications against the actual TransFleet source code. Cross-reference: 1) API-CONTRACTS.yaml endpoints against actual controller routes, 2) DATA-MODEL-SPEC.md entities against actual entity classes, 3) ARCHITECTURE-SPEC.md components against actual service implementations, 4) Check for missing endpoints, entities, or services, 5) Verify business rule descriptions match actual code logic. Report all discrepancies. Save as VALIDATION-REPORT.md in the repo root." --yolo --no-ask-user -s 2>&1 | Tee-Object -FilePath "assets/outputs/step-08-validate-specs.txt"
```

**Key Output:** Generated `VALIDATION-REPORT.md`. Results: DATA-MODEL-SPEC — ✅ perfect match (all 10 entities verified). API-CONTRACTS — ⚠️ 5 JSON casing mismatches (spec uses camelCase, Web API 2 defaults to PascalCase), all 37 routes verified correct. ARCHITECTURE-SPEC — ⚠️ 1 gap (missing DI bootstrap code) + 3 minor count inaccuracies. All 13 business rules and 16 technical debt items verified accurate.

**Tag:** `step-08-validate-specs`

---

### Push Results

```powershell
git push origin solution-final --tags
```

### Generated Artifacts (on `solution-final` branch)

| File | Description |
|------|-------------|
| `SPEC2CLOUD-ANALYSIS.md` | Full reverse-engineering analysis with 47 technical debt items |
| `ARCHITECTURE-SPEC.md` | C4-style architecture specification (865 lines) |
| `API-CONTRACTS.yaml` | OpenAPI 3.0 spec — 39 REST endpoints + 3 WCF operations |
| `DATA-MODEL-SPEC.md` | Entity/relationship docs with Mermaid ER diagram |
| `MODERNIZATION-PATHS.md` | 8-phase modernization roadmap (978 lines) |
| `MIGRATION-PLAN.md` | 20-sprint migration plan with rollback strategies |
| `VALIDATION-REPORT.md` | Cross-reference validation of all specs vs. source code |
| `assets/outputs/step-*.txt` | Raw Copilot CLI output for each step |
