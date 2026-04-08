using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransFleet.Data.Entities
{
    [Table("Geofences")]
    public class Geofence
    {
        [Key]
        public int GeofenceId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public int FleetId { get; set; }

        [Required]
        [StringLength(4000)]
        public string Polygon { get; set; } // GeoJSON polygon

        [StringLength(50)]
        public string AlertType { get; set; } // Entry, Exit, Both

        public bool IsActive { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }

        [ForeignKey("FleetId")]
        public virtual Fleet Fleet { get; set; }
    }
}
