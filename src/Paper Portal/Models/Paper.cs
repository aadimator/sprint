using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Paper_Portal.Models
{
    public class Paper
    {
        public int PaperId { get; set; }
        public string FileName { get; set; }
        public int Copies { get; set; }
        public DateTime Due { get; set; }
        public string EncKey { get; set; }
        public string Hash { get; set; }
        public string Instructor { get; set; }

        public string UploaderId { get; set; }
        public ApplicationUser Uploader { get; set; }
    }
}
