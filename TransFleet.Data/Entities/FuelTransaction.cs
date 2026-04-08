using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransFleet.Data.Entities
{
    [Table("FuelTransactions")]
    public class FuelTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        public int VehicleId { get; set; }

        public int? DriverId { get; set; }

        public int FuelCardId { get; set; }

        public decimal Gallons { get; set; }

        public decimal Amount { get; set; }

        public decimal PricePerGallon { get; set; }

        [StringLength(200)]
        public string Location { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [StringLength(50)]
        public string State { get; set; }

        public DateTime TransactionDate { get; set; }

        public int? OdometerReading { get; set; }

        [StringLength(50)]
        public string AuthorizationCode { get; set; }

        [StringLength(20)]
        public string Status { get; set; } // Approved, Pending, Declined, Flagged

        public DateTime CreatedDate { get; set; }

        [ForeignKey("VehicleId")]
        public virtual Vehicle Vehicle { get; set; }

        [ForeignKey("DriverId")]
        public virtual Driver Driver { get; set; }
    }
}
