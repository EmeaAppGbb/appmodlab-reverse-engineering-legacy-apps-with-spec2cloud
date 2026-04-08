using System;
using System.Linq;
using System.Web.Http;
using TransFleet.Data;
using TransFleet.Data.Entities;

namespace TransFleet.WebApi.Controllers
{
    [RoutePrefix("api/gps")]
    public class GPSController : ApiController
    {
        private readonly IUnitOfWork _unitOfWork;

        public GPSController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        [HttpPost]
        [Route("positions")]
        public IHttpActionResult RecordPosition([FromBody] GPSPosition position)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                position.CreatedDate = DateTime.UtcNow;
                position.Timestamp = DateTime.UtcNow;

                _unitOfWork.Repository<GPSPosition>().Add(position);
                _unitOfWork.SaveChanges();

                return Ok(position);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("positions/vehicle/{vehicleId:int}")]
        public IHttpActionResult GetPositions(int vehicleId, [FromUri] DateTime? startDate = null, [FromUri] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddHours(-24);
                var end = endDate ?? DateTime.UtcNow;

                var positions = _unitOfWork.Repository<GPSPosition>()
                    .Find(p => p.VehicleId == vehicleId && 
                              p.Timestamp >= start && 
                              p.Timestamp <= end)
                    .OrderBy(p => p.Timestamp);

                return Ok(positions);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("positions/latest/vehicle/{vehicleId:int}")]
        public IHttpActionResult GetLatestPosition(int vehicleId)
        {
            try
            {
                var position = _unitOfWork.Repository<GPSPosition>()
                    .Find(p => p.VehicleId == vehicleId)
                    .OrderByDescending(p => p.Timestamp)
                    .FirstOrDefault();

                if (position == null)
                    return NotFound();

                return Ok(position);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
