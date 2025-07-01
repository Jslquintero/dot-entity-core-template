# ASP.NET Core API Template

A clean ASP.NET Core web API template with Entity Framework, MVCS and Repository pattern.

## Features

- ASP.NET Core (Latest)
- Entity Framework Core
- Repository Pattern
- OpenAPI/Swagger Documentation
- Frontend Agnostic

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

6. View API docs at `https://localhost:5001/swagger`

## Project Structure

- `API/` - OpenAPI project, this is where your controllers and conection string should go.
- `Common/` - Shared directory.
- `Data/` - This is where the repository, migrations and DbContext are so you should select this project when making a change in your database.
- `Model/` - Your tables are stored here.
- `Services/` Business logic.

## Frontend Integration

This API works with any frontend framework (React, Angular, Vue, etc.) or mobile app.

## License

MIT
