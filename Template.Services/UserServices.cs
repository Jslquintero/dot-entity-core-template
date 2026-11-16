using Template.Data;
using Template.Data.Repository;
using Template.Model.Entities;
using Template.Services.Interfaces;

namespace Template.Services;

public class UserServices(ApplicationDbContext context) : Repository<User>(context), IUserServices;
