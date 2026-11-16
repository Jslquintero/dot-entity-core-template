using Template.Data;
using Template.Data.Repository;
using Template.Model.Entities;
using Template.Services.Interfaces;

namespace Template.Services;

public class RoleServices(ApplicationDbContext context) : Repository<Role>(context), IRoleServices;
