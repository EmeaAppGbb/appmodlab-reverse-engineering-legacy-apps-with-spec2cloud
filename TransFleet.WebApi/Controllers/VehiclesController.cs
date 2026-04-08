using System;
using System.Collections.Generic;
using System.Web.Http;
using TransFleet.Core.Services;
using TransFleet.Data.Entities;

namespace TransFleet.WebApi.Controllers
{
    [RoutePrefix("api/vehicles")]
    public class VehiclesController : ApiController
    {
        private readonly IVehicleService _vehicleService;

        public VehiclesController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService ?? throw new ArgumentNullException(nameof(vehicleService));
        }

        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult GetVehicle(int id)
        {
            try
            {
                var vehicle = _vehicleService.GetVehicleById(id);
                if (vehicle == null)
                    return NotFound();

                return Ok(vehicle);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("fleet/{fleetId:int}")]
        public IHttpActionResult GetVehiclesByFleet(int fleetId)
        {
            try
            {
                var vehicles = _vehicleService.GetVehiclesByFleet(fleetId);
                return Ok(vehicles);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("status/{status}")]
        public IHttpActionResult GetVehiclesByStatus(string status)
        {
            try
            {
                var vehicles = _vehicleService.GetVehiclesByStatus(status);
                return Ok(vehicles);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult CreateVehicle([FromBody] Vehicle vehicle)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _vehicleService.CreateVehicle(vehicle);
                return Created($"api/vehicles/{vehicle.VehicleId}", vehicle);
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
        [Route("{id:int}")]
        public IHttpActionResult UpdateVehicle(int id, [FromBody] Vehicle vehicle)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (id != vehicle.VehicleId)
                    return BadRequest("ID mismatch");

                _vehicleService.UpdateVehicle(vehicle);
                return Ok(vehicle);
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

        [HttpDelete]
        [Route("{id:int}")]
        public IHttpActionResult DeactivateVehicle(int id, [FromUri] string reason = "")
        {
            try
            {
                _vehicleService.DeactivateVehicle(id, reason);
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
        [Route("{vehicleId:int}/assign-driver/{driverId:int}")]
        public IHttpActionResult AssignDriver(int vehicleId, int driverId)
        {
            try
            {
                _vehicleService.AssignDriver(vehicleId, driverId);
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
        [Route("{vehicleId:int}/unassign-driver")]
        public IHttpActionResult UnassignDriver(int vehicleId)
        {
            try
            {
                _vehicleService.UnassignDriver(vehicleId);
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
        [Route("{id:int}/health")]
        public IHttpActionResult GetHealthReport(int id)
        {
            try
            {
                var report = _vehicleService.GetVehicleHealthReport(id);
                return Ok(report);
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
        [Route("{id:int}/utilization")]
        public IHttpActionResult GetUtilizationReport(int id, [FromUri] DateTime startDate, [FromUri] DateTime endDate)
        {
            try
            {
                var report = _vehicleService.GetUtilizationReport(id, startDate, endDate);
                return Ok(report);
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
        [Route("{id:int}/maintenance-cost")]
        public IHttpActionResult GetMaintenanceCost(int id, [FromUri] DateTime startDate, [FromUri] DateTime endDate)
        {
            try
            {
                var cost = _vehicleService.CalculateTotalMaintenanceCost(id, startDate, endDate);
                return Ok(new { TotalCost = cost });
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
    }
}
