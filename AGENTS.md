# AGENTS.md - Development Guidelines for dot-entity-core-template

This document provides comprehensive guidelines for coding agents working on this ASP.NET Core Web API project. Follow these instructions to maintain consistency, quality, and best practices.

## Project Overview

This is a .NET 9 ASP.NET Core Web API template implementing:
- Entity Framework Core with Identity
- Repository pattern
- Service layer architecture
- OpenAPI/Swagger documentation
- JWT authentication

**Project Structure:**
- `Template.Api/` - Controllers, middleware, configuration
- `Template.Services/` - Business logic services
- `Template.Model/` - Entity models and DTOs
- `Template.Data/` - EF Core context and repositories
- `Template.Common/` - Shared utilities

## Build, Test, and Lint Commands

### Building
```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build Template.Api/Template.Api.csproj

# Clean and build
dotnet clean && dotnet build
```

### Running the Application
```bash
# Run the API (from Template.Api directory)
dotnet run --project Template.Api

# Run with specific profile
dotnet run --project Template.Api --launch-profile https
```

### Testing
```bash
# Run all tests (when test projects exist)
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test Template.Api.Tests/Template.Api.Tests.csproj

# Run single test method
dotnet test --filter "TestMethodName"

# Run tests in watch mode
dotnet watch test
```

### Code Formatting and Linting
```bash
# Format all code (whitespace and style)
dotnet format

# Format whitespace only
dotnet format whitespace

# Format code style only
dotnet format style

# Run analyzers only
dotnet format analyzers

# Verify no changes would be made (CI check)
dotnet format --verify-no-changes

# Format specific files
dotnet format --include Template.Api/Controllers/*.cs

# Exclude specific diagnostics
dotnet format --exclude-diagnostics IDE0001,IDE0002
```

### Database Operations
```bash
# Create migration
dotnet ef migrations add InitialCreate --project Template.Data

# Update database
dotnet ef database update --project Template.Data

# Generate SQL script
dotnet ef migrations script --project Template.Data
```

## Code Style Guidelines

### Language Features and Framework Usage
- **Target Framework:** .NET 9.0
- **Nullable Reference Types:** Enabled - always handle nulls appropriately
- **Implicit Usings:** Enabled
- **File-scoped Namespaces:** Preferred for new files
- **Top-level Statements:** Avoid in library projects, acceptable in Program.cs
- **Records:** Use for immutable DTOs and value objects
- **Async/Await:** Always use async methods for I/O operations
- **Dependency Injection:** Use constructor injection, register services in Program.cs

### Naming Conventions

#### Classes and Types
- **PascalCase** for all type names
- **Interface prefix:** `I` (e.g., `IUserServices`, `IRepository`)
- **Generic type parameters:** `T`, `TEntity`, `TResult`
- **Attribute suffix:** `Attribute` (e.g., `AuthorizeAttribute`)

#### Methods and Properties
- **PascalCase** for public methods and properties
- **Private methods:** camelCase or PascalCase (follow existing pattern)
- **Async methods:** End with `Async` (e.g., `GetUserAsync`)

#### Variables and Fields
- **camelCase** for local variables and parameters
- **Private fields:** `_camelCase` with underscore prefix
- **Constants:** `PascalCase` or `UPPER_CASE` for true constants

#### Files and Namespaces
- **File names:** Match class name (e.g., `UserServices.cs`)
- **Namespaces:** Follow directory structure (e.g., `Template.Services`)
- **Directories:** PascalCase for project names, camelCase for subdirectories

### Code Organization

#### File Structure
```csharp
// 1. Using statements (system first, then third-party, then project)
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Template.Model.Entities;

// 2. Namespace declaration
namespace Template.Services
{
    // 3. Class definition
    public class UserServices : Repository<User>, IUserServices
    {
        // 4. Fields (private fields with underscore)
        private readonly ApplicationDbContext _context;

        // 5. Constructor
        public UserServices(ApplicationDbContext context)
        {
            _context = context;
        }

        // 6. Properties
        public string SomeProperty { get; set; }

        // 7. Methods (public, then private/protected)
        public async Task<User> GetUserByIdAsync(int id)
        {
            // Implementation
        }

        private void PrivateMethod()
        {
            // Implementation
        }
    }
}
```

#### Regions (Use Sparingly)
```csharp
#region Fields
private readonly ILogger<UserServices> _logger;
#endregion

#region Constructor
public UserServices(ILogger<UserServices> logger)
{
    _logger = logger;
}
#endregion

#region Methods
public void PublicMethod() { }
#endregion
```

### Imports and Dependencies

#### Using Statements
- Group by namespace hierarchy
- Remove unused imports
- Prefer qualified imports for conflicting names
- Use global usings in Program.cs for common namespaces

#### NuGet Packages
- Check existing packages before adding new ones
- Use consistent versioning across projects
- Document major package additions in PR descriptions

### Entity Framework and Database

#### Entity Configuration
```csharp
// Entity classes
[Table("AspNetUsers")]
public class User : IdentityUser
{
    [Column(TypeName = "nvarchar(250)")]
    public string Name { get; set; }

    [NotMapped]
    public string FullName => $"{Name} {LastName}";
}
```

#### Repository Pattern
```csharp
// Interface
public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity> GetByIdAsync(int id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task InsertAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);
}

// Implementation
public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly ApplicationDbContext _context;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TEntity> GetByIdAsync(int id)
    {
        return await _context.Set<TEntity>().FindAsync(id);
    }
}
```

