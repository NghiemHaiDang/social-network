using System.ComponentModel.DataAnnotations;

namespace ZaloOA.Application.DTOs.ZaloOA;

public class ConnectApiKeyRequest
{
    [Required]
    public string AccessToken { get; set; } = null!;

    public string? RefreshToken { get; set; }
}
