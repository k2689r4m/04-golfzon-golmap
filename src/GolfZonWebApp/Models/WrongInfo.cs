using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using Newtonsoft.Json;
using NetTopologySuite.Geometries;

namespace GolfZonWebApp.Models
{
    public class WrongInfo : BaseEntity
    {
        public int Id { get; set; }
        [ForeignKey("GolfClubId")]
        public GolfClub GolfClub { get; set; }
        public int? GolfClubId { get; set; }
        [ForeignKey("ConvenienceId")]
        public Convenience Convenience { get; set; }
        public int? ConvenienceId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; }
        public int State { get; set; } = 0;         //0 미처리 1 처리

        public ICollection<WrongInfoImage> WrongInfoImages { get; set; }
    }
}
