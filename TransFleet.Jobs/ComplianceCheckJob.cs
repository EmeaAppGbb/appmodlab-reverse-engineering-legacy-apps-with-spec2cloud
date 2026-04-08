using System;
using System.Linq;
using System.Threading.Tasks;
using TransFleet.Core.Services;
using TransFleet.Data;

namespace TransFleet.Jobs
{
    public class ComplianceCheckJob
    {
        private readonly IComplianceService _complianceService;
        private readonly IUnitOfWork _unitOfWork;

        public ComplianceCheckJob(IComplianceService complianceService, IUnitOfWork unitOfWork)
        {
            _complianceService = complianceService ?? throw new ArgumentNullException(nameof(complianceService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task Execute()
        {
            try
            {
                Console.WriteLine($"[{DateTime.UtcNow}] ComplianceCheckJob: Starting execution");

                var activeDrivers = _unitOfWork.Repository<Data.Entities.Driver>()
                    .Find(d => d.Status == "Active")
                    .ToList();

                var today = DateTime.UtcNow;

                foreach (var driver in activeDrivers)
                {
                    var isCompliant = _complianceService.CheckDriverCompliance(driver.DriverId, today);
                    
                    if (!isCompliant)
                    {
                        var violations = _complianceService.GetViolations(
                            driver.DriverId, 
                            today.AddDays(-7), 
                            today);

                        foreach (var violation in violations)
                        {
                            Console.WriteLine($"COMPLIANCE VIOLATION: Driver {driver.FirstName} {driver.LastName} - {violation.Description}");
                        }
                    }
                }

                Console.WriteLine($"[{DateTime.UtcNow}] ComplianceCheckJob: Completed execution");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.UtcNow}] ComplianceCheckJob: Error - {ex.Message}");
                throw;
            }
        }
    }
}
