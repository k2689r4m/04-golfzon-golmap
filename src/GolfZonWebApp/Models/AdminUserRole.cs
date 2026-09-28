using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace GolfZonWebApp.Models
{
    public class AdminUserRole : IdentityUserRole<string>
    {
        public override string UserId { 
            
            get
            {
                return base.UserId;
            }

            set
            {
                base.UserId = value;
            }
        }
        public AdminUser AdminUser { get; set; }

        public override string RoleId
        {
            get
            {
                return base.RoleId;
            }
            set
            {
                base.RoleId = value;
            }
        }
        public AdminRole AdminRole { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
