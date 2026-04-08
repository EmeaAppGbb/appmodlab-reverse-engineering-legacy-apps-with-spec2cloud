using System;
using System.Collections.Generic;
using System.Linq;
using TransFleet.Data.Entities;

namespace TransFleet.Core.Domain.Rules
{
    public static class DOTComplianceRules
    {
        // DOT Hours of Service regulations
        public const double MaxDrivingHoursPerDay = 11.0;
        public const double MaxOnDutyHoursPerDay = 14.0;
        public const double MaxDrivingHoursPerWeek = 60.0; // 7-day period
        public const double MaxDrivingHoursPerWeek8Day = 70.0; // 8-day period
        public const double RequiredOffDutyHours = 10.0;
        public const double RequiredRestBreakAfterHours = 8.0;
        public const double MinimumRestBreakDuration = 0.5; // 30 minutes

        public static bool IsDriverInCompliance(List<HOSLog> logs, DateTime checkDate)
        {
            if (logs == null || !logs.Any())
                return true;

            var dailyLogs = logs.Where(l => l.StartTime.Date == checkDate.Date).ToList();
            
            var drivingHours = CalculateDrivingHours(dailyLogs);
            var onDutyHours = CalculateOnDutyHours(dailyLogs);

            if (drivingHours > MaxDrivingHoursPerDay)
                return false;

            if (onDutyHours > MaxOnDutyHoursPerDay)
                return false;

            var weeklyLogs = logs.Where(l => 
                l.StartTime >= checkDate.AddDays(-7) && 
                l.StartTime <= checkDate).ToList();
            
            var weeklyDrivingHours = CalculateDrivingHours(weeklyLogs);
            if (weeklyDrivingHours > MaxDrivingHoursPerWeek)
                return false;

            if (!HasRequiredRestBreak(dailyLogs))
                return false;

            return true;
        }

        public static double CalculateDrivingHours(List<HOSLog> logs)
        {
            return logs
                .Where(l => l.DutyStatus == "Driving" && l.EndTime.HasValue)
                .Sum(l => (l.EndTime.Value - l.StartTime).TotalHours);
        }

        public static double CalculateOnDutyHours(List<HOSLog> logs)
        {
            return logs
                .Where(l => (l.DutyStatus == "Driving" || l.DutyStatus == "OnDuty") && l.EndTime.HasValue)
                .Sum(l => (l.EndTime.Value - l.StartTime).TotalHours);
        }

        public static bool HasRequiredRestBreak(List<HOSLog> logs)
        {
            var drivingLogs = logs
                .Where(l => l.DutyStatus == "Driving" && l.EndTime.HasValue)
                .OrderBy(l => l.StartTime)
                .ToList();

            if (!drivingLogs.Any())
                return true;

            double continuousDrivingHours = 0;
            DateTime? lastDrivingEnd = null;

            foreach (var log in drivingLogs)
            {
                if (lastDrivingEnd.HasValue)
                {
                    var breakDuration = (log.StartTime - lastDrivingEnd.Value).TotalHours;
                    if (breakDuration >= MinimumRestBreakDuration)
                    {
                        continuousDrivingHours = 0;
                    }
                }

                continuousDrivingHours += (log.EndTime.Value - log.StartTime).TotalHours;
                
                if (continuousDrivingHours > RequiredRestBreakAfterHours)
                    return false;

                lastDrivingEnd = log.EndTime;
            }

            return true;
        }

        public static double GetRemainingDrivingHours(List<HOSLog> logs, DateTime checkDate)
        {
            var dailyLogs = logs.Where(l => l.StartTime.Date == checkDate.Date).ToList();
            var drivingHours = CalculateDrivingHours(dailyLogs);
            return Math.Max(0, MaxDrivingHoursPerDay - drivingHours);
        }
    }
}
