using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransFleet.Data.Entities
{
    [Table("MaintenanceSchedules")]
    public class MaintenanceSchedule
    {
        [Key]
        public int ScheduleId { get; set; }

        public int VehicleId { get; set; }

        [Required]
        [StringLength(100)]
        public string ServiceType { get; set; } // OilChange, TireRotation, BrakeInspection, etc.

        public int? IntervalMiles { get; set; }

        public int? IntervalDays { get; set; }

        public DateTime? LastServiceDate { get; set; }

        public int? LastServiceMileage { get; set; }

        public DateTime? NextServiceDate { get; set; }

        public int? NextServiceMileage { get; set; }

        [StringLength(20)]
        public string Status { get; set; } // Active, Completed, Overdue, Cancelled

        public DateTime CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }

        [ForeignKey("VehicleId")]
        public virtual Vehicle Vehicle { get; set; }
    }
}
