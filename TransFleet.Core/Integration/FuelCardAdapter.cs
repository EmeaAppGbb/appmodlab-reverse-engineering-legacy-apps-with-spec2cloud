using System;

namespace TransFleet.Core.Integration
{
    public interface IFuelCardAdapter
    {
        FuelCardAuthorizationResponse AuthorizeTransaction(FuelCardAuthorizationRequest request);
        FuelCardSettlementResponse SettleTransaction(string authorizationCode, decimal actualAmount);
        FuelCardBalanceResponse GetCardBalance(string cardNumber);
    }

    public class FuelCardAdapter : IFuelCardAdapter
    {
        // WCF client for external fuel card processor would be initialized here
        private readonly string _serviceEndpoint;

        public FuelCardAdapter(string serviceEndpoint)
        {
            _serviceEndpoint = serviceEndpoint;
        }

        public FuelCardAuthorizationResponse AuthorizeTransaction(FuelCardAuthorizationRequest request)
        {
            // In real implementation, this would call the external WCF service
            // For demo purposes, we simulate the response
            
            return new FuelCardAuthorizationResponse
            {
                IsApproved = true,
                AuthorizationCode = Guid.NewGuid().ToString(),
                AuthorizedAmount = request.RequestedAmount,
                TransactionId = Guid.NewGuid().ToString(),
                ResponseTime = DateTime.UtcNow
            };
        }

        public FuelCardSettlementResponse SettleTransaction(string authorizationCode, decimal actualAmount)
        {
            return new FuelCardSettlementResponse
            {
                IsSettled = true,
                SettlementId = Guid.NewGuid().ToString(),
                SettlementTime = DateTime.UtcNow
            };
        }

        public FuelCardBalanceResponse GetCardBalance(string cardNumber)
        {
            return new FuelCardBalanceResponse
            {
                CardNumber = cardNumber,
                AvailableBalance = 5000.00m,
                CreditLimit = 10000.00m,
                LastUpdated = DateTime.UtcNow
            };
        }
    }

    public class FuelCardAuthorizationRequest
    {
        public string CardNumber { get; set; }
        public decimal RequestedAmount { get; set; }
        public string MerchantId { get; set; }
        public string Location { get; set; }
        public DateTime RequestTime { get; set; }
    }

    public class FuelCardAuthorizationResponse
    {
        public bool IsApproved { get; set; }
        public string AuthorizationCode { get; set; }
        public decimal AuthorizedAmount { get; set; }
        public string TransactionId { get; set; }
        public DateTime ResponseTime { get; set; }
        public string DeclineReason { get; set; }
    }

    public class FuelCardSettlementResponse
    {
        public bool IsSettled { get; set; }
        public string SettlementId { get; set; }
        public DateTime SettlementTime { get; set; }
    }

    public class FuelCardBalanceResponse
    {
        public string CardNumber { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal CreditLimit { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
