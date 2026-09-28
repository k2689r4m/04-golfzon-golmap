using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using Newtonsoft.Json;

namespace GolfZonWebApp.Models
{
    public class GolfClubSearchHistory : BaseEntity
    {
        public int Id { get; set; }

        [ForeignKey("GolfClubId")]
        public GolfClub GolfClub { get; set; }
        public int GolfClubId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
        public int UserId { get; set; }

        public string KeyWord { get; set; }
        public DateTime Dt { get; set; }
    }
}
