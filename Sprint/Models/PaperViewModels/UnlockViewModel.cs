using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Sprint.Models.AccountViewModels
{
    public class UnlockViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name ="Internal Controller Email")]
        public string ICEmail { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Internal Controller Password")]
        public string ICPassword { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Examiner Email")]
        public string ExaminerEmail { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Examiner Password")]
        public string ExaminerPassword { get; set; }

        public int DeptId { get; set; }

        public string DeptName { get; set; }
    }
}
