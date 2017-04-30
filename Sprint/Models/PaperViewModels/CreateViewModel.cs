using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Sprint.Models.PaperViewModels
{
    public class CreateViewModel
    {
        [Required]
        [StringLength(50, ErrorMessage = "File Name cannot be longer than 50 characters.")]
        public string Title { get; set; }

        public int Copies { get; set; }

        [Required]
        public IFormFile File { get; set; }

        public string Comment { get; set; }
    }
}
