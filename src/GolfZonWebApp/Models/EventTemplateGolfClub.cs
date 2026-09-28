using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfZonWebApp.Models
{
    public class EventTemplateGolfClub : BaseEntity
    {
        public int Id { get; set; }
        [ForeignKey("EventTemplateId")]
        public EventTemplate EventTemplate { get; set; }
        public int EventTemplateId { get; set;  }
        [ForeignKey("GolfClubId")]
        public GolfClub GolfClub { get; set; }
        public int GolfClubId { get; set;  }
    }
}