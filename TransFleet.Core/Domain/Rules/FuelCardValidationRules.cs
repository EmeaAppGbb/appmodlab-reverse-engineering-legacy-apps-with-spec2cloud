using System;
using TransFleet.Data.Entities;

namespace TransFleet.Core.Domain.Rules
{
    public static class FuelCardValidationRules
    {
        public const decimal MaxFuelTransactionAmount = 500.00m;
        public const decimal MaxGallonsPerTransaction = 100.0m;
        public const int MaxTransactionsPerDay = 5;
        public const int SuspiciousTransactionIntervalMinutes = 30;

        public static bool IsTransactionValid(FuelTransaction transaction, Vehicle vehicle)
        {
            if (transaction == null || vehicle == null)
                return false;

            // Check transaction amount limits
            if (transaction.Amount > MaxFuelTransactionAmount)
                return false;

            if (transaction.Gallons > MaxGallonsPerTransaction)
                return false;

            // Check fuel type compatibility
            if (!IsFuelTypeCompatible(transaction, vehicle))
                return false;

            return true;
        }

        public static bool IsFuelTypeCompatible(FuelTransaction transaction, Vehicle vehicle)
        {
            // In real implementation, would check actual fuel type from transaction
            // This is simplified business logic
            return true;
        }

        public static bool IsSuspiciousTransaction(FuelTransaction current, FuelTransaction previous)
        {
            if (previous == null)
                return false;

            var timeDifference = (current.TransactionDate - previous.TransactionDate).TotalMinutes;
            
            // Suspicious if same vehicle fuels within 30 minutes
            if (timeDifference < SuspiciousTransactionIntervalMinutes)
                return true;

            // Suspicious if location is geographically impossible
            if (!string.IsNullOrEmpty(current.State) && 
                !string.IsNullOrEmpty(previous.State) && 
                current.State != previous.State && 
                timeDifference < 60)
            {
                return true;
            }

            return false;
        }

        public static decimal CalculateFuelEfficiency(decimal gallons, int milesDriven)
        {
            if (gallons <= 0 || milesDriven <= 0)
                return 0;

            return milesDriven / gallons; // Miles per gallon
        }
    }
}
