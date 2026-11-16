using System.ComponentModel.DataAnnotations;

namespace Template.Api.Models;

public sealed record CreateUserRequest
{
    [Required, EmailAddress, StringLength(256)]
    public required string Email { get; init; }
    [Required, StringLength(128, MinimumLength = 12)]
    public required string Password { get; init; }
    [Required, StringLength(250)]
    public required string Name { get; init; }
    [Required, StringLength(250)]
    public required string LastName { get; init; }
}
