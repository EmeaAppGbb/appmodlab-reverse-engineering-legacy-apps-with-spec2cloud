using System;
using System.Web.Http;
using TransFleet.Core.Services;
using TransFleet.Data.Entities;

namespace TransFleet.WebApi.Controllers
{
    [RoutePrefix("api/maintenance")]
    public class MaintenanceController : ApiController
    {
        private readonly IMaintenanceService _maintenanceService;

        public MaintenanceController(IMaintenanceService maintenanceService)
        {
            _maintenanceService = maintenanceService ?? throw new ArgumentNullException(nameof(maintenanceService));
        }

        [HttpGet]
        [Route("schedules/{id:int}")]
        public IHttpActionResult GetSchedule(int id)
        {
            try
            {
                var schedule = _maintenanceService.GetScheduleById(id);
                if (schedule == null)
                    return NotFound();

                return Ok(schedule);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("schedules/vehicle/{vehicleId:int}")]
        public IHttpActionResult GetSchedulesByVehicle(int vehicleId)
        {
            try
            {
                var schedules = _maintenanceService.GetSchedulesByVehicle(vehicleId);
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("schedules/overdue/fleet/{fleetId:int}")]
        public IHttpActionResult GetOverdueSchedules(int fleetId)
        {
            try
            {
                var schedules = _maintenanceService.GetOverdueSchedules(fleetId);
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("schedules")]
        public IHttpActionResult CreateSchedule([FromBody] MaintenanceSchedule schedule)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _maintenanceService.CreateSchedule(schedule);
                return Created($"api/maintenance/schedules/{schedule.ScheduleId}", schedule);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPut]
        [Route("schedules/{id:int}")]
        public IHttpActionResult UpdateSchedule(int id, [FromBody] MaintenanceSchedule schedule)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (id != schedule.ScheduleId)
                    return BadRequest("ID mismatch");

                _maintenanceService.UpdateSchedule(schedule);
                return Ok(schedule);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("schedules/{id:int}/complete")]
        public IHttpActionResult CompleteService(int id, [FromBody] ServiceCompletionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _maintenanceService.RecordServiceCompletion(id, request.ServiceDate, request.ServiceMileage);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("workorders")]
        public IHttpActionResult CreateWorkOrder([FromBody] WorkOrder workOrder)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var created = _maintenanceService.CreateWorkOrder(workOrder);
                return Created($"api/maintenance/workorders/{created.WorkOrderId}", created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPut]
        [Route("workorders/{id:int}")]
        public IHttpActionResult UpdateWorkOrder(int id, [FromBody] WorkOrder workOrder)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (id != workOrder.WorkOrderId)
                    return BadRequest("ID mismatch");

                _maintenanceService.UpdateWorkOrder(workOrder);
                return Ok(workOrder);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("workorders/{id:int}/complete")]
        public IHttpActionResult CompleteWorkOrder(int id, [FromBody] WorkOrderCompletionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _maintenanceService.CompleteWorkOrder(id, request.ActualCost, request.Notes);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("workorders/open/fleet/{fleetId:int}")]
        public IHttpActionResult GetOpenWorkOrders(int fleetId)
        {
            try
            {
                var workOrders = _maintenanceService.GetOpenWorkOrders(fleetId);
                return Ok(workOrders);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }

    public class ServiceCompletionRequest
    {
        public DateTime ServiceDate { get; set; }
        public int ServiceMileage { get; set; }
    }

    public class WorkOrderCompletionRequest
    {
        public decimal ActualCost { get; set; }
        public string Notes { get; set; }
    }
}
