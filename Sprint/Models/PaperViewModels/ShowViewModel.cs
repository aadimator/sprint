using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Sprint.Models.PaperViewModels
{
    public class ShowViewModel
    {
        public Paper Paper { get; set; }
        public byte[] PdfBytes { get; set; } 
    }
}
