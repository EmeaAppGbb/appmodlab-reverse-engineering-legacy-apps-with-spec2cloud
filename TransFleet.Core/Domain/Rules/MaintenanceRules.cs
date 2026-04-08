using System;
using TransFleet.Data.Entities;

namespace TransFleet.Core.Domain.Rules
{
    public static class MaintenanceRules
    {
        public const int OilChangeIntervalMiles = 5000;
        public const int TireRotationIntervalMiles = 7500;
        public const int BrakeInspectionIntervalMonths = 6;
        public const int AnnualInspectionIntervalDays = 365;

        public static bool IsMaintenanceOverdue(MaintenanceSchedule schedule, Vehicle vehicle)
        {
            if (schedule == null || vehicle == null)
                return false;

            var now = DateTime.UtcNow;
            
            // Check mileage-based interval
            if (schedule.NextServiceMileage.HasValue && 
                vehicle.OdometerReading >= schedule.NextServiceMileage.Value)
            {
                return true;
            }

            // Check date-based interval
            if (schedule.NextServiceDate.HasValue && 
                now >= schedule.NextServiceDate.Value)
            {
                return true;
            }

            return false;
        }

        public static DateTime CalculateNextServiceDate(DateTime lastServiceDate, int intervalDays)
        {
            return lastServiceDate.AddDays(intervalDays);
        }

        public static int CalculateNextServiceMileage(int lastServiceMileage, int intervalMiles)
        {
            return lastServiceMileage + intervalMiles;
        }

        public static int GetMaintenanceIntervalForServiceType(string serviceType)
        {
            return serviceType switch
            {
                "OilChange" => OilChangeIntervalMiles,
                "TireRotation" => TireRotationIntervalMiles,
                "BrakeInspection" => BrakeInspectionIntervalMonths * 30 * 100, // Rough estimate
                "AnnualInspection" => AnnualInspectionIntervalDays * 100,
                _ => 10000 // Default
            };
        }
    }
}
