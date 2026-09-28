using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace GolfZonWebApp.Models
{
    public class RefGolfList : BaseEntity
    {
        public int Id { get; set; }

        [ForeignKey("GolfClubId")]
        public GolfClub GolfClub { get; set; }
        public int GolfClubId { get; set; }

        [ForeignKey("ConvenienceId")]
        public Convenience Convenience { get; set; }
        public int ConvenienceId { get; set; }
    }
}
