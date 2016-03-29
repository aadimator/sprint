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

        [Display(Name = "No. of Copies")]
        [Range(0, 500)]
        public int Copies { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode =true)]
        public DateTime Due { get; set; }

        public string Instructor { get; set; }

        public IFormFile File { get; set; }

        public string Comment { get; set; }

        public CreateViewModel()
        {
            Copies = 2;
            Due = DateTime.Today.AddDays(2);
        }
    }
}
