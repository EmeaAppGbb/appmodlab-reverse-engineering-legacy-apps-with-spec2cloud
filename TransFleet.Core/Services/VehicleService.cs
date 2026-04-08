using System;
using System.Collections.Generic;
using System.Linq;
using TransFleet.Data;
using TransFleet.Data.Entities;
using TransFleet.Core.Domain.Rules;

namespace TransFleet.Core.Services
{
    public interface IVehicleService
    {
        Vehicle GetVehicleById(int vehicleId);
        IEnumerable<Vehicle> GetVehiclesByFleet(int fleetId);
        IEnumerable<Vehicle> GetVehiclesByStatus(string status);
        void CreateVehicle(Vehicle vehicle);
        void UpdateVehicle(Vehicle vehicle);
        void DeactivateVehicle(int vehicleId, string reason);
        void AssignDriver(int vehicleId, int driverId);
        void UnassignDriver(int vehicleId);
        bool IsVehicleAvailableForAssignment(int vehicleId);
        VehicleHealthReport GetVehicleHealthReport(int vehicleId);
        VehicleUtilizationReport GetUtilizationReport(int vehicleId, DateTime startDate, DateTime endDate);
        decimal CalculateTotalMaintenanceCost(int vehicleId, DateTime startDate, DateTime endDate);
    }

    public class VehicleService : IVehicleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public VehicleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public Vehicle GetVehicleById(int vehicleId)
        {
            return _unitOfWork.Repository<Vehicle>().GetById(vehicleId);
        }

        public IEnumerable<Vehicle> GetVehiclesByFleet(int fleetId)
        {
            return _unitOfWork.Repository<Vehicle>()
                .Find(v => v.FleetId == fleetId && v.Status != "Decommissioned");
        }

        public IEnumerable<Vehicle> GetVehiclesByStatus(string status)
        {
            return _unitOfWork.Repository<Vehicle>().Find(v => v.Status == status);
        }

        public void CreateVehicle(Vehicle vehicle)
        {
            if (vehicle == null)
                throw new ArgumentNullException(nameof(vehicle));

            // Business rule: VIN must be unique
            var existingVehicle = _unitOfWork.Repository<Vehicle>()
                .Find(v => v.VIN == vehicle.VIN).FirstOrDefault();
            
            if (existingVehicle != null)
                throw new InvalidOperationException($"A vehicle with VIN {vehicle.VIN} already exists.");

            // Business rule: License plate must be unique within the fleet
            var existingPlate = _unitOfWork.Repository<Vehicle>()
                .Find(v => v.LicensePlate == vehicle.LicensePlate && v.FleetId == vehicle.FleetId)
                .FirstOrDefault();
            
            if (existingPlate != null)
                throw new InvalidOperationException($"A vehicle with license plate {vehicle.LicensePlate} already exists in this fleet.");

            // Business rule: Default status is Active
            if (string.IsNullOrEmpty(vehicle.Status))
                vehicle.Status = "Active";

            // Business rule: Vehicle year must be reasonable
            if (vehicle.Year < 1990 || vehicle.Year > DateTime.Now.Year + 1)
                throw new InvalidOperationException($"Invalid vehicle year: {vehicle.Year}");

            vehicle.CreatedDate = DateTime.UtcNow;
            vehicle.ModifiedDate = DateTime.UtcNow;

            _unitOfWork.Repository<Vehicle>().Add(vehicle);
            _unitOfWork.SaveChanges();

            // Automatically create default maintenance schedules
            CreateDefaultMaintenanceSchedules(vehicle);
        }

        public void UpdateVehicle(Vehicle vehicle)
        {
            if (vehicle == null)
                throw new ArgumentNullException(nameof(vehicle));

            var existing = _unitOfWork.Repository<Vehicle>().GetById(vehicle.VehicleId);
            if (existing == null)
                throw new InvalidOperationException($"Vehicle with ID {vehicle.VehicleId} not found.");

            // Business rule: Cannot change VIN
            if (existing.VIN != vehicle.VIN)
                throw new InvalidOperationException("VIN cannot be changed after vehicle creation.");

            // Business rule: Odometer can only increase
            if (vehicle.OdometerReading < existing.OdometerReading)
                throw new InvalidOperationException("Odometer reading cannot decrease.");

            // Business rule: Cannot reactivate a decommissioned vehicle
            if (existing.Status == "Decommissioned" && vehicle.Status != "Decommissioned")
                throw new InvalidOperationException("Cannot reactivate a decommissioned vehicle.");

            vehicle.ModifiedDate = DateTime.UtcNow;

            _unitOfWork.Repository<Vehicle>().Update(vehicle);
            _unitOfWork.SaveChanges();

            // Check if maintenance schedules need updating based on odometer
            CheckAndUpdateMaintenanceSchedules(vehicle);
        }

