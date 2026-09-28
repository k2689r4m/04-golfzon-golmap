using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfZonWebApp.Models
{
    public class Template : BaseEntity
    {
        public int Id { get; set; }
        [Range(1, 5)]
        public int TemplateType { get; set; }
        public string Type { get; set; }
        [Required]
        public string MenuName { get; set; }
        public bool IsActive { get; set;  }

        public int? Sequence { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ICollection<EventTemplate> EventTemplates { get; set; }
        public ICollection<GolfClubTemplate> GolfClubTemplates { get; set; }

        public ICollection<ConvenienceTemplate> ConvenienceTemplates { get; set; }

        [NotMapped]
        public int Contents { get; set; }
    }
}