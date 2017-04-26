using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sprint.Models
{
    public class Downloads
    {
        public int DownloadsId { get; set; }

        public int PaperId { get; set; }
        public Paper Paper { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public DateTime DownloadedAt { get; set; }
    }
}
