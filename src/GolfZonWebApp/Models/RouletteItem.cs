using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using Newtonsoft.Json;

namespace GolfZonWebApp.Models
{
    public class RouletteItem : BaseEntity
    {
        public int Id { get; set; }
        [ForeignKey("RouletteId")]
        //public Roulette Roulette { get; set; }
        public int RouletteId { get; set; }
        public char Type { get; set; }
        [StringLength(50)]
        public string Win { get; set; }
        public double Percentage { get; set; }
        public int Quantity { get; set; }
        public string FileOriginalName { get; set; }
        public string FileName { get; set; }
        public string FileUri { get; set; }

        [NotMapped]
        public int? ImageIndex { get; set; }
    }
}
