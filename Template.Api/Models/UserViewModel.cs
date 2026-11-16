namespace Template.Api.Models;

public sealed record UserViewModel(
    string Id, string Email, string Name, string LastName, IList<string> Roles);

public sealed record LoginResponse(string Token, DateTime TokenExpirationDate, UserViewModel User);
