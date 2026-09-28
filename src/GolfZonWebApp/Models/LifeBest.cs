using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace GolfZonWebApp.Models
{
    public class LifeBest : BaseEntity
    {
        public int Id { get; set; }

        [ForeignKey("GolfClubId")]
        public GolfClub GolfClub { get; set; }
        public int GolfClubId { get; set; }

        public int CiCode { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
        public int UserId { get; set; }

        public int VisitCnt { get; set; }
        public int TotalScore { get; set; }
        public int BestScore { get; set; }
        public DateTime MonthDate { get; set; }
        public DateTime? BestVisitDate { get; set; }
    }
}
