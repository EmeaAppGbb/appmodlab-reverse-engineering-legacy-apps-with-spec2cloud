using System;
using System.Collections.Generic;
using System.Linq;
using TransFleet.Data;
using TransFleet.Data.Entities;
using TransFleet.Core.Domain.Rules;

namespace TransFleet.Core.Services
{
    public interface IMaintenanceService
    {
        MaintenanceSchedule GetScheduleById(int scheduleId);
        IEnumerable<MaintenanceSchedule> GetSchedulesByVehicle(int vehicleId);
        IEnumerable<MaintenanceSchedule> GetOverdueSchedules(int fleetId);
        void CreateSchedule(MaintenanceSchedule schedule);
        void UpdateSchedule(MaintenanceSchedule schedule);
        void RecordServiceCompletion(int scheduleId, DateTime serviceDate, int serviceMileage);
        WorkOrder CreateWorkOrder(WorkOrder workOrder);
        void UpdateWorkOrder(WorkOrder workOrder);
        void CompleteWorkOrder(int workOrderId, decimal actualCost, string notes);
        IEnumerable<WorkOrder> GetOpenWorkOrders(int fleetId);
    }

    public class MaintenanceService : IMaintenanceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MaintenanceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public MaintenanceSchedule GetScheduleById(int scheduleId)
        {
            return _unitOfWork.Repository<MaintenanceSchedule>().GetById(scheduleId);
        }

        public IEnumerable<MaintenanceSchedule> GetSchedulesByVehicle(int vehicleId)
        {
            return _unitOfWork.Repository<MaintenanceSchedule>()
                .Find(ms => ms.VehicleId == vehicleId);
        }

        public IEnumerable<MaintenanceSchedule> GetOverdueSchedules(int fleetId)
        {
            var vehicles = _unitOfWork.Repository<Vehicle>()
                .Find(v => v.FleetId == fleetId)
                .ToList();

            var overdueSchedules = new List<MaintenanceSchedule>();

            foreach (var vehicle in vehicles)
            {
                var schedules = _unitOfWork.Repository<MaintenanceSchedule>()
                    .Find(ms => ms.VehicleId == vehicle.VehicleId && ms.Status == "Active")
                    .ToList();

                foreach (var schedule in schedules)
                {
                    if (MaintenanceRules.IsMaintenanceOverdue(schedule, vehicle))
                    {
                        overdueSchedules.Add(schedule);
                    }
                }
            }

            return overdueSchedules;
        }

        public void CreateSchedule(MaintenanceSchedule schedule)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));

            var vehicle = _unitOfWork.Repository<Vehicle>().GetById(schedule.VehicleId);
            if (vehicle == null)
                throw new InvalidOperationException($"Vehicle with ID {schedule.VehicleId} not found.");

            schedule.Status = "Active";
            schedule.CreatedDate = DateTime.UtcNow;

            _unitOfWork.Repository<MaintenanceSchedule>().Add(schedule);
            _unitOfWork.SaveChanges();
        }

        public void UpdateSchedule(MaintenanceSchedule schedule)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));

            var existing = _unitOfWork.Repository<MaintenanceSchedule>().GetById(schedule.ScheduleId);
            if (existing == null)
                throw new InvalidOperationException($"Schedule with ID {schedule.ScheduleId} not found.");

            schedule.ModifiedDate = DateTime.UtcNow;
            _unitOfWork.Repository<MaintenanceSchedule>().Update(schedule);
            _unitOfWork.SaveChanges();
        }

        public void RecordServiceCompletion(int scheduleId, DateTime serviceDate, int serviceMileage)
        {
            var schedule = _unitOfWork.Repository<MaintenanceSchedule>().GetById(scheduleId);
            if (schedule == null)
                throw new InvalidOperationException($"Schedule with ID {scheduleId} not found.");

            schedule.LastServiceDate = serviceDate;
            schedule.LastServiceMileage = serviceMileage;

            if (schedule.IntervalDays.HasValue)
            {
                schedule.NextServiceDate = MaintenanceRules.CalculateNextServiceDate(
                    serviceDate, schedule.IntervalDays.Value);
            }

            if (schedule.IntervalMiles.HasValue)
            {
                schedule.NextServiceMileage = MaintenanceRules.CalculateNextServiceMileage(
                    serviceMileage, schedule.IntervalMiles.Value);
            }

            schedule.Status = "Active";
            schedule.ModifiedDate = DateTime.UtcNow;

            _unitOfWork.Repository<MaintenanceSchedule>().Update(schedule);
            _unitOfWork.SaveChanges();
        }

        public WorkOrder CreateWorkOrder(WorkOrder workOrder)
        {
            if (workOrder == null)
                throw new ArgumentNullException(nameof(workOrder));

            var vehicle = _unitOfWork.Repository<Vehicle>().GetById(workOrder.VehicleId);
            if (vehicle == null)
                throw new InvalidOperationException($"Vehicle with ID {workOrder.VehicleId} not found.");

            workOrder.Status = "Open";
            workOrder.CreatedDate = DateTime.UtcNow;

            _unitOfWork.Repository<WorkOrder>().Add(workOrder);
            _unitOfWork.SaveChanges();

            return workOrder;
        }

        public void UpdateWorkOrder(WorkOrder workOrder)
        {
            if (workOrder == null)
                throw new ArgumentNullException(nameof(workOrder));

            var existing = _unitOfWork.Repository<WorkOrder>().GetById(workOrder.WorkOrderId);
            if (existing == null)
                throw new InvalidOperationException($"WorkOrder with ID {workOrder.WorkOrderId} not found.");

            _unitOfWork.Repository<WorkOrder>().Update(workOrder);
            _unitOfWork.SaveChanges();
        }

        public void CompleteWorkOrder(int workOrderId, decimal actualCost, string notes)
        {
            var workOrder = _unitOfWork.Repository<WorkOrder>().GetById(workOrderId);
            if (workOrder == null)
                throw new InvalidOperationException($"WorkOrder with ID {workOrderId} not found.");

            workOrder.Status = "Completed";
            workOrder.CompletedDate = DateTime.UtcNow;
            workOrder.ActualCost = actualCost;
            workOrder.Notes = notes;

            _unitOfWork.Repository<WorkOrder>().Update(workOrder);
            _unitOfWork.SaveChanges();
        }

        public IEnumerable<WorkOrder> GetOpenWorkOrders(int fleetId)
        {
            var vehicles = _unitOfWork.Repository<Vehicle>()
                .Find(v => v.FleetId == fleetId)
                .Select(v => v.VehicleId)
                .ToList();

            return _unitOfWork.Repository<WorkOrder>()
                .Find(wo => vehicles.Contains(wo.VehicleId) && 
                           (wo.Status == "Open" || wo.Status == "InProgress"))
                .OrderByDescending(wo => wo.Priority)
                .ThenBy(wo => wo.CreatedDate);
        }
    }
}
