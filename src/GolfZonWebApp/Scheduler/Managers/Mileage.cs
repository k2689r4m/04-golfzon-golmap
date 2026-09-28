using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using GolfZonWebApp.Models;

namespace GolfZonWebApp.Scheduler.Managers
{
    public class Mileage : BaseEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int UserPossbMileage { get; set; }
    }
}