#### Query Patterns
```csharp
// Use AsNoTracking for read-only queries
public async Task<List<User>> GetActiveUsersAsync()
{
    return await _context.Users
        .Where(u => u.IsActive == true)
        .AsNoTracking()
        .ToListAsync();
}

// Include related entities explicitly
public async Task<User> GetUserWithRolesAsync(int id)
{
    return await _context.Users
        .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
        .FirstOrDefaultAsync(u => u.Id == id);
}
```

### API Controllers

#### Controller Structure
```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserServices _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserServices userService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
            return NotFound();

        return Ok(_mapper.Map<UserDto>(user));
    }
}
```

#### Action Results
- Use typed results with `ActionResult<T>`
- Return appropriate HTTP status codes
- Use `IActionResult` for variable return types
- Document response types with attributes

### Error Handling

#### Exception Handling
```csharp
// Service layer - throw custom exceptions
public async Task<User> GetUserByIdAsync(int id)
{
    var user = await _repository.GetByIdAsync(id);
    if (user == null)
        throw new NotFoundException($"User with id {id} not found");

    return user;
}

// Controller layer - handle exceptions
[HttpGet("{id}")]
public async Task<ActionResult<UserDto>> GetUser(int id)
{
    try
    {
        var user = await _userService.GetUserByIdAsync(id);
        return Ok(_mapper.Map<UserDto>(user));
    }
    catch (NotFoundException)
    {
        return NotFound();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting user {Id}", id);
        return StatusCode(500, "Internal server error");
    }
}
```

#### Validation
```csharp
// Data annotations
public class CreateUserRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }
}

// Manual validation
[HttpPost]
public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequest request)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    // Additional business validation
    if (await _userService.EmailExistsAsync(request.Email))
    {
        ModelState.AddModelError("Email", "Email already exists");
        return BadRequest(ModelState);
    }

    var user = await _userService.CreateUserAsync(request);
    return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
}
```

### Logging

#### Structured Logging
```csharp
public class UserServices : IUserServices
{
    private readonly ILogger<UserServices> _logger;

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        _logger.LogInformation("Creating user with email {Email}", request.Email);

        try
        {
            var user = new User { Email = request.Email };
            await _repository.InsertAsync(user);

            _logger.LogInformation("User created successfully with ID {UserId}", user.Id);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user with email {Email}", request.Email);
            throw;
        }
    }
}
```

### Security Best Practices

#### Authentication & Authorization
```csharp
// JWT Bearer authentication configured in Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* configuration */ });

// Controller authorization
[Authorize]
[HttpGet("profile")]
public async Task<ActionResult<UserProfileDto>> GetProfile()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    // Implementation
}

// Role-based authorization
[Authorize(Roles = "Admin")]
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteUser(int id)
{
    // Implementation
}
```

#### Input Validation
- Always validate user input
- Use parameterized queries (EF Core handles this)
- Sanitize data before storage
- Implement rate limiting for public endpoints

### Testing Guidelines

#### Unit Test Structure
```csharp
// When test projects exist, follow this structure
public class UserServicesTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly UserServices _userServices;

    public UserServicesTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _userServices = new UserServices(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var userId = 1;
        var expectedUser = new User { Id = userId, Name = "Test User" };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _userServices.GetUserByIdAsync(userId);

        // Assert
        Assert.Equal(expectedUser, result);
    }
}
```

#### Test Naming Convention
- `MethodName_Scenario_ExpectedResult`
- Use descriptive names for complex scenarios
- Group related tests in classes

### Documentation

#### API Documentation
```csharp
/// <summary>
/// Manages user-related operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    /// <summary>
    /// Gets a user by their ID
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <returns>The user information</returns>
    /// <response code="200">Returns the user</response>
    /// <response code="404">User not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        // Implementation
    }
}
```

### Performance Considerations

#### Database Optimization
- Use `AsNoTracking()` for read-only queries
- Implement pagination for large datasets
- Use `Include()` judiciously to avoid N+1 queries
- Consider compiled queries for frequently used queries

#### Async Best Practices
- Use `async/await` throughout the call stack
- Avoid `.Result` and `.Wait()`
- Use `Task.WhenAll()` for concurrent operations
- Configure DbContext pooling

### Common Patterns and Anti-patterns

#### ✅ Do's
- Use dependency injection everywhere
- Handle exceptions appropriately
- Write descriptive commit messages
- Keep methods small and focused
- Use meaningful variable names
- Document public APIs

#### ❌ Don'ts
- Don't use magic strings/numbers
- Don't hardcode configuration values
- Don't expose sensitive information in logs
- Don't commit secrets or connection strings
- Don't mix concerns in a single class
- Don't ignore compiler warnings

### Code Review Checklist

Before submitting a PR, ensure:
- [ ] Code builds successfully
- [ ] All tests pass
- [ ] Code is formatted (`dotnet format`)
- [ ] No nullable reference warnings
- [ ] Appropriate logging added
- [ ] Input validation implemented
- [ ] Error handling in place
- [ ] Documentation updated
- [ ] Security considerations addressed

### Tooling and Extensions

#### Recommended VS Code Extensions
- C# (Microsoft)
- C# Extensions (jchannon)
- NuGet Package Manager
- REST Client

#### Useful Commands
```bash
# Create new migration
dotnet ef migrations add MigrationName --project Template.Data

# Scaffold controller
dotnet aspnet-codegenerator controller -name UsersController -async -api -m User -dc ApplicationDbContext --relativeFolderPath Controllers --project Template.Api

# Add package
dotnet add Template.Api package Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

This document should be updated as the project evolves and new patterns emerge.</content>
<parameter name="filePath">/mnt/REPOS/repos/Personal/dot-entity-core-template/AGENTS.md