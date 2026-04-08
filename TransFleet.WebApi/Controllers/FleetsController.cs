using System;
using System.Web.Http;
using TransFleet.Data;
using TransFleet.Data.Entities;

namespace TransFleet.WebApi.Controllers
{
    [RoutePrefix("api/fleets")]
    public class FleetsController : ApiController
    {
        private readonly IUnitOfWork _unitOfWork;

        public FleetsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAllFleets()
        {
            try
            {
                var fleets = _unitOfWork.Repository<Fleet>().GetAll();
                return Ok(fleets);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult GetFleet(int id)
        {
            try
            {
                var fleet = _unitOfWork.Repository<Fleet>().GetById(id);
                if (fleet == null)
                    return NotFound();

                return Ok(fleet);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult CreateFleet([FromBody] Fleet fleet)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                fleet.CreatedDate = DateTime.UtcNow;
                fleet.Status = "Active";

                _unitOfWork.Repository<Fleet>().Add(fleet);
                _unitOfWork.SaveChanges();

                return Created($"api/fleets/{fleet.FleetId}", fleet);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult UpdateFleet(int id, [FromBody] Fleet fleet)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (id != fleet.FleetId)
                    return BadRequest("ID mismatch");

                fleet.ModifiedDate = DateTime.UtcNow;
                _unitOfWork.Repository<Fleet>().Update(fleet);
                _unitOfWork.SaveChanges();

                return Ok(fleet);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
