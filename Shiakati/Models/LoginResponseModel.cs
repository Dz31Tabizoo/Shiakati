using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class LoginResponseModel
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public int UserID { get; set; }
        public string? Username { get; set; }

        public string? Role { get; set; }
    }
}
