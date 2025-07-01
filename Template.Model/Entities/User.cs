using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Template.Model.Entities
{
    [Table("AspNetUsers")]
    public class User : IdentityUser
    {
        public User()
        {
            Name = string.Empty;
            LastName = string.Empty;
            Roles = [];
        }

        #region Properties  
        [Column(TypeName = "nvarchar(250)")]
        public string Name { get; set; }
        [Column(TypeName = "nvarchar(250)")]
        public string LastName { get; set; }

        public bool? IsActive { get; set; }
        #endregion

        #region NotMappeds  
        [NotMapped]
        public IList<string> Roles { get; set; }

        public string FullName => $"{Name} {LastName}";

        public string ShortName
        {
            get
            {
                if (string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(LastName))
                {
                    return string.Empty;
                }
                else if (string.IsNullOrEmpty(Name))
                {
                    return LastName;
                }
                else if (string.IsNullOrEmpty(LastName))
                {
                    return Name;
                }
                else
                {
                    return $"{Name[0]}. {LastName}";
                }
            }
        }
        #endregion

        #region Relationships  
        #endregion
    }
}
