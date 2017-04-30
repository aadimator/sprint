using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sprint.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        [Display(Name ="Name")]
        public string FullName { get; set; }
        public bool Verified { get; set; }
        public int? DepartmentId { get; set; }
        public Department Department { get; set; }

        public ICollection<Paper> Uploads { get; set; }
    }
}
