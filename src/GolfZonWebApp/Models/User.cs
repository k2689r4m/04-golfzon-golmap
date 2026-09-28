using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using Newtonsoft.Json;

namespace GolfZonWebApp.Models
{
    public class User : BaseEntity
    {
        public int Id { get; set; }

        [Required]
        public int UserNum { get; set; }
        [Required]
        [StringLength(255)]
        public string Username { get; set; }
        [Required]
        [StringLength(255)]
        public string Fullname { get; set; }
        [Required]
        [StringLength(255)]
        public string Nickname { get; set; }
        [StringLength(255)]
        public string Phone { get; set; }
        [StringLength(255)]
        public string Email { get; set; }
        [Required]
        public double Point { get; set; } = 0;
        public DateTime? Blinded { get; set; }

        public string ProfileImage { get; set; }
        public bool FlagNew { get; set; } = false;
        
        [StringLength(255)]
        public string RefreshToken { get; set; }

        public ICollection<PointHistory> PointHistorys { get; set; }
        public ICollection<UserLoginHistory> UserLoginHistorys { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<ReviewGood> ReviewGoods { get; set; }
        public ICollection<ReviewGrade> ReviewGrades { get; set; }
        public ICollection<ReviewBlackUser> ReviewBlackUsers { get; set; }
        public Setting Setting { get; set; }
        public DateTime? Regdt { get; set; }
    }
}
