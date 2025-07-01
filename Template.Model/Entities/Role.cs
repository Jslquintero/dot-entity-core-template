using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Template.Model.Entities
{
    [Table("AspNetRoles")]
    public  class Role : IdentityRole
    {
    }
}
