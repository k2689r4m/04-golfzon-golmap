using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfZonWebApp.Models
{
    public class MaintenanceUser
    {
        public int Id { get; set; }
        [ForeignKey("MaintenanceId")]
        public int MaintenanceId { get; set; }
        public int UserNum { get; set; }
    }
}