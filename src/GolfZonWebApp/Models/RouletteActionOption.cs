using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using Newtonsoft.Json;

namespace GolfZonWebApp.Models
{
    public class RouletteActionOption
    {
        public int Id { get; set; }
        [ForeignKey("RouletteId")]
        //public Roulette Roulette { get; set; }
        public int RouletteId { get; set; }
        [ForeignKey("ActionOptionObjectId")]
        //public Roulette Roulette { get; set; }
        public int ActionOptionObjectId { get; set; }
    }
}
