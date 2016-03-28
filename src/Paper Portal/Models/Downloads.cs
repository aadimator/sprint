using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Paper_Portal.Models
{
    public class Downloads
    {
        public int DownloadsId { get; set; }

        public Paper Paper { get; set; }
        public ApplicationUser User { get; set; }
    }
}
