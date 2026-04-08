using System;
using System.Collections.Generic;
using System.Linq;
using TransFleet.Data;
using TransFleet.Data.Entities;

namespace TransFleet.Core.Services
{
    public interface IDriverService
    {
        Driver GetDriverById(int driverId);
        IEnumerable<Driver> GetActiveDrivers();
        IEnumerable<Driver> GetDriversWithExpiredDocuments();
        void CreateDriver(Driver driver);
        void UpdateDriver(Driver driver);
        void TerminateDriver(int driverId, DateTime terminationDate, string reason);
    }

    public class DriverService : IDriverService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DriverService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public Driver GetDriverById(int driverId)
        {
            return _unitOfWork.Repository<Driver>().GetById(driverId);
        }

        public IEnumerable<Driver> GetActiveDrivers()
        {
            return _unitOfWork.Repository<Driver>().Find(d => d.Status == "Active");
        }

        public IEnumerable<Driver> GetDriversWithExpiredDocuments()
        {
            var now = DateTime.UtcNow;
            return _unitOfWork.Repository<Driver>()
                .Find(d => d.Status == "Active" && 
                          (d.LicenseExpiry < now || 
                           (d.MedicalCertExpiry.HasValue && d.MedicalCertExpiry.Value < now)));
        }

        public void CreateDriver(Driver driver)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            // Business rule: License number must be unique
            var existing = _unitOfWork.Repository<Driver>()
                .Find(d => d.LicenseNumber == driver.LicenseNumber)
                .FirstOrDefault();

            if (existing != null)
                throw new InvalidOperationException($"A driver with license number {driver.LicenseNumber} already exists.");

            // Business rule: License must not be expired
            if (driver.LicenseExpiry < DateTime.UtcNow)
                throw new InvalidOperationException("Cannot hire driver with expired license.");

            driver.Status = "Active";
            driver.CreatedDate = DateTime.UtcNow;

            _unitOfWork.Repository<Driver>().Add(driver);
            _unitOfWork.SaveChanges();
        }

        public void UpdateDriver(Driver driver)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            var existing = _unitOfWork.Repository<Driver>().GetById(driver.DriverId);
            if (existing == null)
                throw new InvalidOperationException($"Driver with ID {driver.DriverId} not found.");

            driver.ModifiedDate = DateTime.UtcNow;
            _unitOfWork.Repository<Driver>().Update(driver);
            _unitOfWork.SaveChanges();
        }

        public void TerminateDriver(int driverId, DateTime terminationDate, string reason)
        {
            var driver = _unitOfWork.Repository<Driver>().GetById(driverId);
            if (driver == null)
                throw new InvalidOperationException($"Driver with ID {driverId} not found.");

            // Business rule: Must unassign from any vehicles
            var assignedVehicle = _unitOfWork.Repository<Vehicle>()
                .Find(v => v.CurrentDriverId == driverId)
                .FirstOrDefault();

            if (assignedVehicle != null)
            {
                assignedVehicle.CurrentDriverId = null;
                assignedVehicle.ModifiedDate = DateTime.UtcNow;
                _unitOfWork.Repository<Vehicle>().Update(assignedVehicle);
            }

            driver.Status = "Terminated";
            driver.TerminationDate = terminationDate;
            driver.ModifiedDate = DateTime.UtcNow;

            _unitOfWork.Repository<Driver>().Update(driver);
            _unitOfWork.SaveChanges();
        }
    }
}
