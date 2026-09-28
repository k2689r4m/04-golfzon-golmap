using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfZonWebApp.Models
{
    public class FilterList : BaseEntity
    {
        public int Id { get; set; }
        [ForeignKey("FilterObjectId")]
        public FilterObject FilterObject { get; set; }
        public int FilterObjectId { get; set; }
        [ForeignKey("GolfClubId")]
        //public GolfClub GolfClub { get; set; }
        public int GolfClubId { get; set; }
        [NotMapped]
        [StringLength(255)]
        public string Name { get; set; }

        
    }
}