using System;
using System.Collections.Generic;
using System.Linq;
using TransFleet.Data;
using TransFleet.Data.Entities;
using TransFleet.Core.Domain.Rules;

namespace TransFleet.Core.Services
{
    public interface IFuelService
    {
        void ProcessFuelTransaction(FuelTransaction transaction);
        IEnumerable<FuelTransaction> GetTransactionsByVehicle(int vehicleId, DateTime startDate, DateTime endDate);
        IEnumerable<FuelTransaction> GetSuspiciousTransactions(int fleetId, DateTime startDate, DateTime endDate);
        FuelEfficiencyReport GetFuelEfficiencyReport(int vehicleId, DateTime startDate, DateTime endDate);
        decimal GetFuelCostByFleet(int fleetId, DateTime startDate, DateTime endDate);
    }

    public class FuelService : IFuelService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FuelService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public void ProcessFuelTransaction(FuelTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            var vehicle = _unitOfWork.Repository<Vehicle>().GetById(transaction.VehicleId);
            if (vehicle == null)
                throw new InvalidOperationException($"Vehicle with ID {transaction.VehicleId} not found.");

            // Validate transaction
            if (!FuelCardValidationRules.IsTransactionValid(transaction, vehicle))
            {
                transaction.Status = "Flagged";
            }
            else
            {
                // Check for suspicious patterns
                var previousTransaction = _unitOfWork.Repository<FuelTransaction>()
                    .Find(ft => ft.VehicleId == transaction.VehicleId && 
                               ft.TransactionDate < transaction.TransactionDate)
                    .OrderByDescending(ft => ft.TransactionDate)
                    .FirstOrDefault();

                if (previousTransaction != null && 
                    FuelCardValidationRules.IsSuspiciousTransaction(transaction, previousTransaction))
                {
                    transaction.Status = "Flagged";
                }
                else
                {
                    transaction.Status = "Approved";
                }
            }

            transaction.CreatedDate = DateTime.UtcNow;
            _unitOfWork.Repository<FuelTransaction>().Add(transaction);
            _unitOfWork.SaveChanges();

            // Update vehicle odometer if provided
            if (transaction.OdometerReading.HasValue && 
                transaction.OdometerReading.Value > vehicle.OdometerReading)
            {
                vehicle.OdometerReading = transaction.OdometerReading.Value;
                vehicle.ModifiedDate = DateTime.UtcNow;
                _unitOfWork.Repository<Vehicle>().Update(vehicle);
                _unitOfWork.SaveChanges();
            }
        }

        public IEnumerable<FuelTransaction> GetTransactionsByVehicle(int vehicleId, DateTime startDate, DateTime endDate)
        {
            return _unitOfWork.Repository<FuelTransaction>()
                .Find(ft => ft.VehicleId == vehicleId && 
                           ft.TransactionDate >= startDate && 
                           ft.TransactionDate <= endDate)
                .OrderBy(ft => ft.TransactionDate);
        }

        public IEnumerable<FuelTransaction> GetSuspiciousTransactions(int fleetId, DateTime startDate, DateTime endDate)
        {
            var vehicles = _unitOfWork.Repository<Vehicle>()
                .Find(v => v.FleetId == fleetId)
                .Select(v => v.VehicleId)
                .ToList();

            return _unitOfWork.Repository<FuelTransaction>()
                .Find(ft => vehicles.Contains(ft.VehicleId) && 
                           ft.TransactionDate >= startDate && 
                           ft.TransactionDate <= endDate &&
                           ft.Status == "Flagged")
                .OrderByDescending(ft => ft.TransactionDate);
        }

        public FuelEfficiencyReport GetFuelEfficiencyReport(int vehicleId, DateTime startDate, DateTime endDate)
        {
            var vehicle = _unitOfWork.Repository<Vehicle>().GetById(vehicleId);
            if (vehicle == null)
                throw new InvalidOperationException($"Vehicle with ID {vehicleId} not found.");

            var transactions = _unitOfWork.Repository<FuelTransaction>()
                .Find(ft => ft.VehicleId == vehicleId && 
                           ft.TransactionDate >= startDate && 
                           ft.TransactionDate <= endDate &&
                           ft.Status == "Approved")
                .OrderBy(ft => ft.TransactionDate)
                .ToList();

            var report = new FuelEfficiencyReport
            {
                VehicleId = vehicleId,
                VIN = vehicle.VIN,
                ReportStartDate = startDate,
                ReportEndDate = endDate,
                TotalGallons = transactions.Sum(t => t.Gallons),
                TotalCost = transactions.Sum(t => t.Amount),
                TransactionCount = transactions.Count
            };

            if (transactions.Any() && transactions.Count > 1)
            {
                var firstTransaction = transactions.First();
                var lastTransaction = transactions.Last();

                if (firstTransaction.OdometerReading.HasValue && lastTransaction.OdometerReading.HasValue)
                {
                    report.MilesDriven = lastTransaction.OdometerReading.Value - firstTransaction.OdometerReading.Value;
                    
                    if (report.TotalGallons > 0)
                    {
                        report.AverageMPG = report.MilesDriven / (double)report.TotalGallons;
                    }
                }
            }

            if (report.TotalGallons > 0)
            {
                report.AveragePricePerGallon = report.TotalCost / report.TotalGallons;
            }

            if (report.MilesDriven > 0)
            {
                report.CostPerMile = report.TotalCost / report.MilesDriven;
            }

            return report;
        }

        public decimal GetFuelCostByFleet(int fleetId, DateTime startDate, DateTime endDate)
        {
            var vehicles = _unitOfWork.Repository<Vehicle>()
                .Find(v => v.FleetId == fleetId)
                .Select(v => v.VehicleId)
                .ToList();

            return _unitOfWork.Repository<FuelTransaction>()
                .Find(ft => vehicles.Contains(ft.VehicleId) && 
                           ft.TransactionDate >= startDate && 
                           ft.TransactionDate <= endDate &&
                           ft.Status == "Approved")
                .Sum(ft => ft.Amount);
        }
    }

    public class FuelEfficiencyReport
    {
        public int VehicleId { get; set; }
        public string VIN { get; set; }
        public DateTime ReportStartDate { get; set; }
        public DateTime ReportEndDate { get; set; }
        public decimal TotalGallons { get; set; }
        public decimal TotalCost { get; set; }
        public int MilesDriven { get; set; }
        public double AverageMPG { get; set; }
        public decimal AveragePricePerGallon { get; set; }
        public decimal CostPerMile { get; set; }
        public int TransactionCount { get; set; }
    }
}
