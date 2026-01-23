using Microsoft.Extensions.DependencyInjection;
using ZaloOA.Application.Common.Interfaces;
using ZaloOA.Application.DTOs;
using ZaloOA.Application.UseCases.Commands.ConnectWithApiKey;
using ZaloOA.Application.UseCases.Commands.ConnectWithOAuth2;
using ZaloOA.Application.UseCases.Commands.DisconnectAccount;
using ZaloOA.Application.UseCases.Commands.RefreshToken;
using ZaloOA.Application.UseCases.Queries.GetAccountById;
using ZaloOA.Application.UseCases.Queries.GetConnectedAccounts;
using ZaloOA.Application.UseCases.Queries.GetOAuth2AuthorizeUrl;

namespace ZaloOA.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Command Handlers
        services.AddScoped<ICommandHandler<ConnectWithOAuth2Command, ZaloOAAccountDto>, ConnectWithOAuth2Handler>();
        services.AddScoped<ICommandHandler<ConnectWithApiKeyCommand, ZaloOAAccountDto>, ConnectWithApiKeyHandler>();
        services.AddScoped<ICommandHandler<DisconnectAccountCommand>, DisconnectAccountHandler>();
        services.AddScoped<ICommandHandler<RefreshTokenCommand, ZaloOAAccountDto>, RefreshTokenHandler>();

        // Query Handlers
        services.AddScoped<IQueryHandler<GetOAuth2AuthorizeUrlQuery, OAuth2AuthorizeUrlDto>, GetOAuth2AuthorizeUrlHandler>();
        services.AddScoped<IQueryHandler<GetConnectedAccountsQuery, ZaloOAAccountListDto>, GetConnectedAccountsHandler>();
        services.AddScoped<IQueryHandler<GetAccountByIdQuery, ZaloOAAccountDto>, GetAccountByIdHandler>();

        return services;
    }
}
