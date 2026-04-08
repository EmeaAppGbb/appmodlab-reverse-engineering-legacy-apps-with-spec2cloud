using System;
using System.Linq.Expressions;
using TransFleet.Data.Entities;

namespace TransFleet.Core.Specifications
{
    public class ActiveDriverSpecification : Specification<Driver>
    {
        public override Expression<Func<Driver, bool>> ToExpression()
        {
            return driver => driver.Status == "Active";
        }
    }

    public class DriverWithExpiredLicenseSpecification : Specification<Driver>
    {
        public override Expression<Func<Driver, bool>> ToExpression()
        {
            var now = DateTime.UtcNow;
            return driver => driver.LicenseExpiry < now;
        }
    }

    public class DriverWithExpiredMedicalCertSpecification : Specification<Driver>
    {
        public override Expression<Func<Driver, bool>> ToExpression()
        {
            var now = DateTime.UtcNow;
            return driver => driver.MedicalCertExpiry.HasValue && 
                           driver.MedicalCertExpiry.Value < now &&
                           !string.IsNullOrEmpty(driver.CDLClass);
        }
    }

    public class DriverByCDLClassSpecification : Specification<Driver>
    {
        private readonly string _cdlClass;

        public DriverByCDLClassSpecification(string cdlClass)
        {
            _cdlClass = cdlClass;
        }

        public override Expression<Func<Driver, bool>> ToExpression()
        {
            return driver => driver.CDLClass == _cdlClass;
        }
    }
}
