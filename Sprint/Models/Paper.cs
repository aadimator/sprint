using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sprint.Models
{
    public class Paper
    {
        public int PaperId { get; set; }
        public string EncKey { get; set; }
        public string Hash { get; set; }
        public string UploaderId { get; set; }
        public ApplicationUser Uploader { get; set; }
        public bool Done { get; set; }
        public bool Approved { get; set; }
        public bool Delete { get; set; }
        public bool Locked { get; set; }
        public string Comment { get; set; }
        public string Report { get; set; }

        [Required]
        public string Title { get; set; }

        [Display(Name = "File Name")]
        [Required]
        public string FileName { get; set; }

        [Display(Name = "No. of Copies")]
        [Range(0, 500)]
        [Required]
        public int Copies { get; set; }

        [DataType(DataType.Date)]
        public DateTime CreatedAt { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime UnlockedAt { get; set; }
    }
}
