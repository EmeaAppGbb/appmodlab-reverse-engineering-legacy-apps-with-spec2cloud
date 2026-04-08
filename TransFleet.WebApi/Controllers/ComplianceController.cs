using System;
using System.Web.Http;
using TransFleet.Core.Services;

namespace TransFleet.WebApi.Controllers
{
    [RoutePrefix("api/compliance")]
    public class ComplianceController : ApiController
    {
        private readonly IComplianceService _complianceService;

        public ComplianceController(IComplianceService complianceService)
        {
            _complianceService = complianceService ?? throw new ArgumentNullException(nameof(complianceService));
        }

        [HttpGet]
        [Route("driver/{driverId:int}/check")]
        public IHttpActionResult CheckCompliance(int driverId, [FromUri] DateTime? checkDate = null)
        {
            try
            {
                var date = checkDate ?? DateTime.UtcNow;
                var isCompliant = _complianceService.CheckDriverCompliance(driverId, date);
                return Ok(new { DriverId = driverId, Date = date, IsCompliant = isCompliant });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("driver/{driverId:int}/report")]
        public IHttpActionResult GetComplianceReport(int driverId, [FromUri] DateTime startDate, [FromUri] DateTime endDate)
        {
            try
            {
                var report = _complianceService.GetComplianceReport(driverId, startDate, endDate);
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
        [Route("driver/{driverId:int}/remaining-hours")]
        public IHttpActionResult GetRemainingHours(int driverId, [FromUri] DateTime? checkDate = null)
        {
            try
            {
                var date = checkDate ?? DateTime.UtcNow;
                var hours = _complianceService.GetRemainingDrivingHours(driverId, date);
                return Ok(new { DriverId = driverId, Date = date, RemainingHours = hours });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("driver/{driverId:int}/violations")]
        public IHttpActionResult GetViolations(int driverId, [FromUri] DateTime startDate, [FromUri] DateTime endDate)
        {
            try
            {
                var violations = _complianceService.GetViolations(driverId, startDate, endDate);
                return Ok(violations);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("driver/{driverId:int}/duty-status")]
        public IHttpActionResult RecordDutyStatus(int driverId, [FromBody] DutyStatusRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _complianceService.RecordDutyStatusChange(
                    driverId, 
                    request.DutyStatus, 
                    request.VehicleId, 
                    request.Latitude, 
                    request.Longitude, 
                    request.Remarks);

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
    }

    public class DutyStatusRequest
    {
        public string DutyStatus { get; set; }
        public int? VehicleId { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string Remarks { get; set; }
    }
}
