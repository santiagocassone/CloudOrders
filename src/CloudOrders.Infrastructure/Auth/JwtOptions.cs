using System.ComponentModel.DataAnnotations;

namespace CloudOrders.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(AllowEmptyStrings = false)]
    [MinLength(32)]
    public required string Key { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string Issuer { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string Audience { get; init; }
}