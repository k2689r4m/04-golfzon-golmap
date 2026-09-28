using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using Newtonsoft.Json;

namespace GolfZonWebApp.Models
{
    public class PointHistory : BaseEntity
    {
        public int Id { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
        public int UserId { get; set; }
        public string Accumulate { get; set; }
        public int Group { get; set; }
        public int Type { get; set; }
        public int Point { get; set; } = 0;
    }
}
