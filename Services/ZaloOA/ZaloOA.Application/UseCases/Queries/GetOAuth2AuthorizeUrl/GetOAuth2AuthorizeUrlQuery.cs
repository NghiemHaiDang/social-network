using ZaloOA.Application.Common.Interfaces;
using ZaloOA.Application.DTOs;

namespace ZaloOA.Application.UseCases.Queries.GetOAuth2AuthorizeUrl;

public record GetOAuth2AuthorizeUrlQuery(string? RedirectUri) : IQuery<OAuth2AuthorizeUrlDto>;
