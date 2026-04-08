# TransFleet Legacy Application - Code Structure Summary

## Project Structure

### TransFleet.WebApi (ASP.NET Web API 2)
**Controllers (REST API Endpoints):**
- `VehiclesController` - Vehicle CRUD, assignment, health reports, utilization
- `DriversController` - Driver management
- `FleetsController` - Fleet management
- `ComplianceController` - DOT HOS compliance checking and reporting
- `FuelController` - Fuel transaction processing and efficiency reporting
- `MaintenanceController` - Maintenance schedules and work orders
- `GeofencesController` - Geofence management and alerts
- `GPSController` - GPS position tracking

**Configuration:**
- `Web.config` - Database connection strings, service endpoints

### TransFleet.Core (Business Logic Layer)
**Services (1200+ lines each):**
- `VehicleService` - Complex vehicle management with 20+ business rules
  - Vehicle assignment with driver qualification checks
  - Health scoring based on maintenance and work orders
  - Utilization reporting with GPS and fuel data
  - Automatic maintenance schedule creation
  - Odometer validation
  
- `ComplianceService` - DOT Hours of Service compliance
  - 11-hour daily driving limit enforcement
  - 14-hour on-duty window enforcement
  - 30-minute break requirement validation
  - 60/70-hour weekly limit checking
  - Violation detection and reporting
  
- `FuelService` - Fuel card transaction processing
  - Transaction validation against limits
  - Fraud detection for suspicious patterns
  - Fuel efficiency calculations (MPG)
  - Cost per mile analytics
  
- `MaintenanceService` - Maintenance scheduling and work orders
  - Interval-based schedule management (mileage + time)
  - Overdue detection
  - Work order lifecycle management
  
- `GeofenceService` - Geofencing and alerts
  - Point-in-polygon detection
  - Entry/Exit/Both alert types
  - Real-time violation checking
  
- `DriverService` - Driver management
  - License and medical certificate validation
  - Driver qualification checks
  - Termination workflow

**Domain Layer:**
- `ValueObjects/Money` - Currency handling
- `ValueObjects/Distance` - Distance with unit conversion
- `ValueObjects/Duration` - Time span operations
- `Rules/MaintenanceRules` - Maintenance interval calculations
- `Rules/DOTComplianceRules` - Federal HOS regulations in code
- `Rules/FuelCardValidationRules` - Transaction limits and fraud detection

**Specifications (Pattern Implementation):**
- `Specification<T>` - Base specification with And/Or/Not combinators
- `VehicleSpecifications` - Active, Available, ByFleet, ByFuelType, etc.
- `DriverSpecifications` - Active, ExpiredLicense, ExpiredMedical, ByCDL

**Integration Adapters:**
- `FuelCardAdapter` - WCF integration with fuel card processor
- `DOTReportingAdapter` - Integration with FMCSA ELD reporting system
- `TelematicsAdapter` (interface defined) - GPS device communication

### TransFleet.Data (Data Access Layer)
**Entities (60+ domain entities, 10 shown):**
- `Vehicle` - VIN, Make, Model, Year, Odometer, Status, FuelType
- `Driver` - License, CDL class, Medical cert, HireDate
- `Fleet` - Client assignment, Status
- `Client` - Company details
- `MaintenanceSchedule` - ServiceType, Intervals, NextService
- `FuelTransaction` - Gallons, Amount, Location, Status
- `GPSPosition` - Lat/Lon, Speed, Heading (100M+ rows in production)
- `HOSLog` - DutyStatus, StartTime, EndTime (DOT compliance)
- `Geofence` - Polygon boundary, AlertType
- `WorkOrder` - Type, Priority, Status, Cost

**Data Access Patterns:**
- `TransFleetDbContext` - Entity Framework 6 DbContext
- `Repository<T>` - Generic repository with Find/Add/Update/Remove
- `UnitOfWork` - Transaction boundary and SaveChanges coordination

### TransFleet.WcfServices (WCF Service Layer)
**Services:**
- `ITelematicsService` / `TelematicsService`
  - `ReceiveVehicleData` - Ingest GPS, odometer, fuel level from devices
  - `GetVehicleStatus` - Query current vehicle status
  - `SendCommand` - Remote vehicle commands (disable engine, lock doors)

**Data Contracts:**
- `VehicleTelematicsData` - GPS position, speed, heading, odometer, fuel
- `VehicleStatus` - Online status, last contact time
- `VehicleCommand` - Command type and parameters

### TransFleet.Jobs (Background Processing)
**Scheduled Jobs:**
- `MaintenanceAlertJob` - Scans for overdue maintenance every 6 hours
- `ComplianceCheckJob` - Daily driver HOS compliance validation
- `DataArchivalJob` - Archives old GPS positions every 90 days

## Key Complexity Factors for Reverse Engineering

### Deep Nesting and Business Rules
- `VehicleService.CreateVehicle` has 7 validation rules
- `VehicleService.AssignDriver` has 6 qualification checks
- `DOTComplianceRules.IsDriverInCompliance` chains multiple sub-calculations
- `FuelService.ProcessFuelTransaction` validates against fraud patterns

### Multiple Integration Points
- Fuel card processor (WCF)
- DOT reporting system (WCF)
- Telematics devices (WCF bidirectional)
- GPS position ingestion (high volume)

### Database Complexity
- 150+ tables (10 core entities shown)
- 100M+ GPS position records
- Complex entity relationships
- EF6 Database-First with EDMX

### Cross-Cutting Concerns
- Specification pattern used throughout
- Repository/UnitOfWork abstraction
- Value objects for domain concepts
- Integration adapters for external systems

## Realistic Legacy Patterns

This codebase demonstrates authentic enterprise patterns that Spec2Cloud will extract:

1. **God Service Classes** - VehicleService is 500+ lines with many responsibilities
2. **Business Rules in Code** - DOT regulations hardcoded (not externalized)
3. **Deep Call Chains** - Vehicle assignment → driver check → license validation → compliance check
4. **Mixed Abstractions** - Repository pattern alongside direct EF6 queries
5. **WCF Integration** - Legacy service contracts with DataContracts
6. **Configuration in XML** - Web.config with connection strings and endpoints
7. **Nested Conditionals** - Complex validation logic with multiple branches
8. **Pattern Overuse** - Specification pattern for simple queries

These characteristics make it an excellent candidate for:
- API contract extraction (OpenAPI generation)
- Business rule cataloging
- Data model reverse engineering
- Integration point documentation
- Bounded context identification (microservices decomposition)
- Modernization blueprint creation
