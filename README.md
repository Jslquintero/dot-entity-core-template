# ASP.NET Core API Template

An enterprise-ready ASP.NET Core web API template with Entity Framework, Identity, JWT authentication, background job processing, and comprehensive monitoring. Built with .NET 9.0 and follows clean architecture principles with Repository pattern.

## Features

- **ASP.NET Core 9.0** - Latest .NET framework with modern features
- **Entity Framework Core** - ORM with code-first migrations and database-agnostic design
- **Identity Framework** - User authentication and authorization with JWT tokens
- **Repository Pattern** - Clean data access layer with generic repositories
- **Serilog Logging** - Structured logging with database storage options
- **Hangfire** - Background job processing with web-based dashboard
- **Health Checks** - Application and database monitoring endpoints
- **AutoMapper** - Object-to-object mapping for DTOs and ViewModels
- **OpenAPI/Swagger Documentation** - Interactive API documentation
- **CORS Support** - Cross-origin resource sharing configuration
- **Frontend Agnostic** - Works with any frontend framework or mobile app

## Quick Start

1. Clone the repository:
   ```bash
   git clone https://github.com/Jslquintero/aspnet-repository-pattern-template
   cd aspnet-core-template
   ```

2. Install the template:
   ```bash
   dotnet new install ./
   ```

3. Create a new project:
   ```bash
   dotnet new repoapi -n YourProjectName
   ```

4. Navigate to your project and update connection string in `appsettings.json`

5. Start the application:
   ```bash
   dotnet run
   ```

6. Access the application:
   - **API Documentation**: `https://localhost:5001/swagger`
   - **Health Checks**: `https://localhost:5001/healthz`
   - **Hangfire Dashboard**: `https://localhost:5001/hangfire` (requires authentication)

7. **Default Admin User** (automatically created on first run):
   - Email: `admin@admin.com`
   - Password: `Admin1234`
   - Role: `SysAdmin`

## Authentication

The template includes JWT-based authentication with the following roles:
- `SysAdmin` - System administrator with full access
- `Admin` - Administrative user
- `Reparacion` - Repair technician
- `Caja` - Cashier

## Background Jobs

Hangfire is configured for background job processing:
- Dashboard available at `/hangfire`
- In-memory storage (development) - configure persistent storage for production
- Job queues and worker threads automatically managed

## Monitoring & Logging

- **Health Checks**: Monitor application and database health at `/healthz`
- **Serilog**: Structured logging with console output and database storage options
- **Database Monitoring**: Entity Framework health checks included

## Database Configuration

The template supports multiple database providers:
- SQL Server (default configuration in comments)
- PostgreSQL (configuration examples included)
- Automatic migrations on startup

## Project Structure

- `Template.Api/` - Main API project containing controllers, middleware, configuration, and Program.cs
  - `Controllers/` - API controllers (Users,...)
  - `Middlewares/` - Custom middleware (ApplicationIdentityMiddleware)
  - `Models/` - ViewModels and DTOs (LoginViewModel, UserViewModel)
  - `AutoMapping.cs` - AutoMapper configuration
  - `Program.cs` - Application startup and configuration
- `Template.Common/` - Shared utilities and common code
- `Template.Data/` - Data access layer with EF Core context, repositories, and migrations
- `Template.Model/` - Entity models and database entities
- `Template.Services/` - Business logic services following repository pattern

## Development Notes

This template includes enterprise-level configurations that are production-ready:

- **Security**: JWT authentication, CORS policies, and secure defaults
- **Monitoring**: Health checks and comprehensive logging
- **Background Processing**: Hangfire for job queuing and processing
- **Database**: Agnostic design supporting SQL Server and PostgreSQL
- **Identity**: Pre-configured user roles and seeding for immediate use

For production deployment:
1. Configure persistent database storage (replace in-memory Hangfire storage)
2. Set up proper logging database or external logging service
3. Configure JWT signing keys and secure token storage
4. Update CORS policies for your specific domains
5. Review and adjust password policies as needed

## Frontend Integration

This API works with any frontend framework (React, Angular, Vue, etc.) or mobile app. JWT tokens can be used for authentication in client applications.

## License

MIT
