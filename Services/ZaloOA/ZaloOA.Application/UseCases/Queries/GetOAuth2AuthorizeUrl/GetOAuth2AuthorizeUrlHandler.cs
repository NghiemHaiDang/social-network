using ZaloOA.Application.Common;
using ZaloOA.Application.Common.Interfaces;
using ZaloOA.Application.DTOs;

namespace ZaloOA.Application.UseCases.Queries.GetOAuth2AuthorizeUrl;

public class GetOAuth2AuthorizeUrlHandler : IQueryHandler<GetOAuth2AuthorizeUrlQuery, OAuth2AuthorizeUrlDto>
{
    private readonly IZaloConfiguration _configuration;

    public GetOAuth2AuthorizeUrlHandler(IZaloConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<Result<OAuth2AuthorizeUrlDto>> HandleAsync(GetOAuth2AuthorizeUrlQuery query, CancellationToken cancellationToken = default)
    {
        var state = Guid.NewGuid().ToString("N");
        var redirectUri = query.RedirectUri ?? _configuration.DefaultRedirectUri;

        var authorizeUrl = $"{_configuration.OAuthAuthorizeUrl}" +
            $"?app_id={_configuration.AppId}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&state={state}";

        var response = new OAuth2AuthorizeUrlDto
        {
            AuthorizeUrl = authorizeUrl,
            State = state
        };

        return Task.FromResult(Result<OAuth2AuthorizeUrlDto>.Success(response));
    }
}
