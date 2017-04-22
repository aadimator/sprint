using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using System.ComponentModel.DataAnnotations;

namespace Paper_Portal.ViewModels.Manage
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
