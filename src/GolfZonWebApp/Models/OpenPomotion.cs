using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace GolfZonWebApp.Models
{
    public class OpenPomotion
    {
        [Key]
        public int AttendSequence { get; set; }
        public Byte AttendStatus { get; set; }
        public Int64 AttendUser { get; set; }
        public DateTime AccessDate { get; set; }
        public DateTime ProvideDate { get; set; }
    }
}
