using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using Newtonsoft.Json;

namespace GolfZonWebApp.Models
{
    public class PointObject : BaseEntity
    {
        public int Id { get; set; }
        public string Accumulate { get; set; }
        public int Group { get; set; }
        public int Type { get; set; }
        public int Point { get; set; } = 0;
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        [NotMapped]
        public int? State { get; set; }
    }
}
