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
