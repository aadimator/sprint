using System.ComponentModel.DataAnnotations;

namespace Paper_Portal.ViewModels.Manage
{
    public class EditVM
    {
        [Required]
        [StringLength(75, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 5)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
    }
}
