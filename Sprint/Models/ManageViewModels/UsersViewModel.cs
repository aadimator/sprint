using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sprint.Models.ManageViewModels
{
    public class UsersViewModel
    {
        public string Id { get; set; }

        [Display(Name ="Full Name")]
        public string FullName { get; set; }

        public string Email { get; set; }
        public string Department { get; set; }

        [Display(Name =("Email Confirmed"))]
        public bool EmailConfirmed { get; set; }

        public bool Verified { get; set; }

        public bool Selected { get; set; }

        public IList<String> Roles { get; set; }

        public UsersViewModel(string id, string name, string email, string department, 
            bool emailConfirmed, bool verified, IList<String> roles)
        {
            Id = id;
            Email = email;
            Department = department;
            EmailConfirmed = emailConfirmed;
            FullName = name;
            Verified = verified;
            Roles = roles;
        }
    }
}