        public void DeactivateVehicle(int vehicleId, string reason)
        {
            var vehicle = _unitOfWork.Repository<Vehicle>().GetById(vehicleId);
            if (vehicle == null)
                throw new InvalidOperationException($"Vehicle with ID {vehicleId} not found.");

            // Business rule: Must unassign driver before deactivation
            if (vehicle.CurrentDriverId.HasValue)
            {
                UnassignDriver(vehicleId);
            }

            // Business rule: Must complete or cancel open work orders
            var openWorkOrders = _unitOfWork.Repository<WorkOrder>()
                .Find(wo => wo.VehicleId == vehicleId && 
                           (wo.Status == "Open" || wo.Status == "InProgress"))
                .ToList();

            if (openWorkOrders.Any())
            {
                throw new InvalidOperationException(
                    $"Cannot deactivate vehicle. There are {openWorkOrders.Count} open work orders.");
            }

            vehicle.Status = "Decommissioned";
            vehicle.ModifiedDate = DateTime.UtcNow;

            _unitOfWork.Repository<Vehicle>().Update(vehicle);
            _unitOfWork.SaveChanges();
        }

        public void AssignDriver(int vehicleId, int driverId)
        {
            var vehicle = _unitOfWork.Repository<Vehicle>().GetById(vehicleId);
            if (vehicle == null)
                throw new InvalidOperationException($"Vehicle with ID {vehicleId} not found.");

            var driver = _unitOfWork.Repository<Driver>().GetById(driverId);
            if (driver == null)
                throw new InvalidOperationException($"Driver with ID {driverId} not found.");

            // Business rule: Vehicle must be in Active status
            if (vehicle.Status != "Active")
                throw new InvalidOperationException($"Cannot assign driver to vehicle with status: {vehicle.Status}");

            // Business rule: Driver must be in Active status
            if (driver.Status != "Active")
                throw new InvalidOperationException($"Cannot assign vehicle to driver with status: {driver.Status}");

            // Business rule: Driver license must not be expired
            if (driver.LicenseExpiry < DateTime.UtcNow)
                throw new InvalidOperationException("Driver license has expired.");

            // Business rule: Driver medical certificate must be valid (if commercial)
            if (!string.IsNullOrEmpty(driver.CDLClass) && 
                driver.MedicalCertExpiry.HasValue && 
                driver.MedicalCertExpiry.Value < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Driver medical certificate has expired.");
            }

            // Business rule: Check if driver is already assigned to another vehicle
            var driverCurrentVehicle = _unitOfWork.Repository<Vehicle>()
                .Find(v => v.CurrentDriverId == driverId && v.VehicleId != vehicleId)
                .FirstOrDefault();
            
            if (driverCurrentVehicle != null)
            {
                throw new InvalidOperationException(
                    $"Driver is already assigned to vehicle {driverCurrentVehicle.VIN}. Unassign first.");
            }

            vehicle.CurrentDriverId = driverId;
            vehicle.ModifiedDate = DateTime.UtcNow;

            _unitOfWork.Repository<Vehicle>().Update(vehicle);
            _unitOfWork.SaveChanges();
        }

        public void UnassignDriver(int vehicleId)
        {
            var vehicle = _unitOfWork.Repository<Vehicle>().GetById(vehicleId);
            if (vehicle == null)
                throw new InvalidOperationException($"Vehicle with ID {vehicleId} not found.");

            if (!vehicle.CurrentDriverId.HasValue)
                throw new InvalidOperationException("Vehicle does not have an assigned driver.");

            vehicle.CurrentDriverId = null;
            vehicle.ModifiedDate = DateTime.UtcNow;

            _unitOfWork.Repository<Vehicle>().Update(vehicle);
            _unitOfWork.SaveChanges();
        }

        public bool IsVehicleAvailableForAssignment(int vehicleId)
        {
            var vehicle = _unitOfWork.Repository<Vehicle>().GetById(vehicleId);
            if (vehicle == null)
                return false;

            // Vehicle is available if it's Active and has no current driver
            if (vehicle.Status != "Active" || vehicle.CurrentDriverId.HasValue)
                return false;

            // Check for overdue maintenance
            var maintenanceSchedules = _unitOfWork.Repository<MaintenanceSchedule>()
                .Find(ms => ms.VehicleId == vehicleId && ms.Status == "Active")
                .ToList();

            foreach (var schedule in maintenanceSchedules)
            {
                if (MaintenanceRules.IsMaintenanceOverdue(schedule, vehicle))
                    return false; // Cannot assign if maintenance is overdue
            }

            return true;
        }

