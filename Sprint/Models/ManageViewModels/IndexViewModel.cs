using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Sprint.Models.ManageViewModels
{
    public class IndexViewModel
    {
        [Display(Name =("User Name"))]
        public string UserName { get; set; }
        [Display(Name =("Full Name"))]
        public string FullName { get; set; }

        public string Department { get; set; }
        public string Email { get; set; }
    }
}
