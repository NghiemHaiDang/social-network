namespace ZaloOA.Application.DTOs.ZaloOA;

public class ZaloOAListResponse
{
    public List<ZaloOAResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