        public VehicleHealthReport GetVehicleHealthReport(int vehicleId)
        {
            var vehicle = _unitOfWork.Repository<Vehicle>().GetById(vehicleId);
            if (vehicle == null)
                throw new InvalidOperationException($"Vehicle with ID {vehicleId} not found.");

            var report = new VehicleHealthReport
            {
                VehicleId = vehicleId,
                VIN = vehicle.VIN,
                CurrentOdometer = vehicle.OdometerReading,
                OverallHealthScore = 100 // Start at perfect
            };

            // Check maintenance status
            var maintenanceSchedules = _unitOfWork.Repository<MaintenanceSchedule>()
                .Find(ms => ms.VehicleId == vehicleId && ms.Status == "Active")
                .ToList();

            var overdueCount = 0;
            var upcomingCount = 0;

            foreach (var schedule in maintenanceSchedules)
            {
                if (MaintenanceRules.IsMaintenanceOverdue(schedule, vehicle))
                {
                    overdueCount++;
                    report.OverallHealthScore -= 20;
                }
                else if (schedule.NextServiceDate.HasValue && 
                         schedule.NextServiceDate.Value <= DateTime.UtcNow.AddDays(30))
                {
                    upcomingCount++;
                    report.OverallHealthScore -= 5;
                }
            }

            report.OverdueMaintenanceCount = overdueCount;
            report.UpcomingMaintenanceCount = upcomingCount;

            // Check work orders
            var openWorkOrders = _unitOfWork.Repository<WorkOrder>()
                .Find(wo => wo.VehicleId == vehicleId && 
                           (wo.Status == "Open" || wo.Status == "InProgress"))
                .ToList();

            report.OpenWorkOrderCount = openWorkOrders.Count;
            
            var criticalWorkOrders = openWorkOrders.Count(wo => wo.Priority == "Critical");
            report.OverallHealthScore -= criticalWorkOrders * 15;

            // Ensure score doesn't go below 0
            report.OverallHealthScore = Math.Max(0, report.OverallHealthScore);

            report.HealthStatus = report.OverallHealthScore >= 80 ? "Good" :
                                  report.OverallHealthScore >= 50 ? "Fair" : "Poor";

            return report;
        }

        public VehicleUtilizationReport GetUtilizationReport(int vehicleId, DateTime startDate, DateTime endDate)
        {
            var vehicle = _unitOfWork.Repository<Vehicle>().GetById(vehicleId);
            if (vehicle == null)
                throw new InvalidOperationException($"Vehicle with ID {vehicleId} not found.");

            var report = new VehicleUtilizationReport
            {
                VehicleId = vehicleId,
                VIN = vehicle.VIN,
                ReportStartDate = startDate,
                ReportEndDate = endDate
            };

            // Calculate miles driven
            var gpsPositions = _unitOfWork.Repository<GPSPosition>()
                .Find(gps => gps.VehicleId == vehicleId && 
                            gps.Timestamp >= startDate && 
                            gps.Timestamp <= endDate)
                .OrderBy(gps => gps.Timestamp)
                .ToList();

            if (gpsPositions.Any())
            {
                report.TotalMilesDriven = CalculateTotalMiles(gpsPositions);
                report.AverageSpeed = gpsPositions.Average(gps => gps.Speed ?? 0);
            }

            // Calculate fuel consumption
            var fuelTransactions = _unitOfWork.Repository<FuelTransaction>()
                .Find(ft => ft.VehicleId == vehicleId && 
                           ft.TransactionDate >= startDate && 
                           ft.TransactionDate <= endDate)
                .ToList();

            report.TotalFuelGallons = fuelTransactions.Sum(ft => ft.Gallons);
            report.TotalFuelCost = fuelTransactions.Sum(ft => ft.Amount);

            if (report.TotalFuelGallons > 0 && report.TotalMilesDriven > 0)
            {
                report.AverageMPG = (decimal)report.TotalMilesDriven / report.TotalFuelGallons;
            }

            // Calculate active days
            var activeDays = gpsPositions
                .Select(gps => gps.Timestamp.Date)
                .Distinct()
                .Count();

            report.ActiveDays = activeDays;
            report.TotalDays = (endDate - startDate).Days + 1;
            report.UtilizationPercentage = report.TotalDays > 0 
                ? (decimal)activeDays / report.TotalDays * 100 
                : 0;

            return report;
        }

