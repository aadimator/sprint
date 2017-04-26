using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Sprint.Models.ManageViewModels
{
    public class VerifyUsersViewModel
    {
        public string Id { get; set; }

        [Display(Name ="Full Name")]
        public string FullName { get; set; }

        public string Email { get; set; }
        public string Department { get; set; }

        [Display(Name =("Email Confirmed"))]
        public bool EmailConfirmed { get; set; }

        public bool Selected { get; set; }

        public VerifyUsersViewModel(string id, string name, string email, string department, bool emailConfirmed)
        {
            Id = id;
            Email = email;
            Department = department;
            EmailConfirmed = emailConfirmed;
            FullName = (name == null) ? "Not Set" : name;
        }
    }
}
