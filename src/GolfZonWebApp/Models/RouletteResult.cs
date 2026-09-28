using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using Newtonsoft.Json;

namespace GolfZonWebApp.Models
{
    public class RouletteResult : BaseEntity
    {
        public int Id { get; set; }
        [ForeignKey("RouletteId")]
        //public Roulette Roulette { get; set; }
        public int RouletteId { get; set; }
        public Roulette Roulette { get; set; }
        [ForeignKey("RouletteItemId")]
        public int RouletteItemId { get; set; }
        public RouletteItem RouletteItem { get; set; }
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public User User { get; set; }

        public string RouletteItemWin { get; set; }
        public string RouletteItemType { get; set; }
        public string RouletteItemFileUri { get; set; }

        public string Contact { get; set; }
        public string Address { get; set; }
        public string AddressDetail { get; set; }
        public bool Result { get; set; }
        public string Name { get; set; }
    }
}
