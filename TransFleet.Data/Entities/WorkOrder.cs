using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransFleet.Data.Entities
{
    [Table("WorkOrders")]
    public class WorkOrder
    {
        [Key]
        public int WorkOrderId { get; set; }

        public int VehicleId { get; set; }

        [Required]
        [StringLength(100)]
        public string Type { get; set; } // Repair, Inspection, Recall, Modification

        [StringLength(20)]
        public string Priority { get; set; } // Low, Medium, High, Critical

        [StringLength(20)]
        public string Status { get; set; } // Open, InProgress, Completed, Cancelled

        [StringLength(1000)]
        public string Description { get; set; }

        public int? AssignedToVendorId { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ScheduledDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        public decimal? EstimatedCost { get; set; }

        public decimal? ActualCost { get; set; }

        public int? OdometerReading { get; set; }

        [StringLength(2000)]
        public string Notes { get; set; }

        [ForeignKey("VehicleId")]
        public virtual Vehicle Vehicle { get; set; }
    }
}
