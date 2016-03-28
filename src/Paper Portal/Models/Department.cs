using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Paper_Portal.Models
{
    public class Department
    {
        public int DepartmentId { get; set; }
        public string Name { get; set; }

        public ICollection<ApplicationUser> Users { get; set; }
    }
}
