using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Paper_Portal.Models
{
    public class Paper
    {
        public int PaperId { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public string EncKey { get; set; }
        public string Hash { get; set; }
        public string Instructor { get; set; }
        public bool Complete { get; set; }
        public string Comment { get; set; }
        public string Report { get; set; }

        [Display(Name = "No. of Copies")]
        [Range(0, 500)]
        [Required]
        public int Copies { get; set; }

        [Display(Name ="No. of Downloads")]
        public int DownloadsNum { get; set; }

        [DataType(DataType.Date)]
        public DateTime Created { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime Due { get; set; }

        public string UploaderId { get; set; }
        public ApplicationUser Uploader { get; set; }

        public ICollection<Downloads> Downloader { get; set; }
    }
}
