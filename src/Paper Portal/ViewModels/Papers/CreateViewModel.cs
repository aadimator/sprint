using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Paper_Portal.ViewModels.Papers
{
    public class CreateViewModel
    {
        [Required]
        [StringLength(50, ErrorMessage = "File Name cannot be longer than 50 characters.")]
        public string Title { get; set; }

        public int Copies { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime Due { get; set; }

        public string Instructor { get; set; }

        [Required]
        public IFormFile File { get; set; }

        public string Comment { get; set; }
    }
}
