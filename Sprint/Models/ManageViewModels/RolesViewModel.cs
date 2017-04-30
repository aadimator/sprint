using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sprint.Models.ManageViewModels
{
    public class RolesViewModel
    {
        public string Id { get; set; }

        [Display(Name ="Full Name")]
        public string FullName { get; set; }

        public string Email { get; set; }
        
        public List<Roles> Roles { get; set; }

    }

    public class Roles
    {
        public string Name { get; set; }
        public bool Selected { get; set; }
    }
}
