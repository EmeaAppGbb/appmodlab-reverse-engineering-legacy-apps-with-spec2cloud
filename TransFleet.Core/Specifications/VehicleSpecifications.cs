using System;
using System.Linq.Expressions;
using TransFleet.Data.Entities;

namespace TransFleet.Core.Specifications
{
    public class ActiveVehicleSpecification : Specification<Vehicle>
    {
        public override Expression<Func<Vehicle, bool>> ToExpression()
        {
            return vehicle => vehicle.Status == "Active";
        }
    }

    public class VehicleByFleetSpecification : Specification<Vehicle>
    {
        private readonly int _fleetId;

        public VehicleByFleetSpecification(int fleetId)
        {
            _fleetId = fleetId;
        }

        public override Expression<Func<Vehicle, bool>> ToExpression()
        {
            return vehicle => vehicle.FleetId == _fleetId;
        }
    }

    public class VehicleNeedsMaintenanceSpecification : Specification<Vehicle>
    {
        public override Expression<Func<Vehicle, bool>> ToExpression()
        {
            return vehicle => vehicle.Status == "Active" || vehicle.Status == "Maintenance";
        }
    }

    public class AvailableVehicleSpecification : Specification<Vehicle>
    {
        public override Expression<Func<Vehicle, bool>> ToExpression()
        {
            return vehicle => vehicle.Status == "Active" && !vehicle.CurrentDriverId.HasValue;
        }
    }

    public class VehicleByYearRangeSpecification : Specification<Vehicle>
    {
        private readonly int _minYear;
        private readonly int _maxYear;

        public VehicleByYearRangeSpecification(int minYear, int maxYear)
        {
            _minYear = minYear;
            _maxYear = maxYear;
        }

        public override Expression<Func<Vehicle, bool>> ToExpression()
        {
            return vehicle => vehicle.Year >= _minYear && vehicle.Year <= _maxYear;
        }
    }

    public class VehicleByFuelTypeSpecification : Specification<Vehicle>
    {
        private readonly string _fuelType;

        public VehicleByFuelTypeSpecification(string fuelType)
        {
            _fuelType = fuelType;
        }

        public override Expression<Func<Vehicle, bool>> ToExpression()
        {
            return vehicle => vehicle.FuelType == _fuelType;
        }
    }
}
