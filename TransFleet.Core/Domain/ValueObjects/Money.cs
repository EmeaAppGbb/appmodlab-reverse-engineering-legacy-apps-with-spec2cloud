using System;

namespace TransFleet.Core.Domain.ValueObjects
{
    public class Money
    {
        public decimal Amount { get; private set; }
        public string Currency { get; private set; }

        public Money(decimal amount, string currency = "USD")
        {
            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative", nameof(amount));
            
            Amount = amount;
            Currency = currency ?? "USD";
        }

        public Money Add(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException("Cannot add money with different currencies");
            
            return new Money(Amount + other.Amount, Currency);
        }

        public Money Subtract(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException("Cannot subtract money with different currencies");
            
            return new Money(Amount - other.Amount, Currency);
        }

        public override bool Equals(object obj)
        {
            if (obj is Money other)
            {
                return Amount == other.Amount && Currency == other.Currency;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Amount, Currency);
        }

        public override string ToString()
        {
            return $"{Amount:C} {Currency}";
        }
    }
}
