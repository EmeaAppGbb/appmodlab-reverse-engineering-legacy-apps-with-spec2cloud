# TransFleet Legacy Application - Build and Development Guide

## Overview
TransFleet is a legacy enterprise fleet management system built on .NET Framework 4.7.2. This application demonstrates complex patterns for reverse engineering with Spec2Cloud.

## Prerequisites
- Visual Studio 2022
- .NET Framework 4.7.2 SDK
- SQL Server 2014 or later (LocalDB works for development)
- IIS Express or IIS

## Building the Application

### Using Visual Studio
1. Open `TransFleet.sln`
2. Restore NuGet packages (right-click solution → Restore NuGet Packages)
3. Build solution (F6 or Build → Build Solution)

### Using Command Line
```bash
# Restore packages
dotnet restore TransFleet.sln

# Build
msbuild TransFleet.sln /p:Configuration=Release
```

## Database Setup

The application uses Entity Framework 6 Code First with automatic migrations. On first run, the database will be created automatically using the connection string in Web.config.

Default connection string:
```
Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=TransFleet;Integrated Security=True
```

## Running the Application

### Web API
The TransFleet.WebApi project hosts REST API endpoints:
- Press F5 in Visual Studio to start with debugging
- API will be available at: `http://localhost:5000/api`

### Key API Endpoints
- `GET /api/vehicles` - List all vehicles
- `GET /api/vehicles/{id}` - Get vehicle details
- `POST /api/vehicles` - Create new vehicle
- `GET /api/compliance/driver/{id}/report` - Get DOT compliance report
- `POST /api/fuel/transactions` - Process fuel transaction
- `GET /api/maintenance/schedules/overdue/fleet/{id}` - Get overdue maintenance

### WCF Services
The TelematicsService provides vehicle data ingestion:
- Endpoint: `http://localhost:5000/TelematicsService.svc`
- Contract: `ITelematicsService`

## Architecture Highlights

### Layered Architecture
- **TransFleet.WebApi** - ASP.NET Web API 2 controllers
- **TransFleet.Core** - Domain services and business logic
- **TransFleet.Data** - Entity Framework 6 data access with Repository/UnitOfWork patterns
- **TransFleet.WcfServices** - WCF service contracts
- **TransFleet.Jobs** - Background jobs (Hangfire/Quartz.NET)

### Design Patterns Demonstrated
- Repository Pattern
- Unit of Work Pattern
- Specification Pattern
- Domain Services
- Value Objects
- Integration Adapters

### Complex Business Rules
- DOT Hours of Service compliance calculations
- Maintenance scheduling based on mileage and time
- Fuel card transaction validation
- Geofencing alerts
- Vehicle health scoring

## Background Jobs

The application includes scheduled jobs:
- **MaintenanceAlertJob** - Checks for overdue maintenance every 6 hours
- **ComplianceCheckJob** - Validates driver HOS compliance daily
- **DataArchivalJob** - Archives old GPS positions

## Testing

Example API calls using curl:

```bash
# Get vehicle health report
curl http://localhost:5000/api/vehicles/1/health

# Check driver compliance
curl http://localhost:5000/api/compliance/driver/1/check

# Get fuel efficiency report
curl http://localhost:5000/api/fuel/efficiency/vehicle/1?startDate=2024-01-01&endDate=2024-12-31
```

## Key Business Domain Concepts

### Fleet Management
- Vehicles organized into fleets by client
- Driver assignment with qualification checks
- Real-time GPS tracking

### Maintenance
- Scheduled maintenance based on mileage and time intervals
- Work orders with priority and status tracking
- Automatic alerts for overdue maintenance

### DOT Compliance
- Hours of Service (HOS) logging
- 11-hour driving limit
- 14-hour on-duty limit
- 30-minute break requirement
- Violation tracking and reporting

### Fuel Management
- Fuel card transaction processing
- Fraud detection for suspicious transactions
- MPG calculations and efficiency reporting

## Configuration

Key settings in Web.config:
- `TransFleetConnection` - Database connection string
- `FuelCardServiceEndpoint` - External fuel card processor
- `DOTReportingEndpoint` - DOT reporting system
- `CarrierNumber` - DOT carrier identification

## Legacy Code Characteristics

This codebase intentionally demonstrates realistic legacy patterns:
- Large service classes (1200+ lines)
- Mixed concerns in some methods
- Database-First EF6 with EDMX
- WCF for integration
- Configuration in XML
- Nested business rule logic
- Specification pattern usage

These patterns make it an ideal candidate for Spec2Cloud analysis and modernization planning.
