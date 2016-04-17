using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Paper_Portal.Models;

namespace Paper_Portal.ViewModels.Papers
{
    public class StatusViewModel
    {
        public IEnumerable<Paper> Completed { get; set; }
        public IEnumerable<Paper> Incomplete { get; set; }
    }
}
