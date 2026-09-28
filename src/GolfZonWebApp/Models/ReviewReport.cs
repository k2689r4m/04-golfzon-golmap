using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using Newtonsoft.Json;
using NetTopologySuite.Geometries;

namespace GolfZonWebApp.Models
{
    public class ReviewReport : BaseEntity
    {
        public int Id { get; set; }
        [ForeignKey("ReviewId")]
        public Review Review { get; set; }
        public int ReviewId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
        public int? UserId { get; set; }
        public string Content { get; set; }
    }
}
