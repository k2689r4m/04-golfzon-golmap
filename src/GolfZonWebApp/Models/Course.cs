using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfZonWebApp.Models
{
    public class Course : BaseEntity
    {
        public int Id { get; set; }
        [ForeignKey("GolfClubId")]
        public GolfClub GolfClub { get; set; }
        public int GolfClubId { get; set; }
        [StringLength(255)]
        public string CourseName { get; set; }
        public double? Length { get; set; }
        public ICollection<Hole> Holes { get; set; }
    }
}