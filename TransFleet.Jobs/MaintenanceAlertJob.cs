using System;
using System.Threading.Tasks;
using TransFleet.Core.Services;
using TransFleet.Data;

namespace TransFleet.Jobs
{
    public class MaintenanceAlertJob
    {
        private readonly IMaintenanceService _maintenanceService;
        private readonly IUnitOfWork _unitOfWork;

        public MaintenanceAlertJob(IMaintenanceService maintenanceService, IUnitOfWork unitOfWork)
        {
            _maintenanceService = maintenanceService ?? throw new ArgumentNullException(nameof(maintenanceService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task Execute()
        {
            try
            {
                Console.WriteLine($"[{DateTime.UtcNow}] MaintenanceAlertJob: Starting execution");

                // Get all fleets - in real implementation, this would be more targeted
                var fleets = _unitOfWork.Repository<Data.Entities.Fleet>().GetAll();

                foreach (var fleet in fleets)
                {
                    var overdueSchedules = _maintenanceService.GetOverdueSchedules(fleet.FleetId);
                    
                    foreach (var schedule in overdueSchedules)
                    {
                        // In real implementation, send alerts via email/SMS/notification system
                        Console.WriteLine($"ALERT: Vehicle {schedule.VehicleId} has overdue {schedule.ServiceType}");
                    }
                }

                Console.WriteLine($"[{DateTime.UtcNow}] MaintenanceAlertJob: Completed execution");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.UtcNow}] MaintenanceAlertJob: Error - {ex.Message}");
                throw;
            }
        }
    }
}
