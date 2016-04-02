using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Paper_Portal.Models
{
    public class Downloads
    {
        public int PaperId { get; set; }
        public Paper Paper { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
