using System.ComponentModel.DataAnnotations;

namespace ZaloOA.Application.DTOs.ZaloOA;

public class ConnectOAuth2Request
{
    [Required]
    public string Code { get; set; } = null!;

    public string? CodeVerifier { get; set; }
}
