using System.ComponentModel.DataAnnotations;

namespace Template.Api.Models;

public sealed record LoginViewModel
{
    [Required, EmailAddress, StringLength(256)]
    public required string Email { get; init; }
    [Required, StringLength(128)]
    public required string Password { get; init; }
}