        public decimal CalculateTotalMaintenanceCost(int vehicleId, DateTime startDate, DateTime endDate)
        {
            var workOrders = _unitOfWork.Repository<WorkOrder>()
                .Find(wo => wo.VehicleId == vehicleId && 
                           wo.CompletedDate.HasValue &&
                           wo.CompletedDate.Value >= startDate && 
                           wo.CompletedDate.Value <= endDate &&
                           wo.ActualCost.HasValue)
                .ToList();

            return workOrders.Sum(wo => wo.ActualCost.Value);
        }

        private void CreateDefaultMaintenanceSchedules(Vehicle vehicle)
        {
            var defaultSchedules = new[]
            {
                new MaintenanceSchedule
                {
                    VehicleId = vehicle.VehicleId,
                    ServiceType = "OilChange",
                    IntervalMiles = MaintenanceRules.OilChangeIntervalMiles,
                    IntervalDays = 180,
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow,
                    NextServiceMileage = vehicle.OdometerReading + MaintenanceRules.OilChangeIntervalMiles,
                    NextServiceDate = DateTime.UtcNow.AddDays(180)
                },
                new MaintenanceSchedule
                {
                    VehicleId = vehicle.VehicleId,
                    ServiceType = "TireRotation",
                    IntervalMiles = MaintenanceRules.TireRotationIntervalMiles,
                    IntervalDays = 180,
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow,
                    NextServiceMileage = vehicle.OdometerReading + MaintenanceRules.TireRotationIntervalMiles,
                    NextServiceDate = DateTime.UtcNow.AddDays(180)
                },
                new MaintenanceSchedule
                {
                    VehicleId = vehicle.VehicleId,
                    ServiceType = "AnnualInspection",
                    IntervalDays = MaintenanceRules.AnnualInspectionIntervalDays,
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow,
                    NextServiceDate = DateTime.UtcNow.AddDays(MaintenanceRules.AnnualInspectionIntervalDays)
                }
            };

            foreach (var schedule in defaultSchedules)
            {
                _unitOfWork.Repository<MaintenanceSchedule>().Add(schedule);
            }

            _unitOfWork.SaveChanges();
        }

        private void CheckAndUpdateMaintenanceSchedules(Vehicle vehicle)
        {
            var schedules = _unitOfWork.Repository<MaintenanceSchedule>()
                .Find(ms => ms.VehicleId == vehicle.VehicleId && ms.Status == "Active")
                .ToList();

            foreach (var schedule in schedules)
            {
                if (MaintenanceRules.IsMaintenanceOverdue(schedule, vehicle))
                {
                    schedule.Status = "Overdue";
                    _unitOfWork.Repository<MaintenanceSchedule>().Update(schedule);
                }
            }

            _unitOfWork.SaveChanges();
        }

        private double CalculateTotalMiles(List<GPSPosition> positions)
        {
            if (positions.Count < 2)
                return 0;

            double totalMiles = 0;
            for (int i = 1; i < positions.Count; i++)
            {
                var distance = CalculateDistance(
                    (double)positions[i - 1].Latitude, (double)positions[i - 1].Longitude,
                    (double)positions[i].Latitude, (double)positions[i].Longitude);
                totalMiles += distance;
            }

            return totalMiles;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula for calculating distance between two GPS coordinates
            const double earthRadiusMiles = 3959;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusMiles * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }

    public class VehicleHealthReport
    {
        public int VehicleId { get; set; }
        public string VIN { get; set; }
        public int CurrentOdometer { get; set; }
        public int OverallHealthScore { get; set; }
        public string HealthStatus { get; set; }
        public int OverdueMaintenanceCount { get; set; }
        public int UpcomingMaintenanceCount { get; set; }
        public int OpenWorkOrderCount { get; set; }
    }

    public class VehicleUtilizationReport
    {
        public int VehicleId { get; set; }
        public string VIN { get; set; }
        public DateTime ReportStartDate { get; set; }
        public DateTime ReportEndDate { get; set; }
        public double TotalMilesDriven { get; set; }
        public decimal AverageSpeed { get; set; }
        public decimal TotalFuelGallons { get; set; }
        public decimal TotalFuelCost { get; set; }
        public decimal AverageMPG { get; set; }
        public int ActiveDays { get; set; }
        public int TotalDays { get; set; }
        public decimal UtilizationPercentage { get; set; }
    }
}
