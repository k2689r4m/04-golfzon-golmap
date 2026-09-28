using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using Newtonsoft.Json;

namespace GolfZonWebApp.Models
{
    public class Roulette : BaseEntity
    {
        public int Id { get; set; }
        public int Size { get; set; }
        [StringLength(2)]
        public string Status { get; set; }
        public int StampSetting { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Version { get; set; }

        public ICollection<RouletteItem> RouletteItems { get; set; }
        public ICollection<RouletteActionOption> RouletteActionOptions { get; set; }
    }
}
