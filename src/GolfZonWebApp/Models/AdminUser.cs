using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfZonWebApp.Models
{
    public class AdminUser : IdentityUser
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; }

        public string Department { get; set; }

        [Range(0, 2)] //0: normal, 1: deleted, 2: force deleted
        public int Status { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ICollection<AdminUserRole> AdminUserRoles { get; set; }

        [StringLength(130)]
        public string AccessToken { get; set; }

        public DateTime? LockDateTime { get; set; }
    }
}
