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

```
See: assets/screenshots/01-project-structure.txt
```

### Build Output
The solution compiles successfully with `dotnet build` after NuGet restore (5 DLLs produced).

```
See: assets/screenshots/02-build-output.txt
```

### VehicleService — Business Logic Overview
The core "god service" with ~500 lines, 20+ business rules, and complex validation chains.

```
See: assets/screenshots/03-vehicle-service-overview.txt
```

### WCF Telematics Contract
Legacy WCF service interface for vehicle telemetry ingestion and remote commands.

```
See: assets/screenshots/04-wcf-telematics-contract.txt
```

### Data Model Overview
Entity Framework 6 DbContext with 10 entity types and complex relationship mapping.

```
See: assets/screenshots/05-data-model-overview.txt
```

### Architecture Layers
Layered architecture: WebApi → Core (services/domain/specs) → Data (EF6) + WcfServices + Jobs.

```
See: assets/screenshots/06-architecture-layers.txt
```

### REST API Endpoints
8 Web API controllers exposing fleet management, compliance, fuel, and maintenance endpoints.

```
See: assets/screenshots/07-api-endpoints.txt
```

### Spec2Cloud Reverse Engineering Workflow
The 9-phase investigation process from codebase exploration to modernization blueprint.

```
See: assets/screenshots/08-spec2cloud-workflow.txt
```
