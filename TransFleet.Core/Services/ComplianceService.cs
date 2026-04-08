using System;
using System.Collections.Generic;
using System.Linq;
using TransFleet.Data;
using TransFleet.Data.Entities;
using TransFleet.Core.Domain.Rules;

namespace TransFleet.Core.Services
{
    public interface IComplianceService
    {
        bool CheckDriverCompliance(int driverId, DateTime checkDate);
        DOTComplianceReport GetComplianceReport(int driverId, DateTime startDate, DateTime endDate);
        double GetRemainingDrivingHours(int driverId, DateTime checkDate);
        IEnumerable<ComplianceViolation> GetViolations(int driverId, DateTime startDate, DateTime endDate);
        void RecordDutyStatusChange(int driverId, string dutyStatus, int? vehicleId, decimal? latitude, decimal? longitude, string remarks);
    }

    public class ComplianceService : IComplianceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ComplianceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public bool CheckDriverCompliance(int driverId, DateTime checkDate)
        {
            var logs = _unitOfWork.Repository<HOSLog>()
                .Find(l => l.DriverId == driverId && 
                          l.StartTime >= checkDate.AddDays(-7) && 
                          l.StartTime <= checkDate.AddDays(1))
                .ToList();

            return DOTComplianceRules.IsDriverInCompliance(logs, checkDate);
        }

        public DOTComplianceReport GetComplianceReport(int driverId, DateTime startDate, DateTime endDate)
        {
            var driver = _unitOfWork.Repository<Driver>().GetById(driverId);
            if (driver == null)
                throw new InvalidOperationException($"Driver with ID {driverId} not found.");

            var logs = _unitOfWork.Repository<HOSLog>()
                .Find(l => l.DriverId == driverId && 
                          l.StartTime >= startDate && 
                          l.StartTime <= endDate)
                .ToList();

            var report = new DOTComplianceReport
            {
                DriverId = driverId,
                DriverName = $"{driver.FirstName} {driver.LastName}",
                ReportStartDate = startDate,
                ReportEndDate = endDate
            };

            // Calculate daily compliance
            var currentDate = startDate.Date;
            var dailyReports = new List<DailyComplianceReport>();

            while (currentDate <= endDate.Date)
            {
                var dailyLogs = logs.Where(l => l.StartTime.Date == currentDate).ToList();
                
                var dailyReport = new DailyComplianceReport
                {
                    Date = currentDate,
                    DrivingHours = DOTComplianceRules.CalculateDrivingHours(dailyLogs),
                    OnDutyHours = DOTComplianceRules.CalculateOnDutyHours(dailyLogs),
                    IsCompliant = DOTComplianceRules.IsDriverInCompliance(
                        logs.Where(l => l.StartTime >= currentDate.AddDays(-7) && l.StartTime <= currentDate).ToList(),
                        currentDate)
                };

                if (!dailyReport.IsCompliant)
                {
                    report.ViolationCount++;
                }

                dailyReports.Add(dailyReport);
                currentDate = currentDate.AddDays(1);
            }

            report.DailyReports = dailyReports;
            report.TotalDrivingHours = dailyReports.Sum(d => d.DrivingHours);
            report.TotalOnDutyHours = dailyReports.Sum(d => d.OnDutyHours);
            report.OverallCompliance = report.ViolationCount == 0;

            return report;
        }

        public double GetRemainingDrivingHours(int driverId, DateTime checkDate)
        {
            var logs = _unitOfWork.Repository<HOSLog>()
                .Find(l => l.DriverId == driverId && l.StartTime.Date == checkDate.Date)
                .ToList();

            return DOTComplianceRules.GetRemainingDrivingHours(logs, checkDate);
        }

