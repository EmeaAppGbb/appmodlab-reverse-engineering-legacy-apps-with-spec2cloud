using System;

namespace TransFleet.Core.Domain.ValueObjects
{
    public class Distance
    {
        public decimal Value { get; private set; }
        public DistanceUnit Unit { get; private set; }

        public Distance(decimal value, DistanceUnit unit = DistanceUnit.Miles)
        {
            if (value < 0)
                throw new ArgumentException("Distance cannot be negative", nameof(value));
            
            Value = value;
            Unit = unit;
        }

        public Distance ConvertTo(DistanceUnit targetUnit)
        {
            if (Unit == targetUnit)
                return this;

            decimal convertedValue = Unit switch
            {
                DistanceUnit.Miles when targetUnit == DistanceUnit.Kilometers => Value * 1.60934m,
                DistanceUnit.Kilometers when targetUnit == DistanceUnit.Miles => Value / 1.60934m,
                _ => Value
            };

            return new Distance(convertedValue, targetUnit);
        }

        public override string ToString()
        {
            return $"{Value:F2} {Unit}";
        }
    }

    public enum DistanceUnit
    {
        Miles,
        Kilometers
    }
}
