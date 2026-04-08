using System;
using System.Web.Http;
using TransFleet.Core.Services;
using TransFleet.Data.Entities;

namespace TransFleet.WebApi.Controllers
{
    [RoutePrefix("api/geofences")]
    public class GeofencesController : ApiController
    {
        private readonly IGeofenceService _geofenceService;

        public GeofencesController(IGeofenceService geofenceService)
        {
            _geofenceService = geofenceService ?? throw new ArgumentNullException(nameof(geofenceService));
        }

        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult GetGeofence(int id)
        {
            try
            {
                var geofence = _geofenceService.GetGeofenceById(id);
                if (geofence == null)
                    return NotFound();

                return Ok(geofence);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("fleet/{fleetId:int}")]
        public IHttpActionResult GetGeofencesByFleet(int fleetId)
        {
            try
            {
                var geofences = _geofenceService.GetGeofencesByFleet(fleetId);
                return Ok(geofences);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult CreateGeofence([FromBody] Geofence geofence)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _geofenceService.CreateGeofence(geofence);
                return Created($"api/geofences/{geofence.GeofenceId}", geofence);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult UpdateGeofence(int id, [FromBody] Geofence geofence)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (id != geofence.GeofenceId)
                    return BadRequest("ID mismatch");

                _geofenceService.UpdateGeofence(geofence);
                return Ok(geofence);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpDelete]
        [Route("{id:int}")]
        public IHttpActionResult DeleteGeofence(int id)
        {
            try
            {
                _geofenceService.DeleteGeofence(id);
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
        [Route("check/vehicle/{vehicleId:int}/geofence/{geofenceId:int}")]
        public IHttpActionResult CheckVehicleInGeofence(int vehicleId, int geofenceId)
        {
            try
            {
                var isInside = _geofenceService.IsVehicleInGeofence(vehicleId, geofenceId);
                return Ok(new { VehicleId = vehicleId, GeofenceId = geofenceId, IsInside = isInside });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("alerts/fleet/{fleetId:int}")]
        public IHttpActionResult GetGeofenceAlerts(int fleetId)
        {
            try
            {
                var alerts = _geofenceService.CheckGeofenceViolations(fleetId);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
