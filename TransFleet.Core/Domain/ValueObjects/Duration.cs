using System;

namespace TransFleet.Core.Domain.ValueObjects
{
    public class Duration
    {
        public TimeSpan Value { get; private set; }

        public Duration(TimeSpan value)
        {
            if (value < TimeSpan.Zero)
                throw new ArgumentException("Duration cannot be negative", nameof(value));
            
            Value = value;
        }

        public static Duration FromHours(double hours)
        {
            return new Duration(TimeSpan.FromHours(hours));
        }

        public static Duration FromMinutes(double minutes)
        {
            return new Duration(TimeSpan.FromMinutes(minutes));
        }

        public Duration Add(Duration other)
        {
            return new Duration(Value + other.Value);
        }

        public Duration Subtract(Duration other)
        {
            if (Value < other.Value)
                throw new InvalidOperationException("Cannot subtract a larger duration from a smaller one");
            
            return new Duration(Value - other.Value);
        }

        public double TotalHours => Value.TotalHours;
        public double TotalMinutes => Value.TotalMinutes;

        public override string ToString()
        {
            return $"{Value.TotalHours:F2} hours";
        }
    }
}
