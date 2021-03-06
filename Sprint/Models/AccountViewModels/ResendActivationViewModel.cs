﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Sprint.Models.AccountViewModels
{
    public class ResendActivationViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