        public IEnumerable<ComplianceViolation> GetViolations(int driverId, DateTime startDate, DateTime endDate)
        {
            var violations = new List<ComplianceViolation>();
            var logs = _unitOfWork.Repository<HOSLog>()
                .Find(l => l.DriverId == driverId && 
                          l.StartTime >= startDate && 
                          l.StartTime <= endDate)
                .ToList();

            var currentDate = startDate.Date;
            while (currentDate <= endDate.Date)
            {
                var dailyLogs = logs.Where(l => l.StartTime.Date == currentDate).ToList();
                var drivingHours = DOTComplianceRules.CalculateDrivingHours(dailyLogs);
                var onDutyHours = DOTComplianceRules.CalculateOnDutyHours(dailyLogs);

                if (drivingHours > DOTComplianceRules.MaxDrivingHoursPerDay)
                {
                    violations.Add(new ComplianceViolation
                    {
                        DriverId = driverId,
                        ViolationDate = currentDate,
                        ViolationType = "ExcessiveDrivingHours",
                        Description = $"Driving hours ({drivingHours:F2}) exceeded daily limit of {DOTComplianceRules.MaxDrivingHoursPerDay}",
                        Severity = "High"
                    });
                }

                if (onDutyHours > DOTComplianceRules.MaxOnDutyHoursPerDay)
                {
                    violations.Add(new ComplianceViolation
                    {
                        DriverId = driverId,
                        ViolationDate = currentDate,
                        ViolationType = "ExcessiveOnDutyHours",
                        Description = $"On-duty hours ({onDutyHours:F2}) exceeded daily limit of {DOTComplianceRules.MaxOnDutyHoursPerDay}",
                        Severity = "High"
                    });
                }

                if (!DOTComplianceRules.HasRequiredRestBreak(dailyLogs))
                {
                    violations.Add(new ComplianceViolation
                    {
                        DriverId = driverId,
                        ViolationDate = currentDate,
                        ViolationType = "MissingRestBreak",
                        Description = $"Required rest break not taken after {DOTComplianceRules.RequiredRestBreakAfterHours} hours of driving",
                        Severity = "Medium"
                    });
                }

                currentDate = currentDate.AddDays(1);
            }

            return violations;
        }

        public void RecordDutyStatusChange(int driverId, string dutyStatus, int? vehicleId, decimal? latitude, decimal? longitude, string remarks)
        {
            var driver = _unitOfWork.Repository<Driver>().GetById(driverId);
            if (driver == null)
                throw new InvalidOperationException($"Driver with ID {driverId} not found.");

            // Close any open HOS log for this driver
            var openLog = _unitOfWork.Repository<HOSLog>()
                .Find(l => l.DriverId == driverId && !l.EndTime.HasValue)
                .OrderByDescending(l => l.StartTime)
                .FirstOrDefault();

            if (openLog != null)
            {
                openLog.EndTime = DateTime.UtcNow;
                _unitOfWork.Repository<HOSLog>().Update(openLog);
            }

            // Create new HOS log entry
            var newLog = new HOSLog
            {
                DriverId = driverId,
                DutyStatus = dutyStatus,
                StartTime = DateTime.UtcNow,
                VehicleId = vehicleId,
                Latitude = latitude,
                Longitude = longitude,
                Remarks = remarks,
                RecordStatus = "Active",
                RecordOrigin = "Manual",
                CreatedDate = DateTime.UtcNow
            };

            _unitOfWork.Repository<HOSLog>().Add(newLog);
            _unitOfWork.SaveChanges();
        }
    }

    public class DOTComplianceReport
    {
        public int DriverId { get; set; }
        public string DriverName { get; set; }
        public DateTime ReportStartDate { get; set; }
        public DateTime ReportEndDate { get; set; }
        public double TotalDrivingHours { get; set; }
        public double TotalOnDutyHours { get; set; }
        public int ViolationCount { get; set; }
        public bool OverallCompliance { get; set; }
        public IEnumerable<DailyComplianceReport> DailyReports { get; set; }
    }

    public class DailyComplianceReport
    {
        public DateTime Date { get; set; }
        public double DrivingHours { get; set; }
        public double OnDutyHours { get; set; }
        public bool IsCompliant { get; set; }
    }

    public class ComplianceViolation
    {
        public int DriverId { get; set; }
        public DateTime ViolationDate { get; set; }
        public string ViolationType { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
    }
}
