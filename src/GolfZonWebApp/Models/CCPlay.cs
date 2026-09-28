using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace GolfZonWebApp.Models
{
    public class CCPlay : BaseEntity
    {
        public int Id { get; set; }

        [ForeignKey("GolfClubId")]
        public GolfClub GolfClub { get; set; }
        public int GolfClubId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
        public int UserId { get; set; }

        public DateTime? VisitDate { get; set; }
        public int? Score { get; set; }
    }
}
