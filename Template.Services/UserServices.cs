using Template.Model.Entities;
using Template.Data.Repository;
using Template.Services.Interfaces;
using Template.Data;

namespace Template.Services
{
    public class UserServices : Repository<User>, IUserServices
    {
        private readonly ApplicationDbContext _context;
        public UserServices(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // Additional methods specific to UserServices can be added here
    }
}
