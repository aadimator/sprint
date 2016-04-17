using System;

namespace Paper_Portal.ViewModels.Email
{
    public class JobStatusVM
    {
        public string BaseURL { get; set; }
        public string Action { get; set; }
        public string ActionBy { get; set; }
        public string Title { get; set; }
        public int Copies { get; set; }
        public DateTime At { get; set; }
        public string Detail { get; set; }
    }
}
