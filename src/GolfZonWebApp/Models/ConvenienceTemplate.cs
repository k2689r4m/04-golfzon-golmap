using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfZonWebApp.Models
{
    public class ConvenienceTemplate : BaseEntity
    {
        public int Id { get; set; }
        [ForeignKey("TemplateId")]
        public Template Template { get; set; }
        public int TemplateId { get; set; }
        [ForeignKey("ConvenienceId")]
        public Convenience Convenience { get; set; }
        public int ConvenienceId { get; set; }
    }
}