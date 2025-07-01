using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template.Data;
using Template.Data.Repository;
using Template.Model.Entities;
using Template.Services.Interfaces;

namespace Template.Services
{
    public class RoleServices : Repository<Role>, IRoleServices
    {
        private readonly ApplicationDbContext _context;
        public RoleServices(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // Additional methods specific to RoleServices can be added here
    }
}
