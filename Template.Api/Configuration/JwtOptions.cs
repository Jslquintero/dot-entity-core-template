using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Template.Api.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const string SecurityStampClaim = "security_stamp";

    [Required]
    public string Key { get; set; } = string.Empty;
    [Required]
    public string Issuer { get; set; } = string.Empty;
    [Required]
    public string Audience { get; set; } = string.Empty;
    [Range(1, 60)]
    public int ExpirationMinutes { get; set; } = 15;

    public bool HasStrongKey() => Encoding.UTF8.GetByteCount(Key) >= 32;
}
