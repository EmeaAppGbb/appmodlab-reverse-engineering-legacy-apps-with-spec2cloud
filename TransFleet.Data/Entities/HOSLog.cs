using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransFleet.Data.Entities
{
    [Table("HOSLogs")]
    public class HOSLog
    {
        [Key]
        public long LogId { get; set; }

        public int DriverId { get; set; }

        [Required]
        [StringLength(20)]
        public string DutyStatus { get; set; } // OffDuty, SleeperBerth, Driving, OnDuty

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        [StringLength(200)]
        public string Location { get; set; }

        public int? VehicleId { get; set; }

        public int? OdometerReading { get; set; }

        [StringLength(500)]
        public string Remarks { get; set; }

        [StringLength(20)]
        public string RecordStatus { get; set; } // Active, Changed, Inactive

        [StringLength(50)]
        public string RecordOrigin { get; set; } // Auto, Manual, Edited

        public DateTime CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }

        [ForeignKey("DriverId")]
        public virtual Driver Driver { get; set; }

        [ForeignKey("VehicleId")]
        public virtual Vehicle Vehicle { get; set; }
    }
}
