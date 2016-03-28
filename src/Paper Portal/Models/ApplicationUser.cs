﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Paper_Portal.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public bool Verified { get; set; }
        public int DepartmentId { get; set; }
        public Department Department { get; set; }

        public ICollection<Paper> Uploads { get; set; } // for teachers
        public ICollection<Downloads> Downloads { get; set; } // for printers
    }
}
