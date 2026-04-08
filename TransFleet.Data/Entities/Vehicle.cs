using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransFleet.Data.Entities
{
    [Table("Vehicles")]
    public class Vehicle
    {
        [Key]
        public int VehicleId { get; set; }

        [Required]
        [StringLength(17)]
        public string VIN { get; set; }

        [Required]
        [StringLength(50)]
        public string Make { get; set; }

        [Required]
        [StringLength(50)]
        public string Model { get; set; }

        public int Year { get; set; }

        [StringLength(20)]
        public string LicensePlate { get; set; }

        public int FleetId { get; set; }

        [StringLength(20)]
        public string Status { get; set; } // Active, Maintenance, Retired, Decommissioned

        public int OdometerReading { get; set; }

        [StringLength(20)]
        public string FuelType { get; set; } // Diesel, Gasoline, Electric, Hybrid

        public DateTime? AcquisitionDate { get; set; }

        public decimal? PurchasePrice { get; set; }

        public int? CurrentDriverId { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }

        [StringLength(50)]
        public string CreatedBy { get; set; }

        [StringLength(50)]
        public string ModifiedBy { get; set; }

        [ForeignKey("FleetId")]
        public virtual Fleet Fleet { get; set; }

        [ForeignKey("CurrentDriverId")]
        public virtual Driver CurrentDriver { get; set; }
    }
}
