using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template.Data.Repository;
using Template.Model.Entities;

namespace Template.Services.Interfaces
{
    public interface IRoleServices : IRepository<Role>
    {
    }
}
