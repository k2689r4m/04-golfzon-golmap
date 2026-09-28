using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfZonWebApp.Models
{
    public class GolfClubTemplate : BaseEntity
    {
        public int Id { get; set; }
        [ForeignKey("TemplateId")]
        public Template Template { get; set; }
        public int TemplateId { get; set; }
        [ForeignKey("GolfClubId")]
        public GolfClub GolfClub { get; set; }
        public int GolfClubId { get; set; }
    }
}