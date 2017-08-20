using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Sprint.Models.PaperViewModels
{
    public class DeptViewModel
    {
        public int DeptId { get; set; }
        public string DeptName { get; set; }

        public int Done { get; set; }
        public int Undone { get; set; }
    }
}
