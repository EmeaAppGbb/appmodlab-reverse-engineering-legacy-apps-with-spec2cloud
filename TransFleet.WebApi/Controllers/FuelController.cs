using System;
using System.Web.Http;
using TransFleet.Core.Services;
using TransFleet.Data.Entities;

namespace TransFleet.WebApi.Controllers
{
    [RoutePrefix("api/fuel")]
    public class FuelController : ApiController
    {
        private readonly IFuelService _fuelService;

        public FuelController(IFuelService fuelService)
        {
            _fuelService = fuelService ?? throw new ArgumentNullException(nameof(fuelService));
        }

        [HttpPost]
        [Route("transactions")]
        public IHttpActionResult ProcessTransaction([FromBody] FuelTransaction transaction)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _fuelService.ProcessFuelTransaction(transaction);
                return Created($"api/fuel/transactions/{transaction.TransactionId}", transaction);
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
        [Route("transactions/vehicle/{vehicleId:int}")]
        public IHttpActionResult GetTransactionsByVehicle(int vehicleId, [FromUri] DateTime startDate, [FromUri] DateTime endDate)
        {
            try
            {
                var transactions = _fuelService.GetTransactionsByVehicle(vehicleId, startDate, endDate);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("transactions/suspicious/fleet/{fleetId:int}")]
        public IHttpActionResult GetSuspiciousTransactions(int fleetId, [FromUri] DateTime startDate, [FromUri] DateTime endDate)
        {
            try
            {
                var transactions = _fuelService.GetSuspiciousTransactions(fleetId, startDate, endDate);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("efficiency/vehicle/{vehicleId:int}")]
        public IHttpActionResult GetEfficiencyReport(int vehicleId, [FromUri] DateTime startDate, [FromUri] DateTime endDate)
        {
            try
            {
                var report = _fuelService.GetFuelEfficiencyReport(vehicleId, startDate, endDate);
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
        [Route("cost/fleet/{fleetId:int}")]
        public IHttpActionResult GetFleetCost(int fleetId, [FromUri] DateTime startDate, [FromUri] DateTime endDate)
        {
            try
            {
                var cost = _fuelService.GetFuelCostByFleet(fleetId, startDate, endDate);
                return Ok(new { FleetId = fleetId, TotalCost = cost });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
