using System;
using System.Web.Http;
using TransFleet.Data;
using TransFleet.Data.Entities;

namespace TransFleet.WebApi.Controllers
{
    [RoutePrefix("api/drivers")]
    public class DriversController : ApiController
    {
        private readonly IUnitOfWork _unitOfWork;

        public DriversController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult GetDriver(int id)
        {
            try
            {
                var driver = _unitOfWork.Repository<Driver>().GetById(id);
                if (driver == null)
                    return NotFound();

                return Ok(driver);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAllDrivers()
        {
            try
            {
                var drivers = _unitOfWork.Repository<Driver>().GetAll();
                return Ok(drivers);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult CreateDriver([FromBody] Driver driver)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                driver.CreatedDate = DateTime.UtcNow;
                driver.Status = "Active";

                _unitOfWork.Repository<Driver>().Add(driver);
                _unitOfWork.SaveChanges();

                return Created($"api/drivers/{driver.DriverId}", driver);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult UpdateDriver(int id, [FromBody] Driver driver)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (id != driver.DriverId)
                    return BadRequest("ID mismatch");

                driver.ModifiedDate = DateTime.UtcNow;
                _unitOfWork.Repository<Driver>().Update(driver);
                _unitOfWork.SaveChanges();

                return Ok(driver);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
