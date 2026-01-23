using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZaloOA.Application.Common.Interfaces;
using ZaloOA.Application.DTOs;
using ZaloOA.Application.UseCases.Commands.ConnectWithApiKey;
using ZaloOA.Application.UseCases.Commands.ConnectWithOAuth2;
using ZaloOA.Application.UseCases.Commands.DisconnectAccount;
using ZaloOA.Application.UseCases.Commands.RefreshToken;
using ZaloOA.Application.UseCases.Queries.GetAccountById;
using ZaloOA.Application.UseCases.Queries.GetConnectedAccounts;
using ZaloOA.Application.UseCases.Queries.GetOAuth2AuthorizeUrl;

namespace ZaloOA.API.Controllers;

[ApiController]
[Route("api/zalooa")]
[Authorize]
public class ZaloOAController : ControllerBase
{
    private readonly IQueryHandler<GetOAuth2AuthorizeUrlQuery, OAuth2AuthorizeUrlDto> _getAuthUrlHandler;
    private readonly IQueryHandler<GetConnectedAccountsQuery, ZaloOAAccountListDto> _getAccountsHandler;
    private readonly IQueryHandler<GetAccountByIdQuery, ZaloOAAccountDto> _getAccountByIdHandler;
    private readonly ICommandHandler<ConnectWithOAuth2Command, ZaloOAAccountDto> _connectOAuth2Handler;
    private readonly ICommandHandler<ConnectWithApiKeyCommand, ZaloOAAccountDto> _connectApiKeyHandler;
    private readonly ICommandHandler<DisconnectAccountCommand> _disconnectHandler;
    private readonly ICommandHandler<RefreshTokenCommand, ZaloOAAccountDto> _refreshTokenHandler;

    public ZaloOAController(
        IQueryHandler<GetOAuth2AuthorizeUrlQuery, OAuth2AuthorizeUrlDto> getAuthUrlHandler,
        IQueryHandler<GetConnectedAccountsQuery, ZaloOAAccountListDto> getAccountsHandler,
        IQueryHandler<GetAccountByIdQuery, ZaloOAAccountDto> getAccountByIdHandler,
        ICommandHandler<ConnectWithOAuth2Command, ZaloOAAccountDto> connectOAuth2Handler,
        ICommandHandler<ConnectWithApiKeyCommand, ZaloOAAccountDto> connectApiKeyHandler,
        ICommandHandler<DisconnectAccountCommand> disconnectHandler,
        ICommandHandler<RefreshTokenCommand, ZaloOAAccountDto> refreshTokenHandler)
    {
        _getAuthUrlHandler = getAuthUrlHandler;
        _getAccountsHandler = getAccountsHandler;
        _getAccountByIdHandler = getAccountByIdHandler;
        _connectOAuth2Handler = connectOAuth2Handler;
        _connectApiKeyHandler = connectApiKeyHandler;
        _disconnectHandler = disconnectHandler;
        _refreshTokenHandler = refreshTokenHandler;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User ID not found in token");

    [HttpGet("oauth/authorize")]
    public async Task<IActionResult> GetOAuthAuthorizeUrl([FromQuery] string? redirectUri = null, CancellationToken cancellationToken = default)
    {
        var query = new GetOAuth2AuthorizeUrlQuery(redirectUri);
        var result = await _getAuthUrlHandler.HandleAsync(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("oauth/connect")]
    public async Task<IActionResult> ConnectWithOAuth2([FromBody] ConnectOAuth2Request request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var command = new ConnectWithOAuth2Command(userId, request.Code, request.CodeVerifier);
        var result = await _connectOAuth2Handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("connect/apikey")]
    public async Task<IActionResult> ConnectWithApiKey([FromBody] ConnectApiKeyRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var command = new ConnectWithApiKeyCommand(userId, request.AccessToken, request.RefreshToken);
        var result = await _connectApiKeyHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> GetConnectedAccounts(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var query = new GetConnectedAccountsQuery(userId);
        var result = await _getAccountsHandler.HandleAsync(query, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAccountById(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var query = new GetAccountByIdQuery(userId, id);
        var result = await _getAccountByIdHandler.HandleAsync(query, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DisconnectAccount(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var command = new DisconnectAccountCommand(userId, id);
        var result = await _disconnectHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        return NoContent();
    }

    [HttpPost("{id:guid}/refresh-token")]
    public async Task<IActionResult> RefreshToken(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var command = new RefreshTokenCommand(userId, id);
        var result = await _refreshTokenHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }
}

public class ConnectOAuth2Request
{
    public string Code { get; set; } = null!;
    public string? CodeVerifier { get; set; }
}

public class ConnectApiKeyRequest
{
    public string AccessToken { get; set; } = null!;
    public string? RefreshToken { get; set; }
}
