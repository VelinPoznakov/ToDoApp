using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;


namespace TodoApp.Domain.Models
{
    public class ApplicationUser: IdentityUser
    {
        public string Custom { get; set; }
    }
}