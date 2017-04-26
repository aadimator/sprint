using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Sprint.Models;

namespace Sprint.Models.PaperViewModels
{
    public class StatusViewModel
    {
        public IEnumerable<Paper> Completed { get; set; }
        public IEnumerable<Paper> Incomplete { get; set; }
    }
}
