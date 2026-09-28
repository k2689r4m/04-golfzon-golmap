using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GolfZonWebApp.Models
{
    public class AdminRole : IdentityRole
    {
        public string Type { get; set; }
        public string EngType { get; set; }
        public int Order { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ICollection<AdminUserRole> adminUserRoles { get; set; }
    }
}
