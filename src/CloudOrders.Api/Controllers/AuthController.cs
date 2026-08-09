using CloudOrders.Api.Contracts;
using CloudOrders.Application.Auth;
using Microsoft.AspNetCore.Mvc;

namespace CloudOrders.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly LoginHandler _loginHandler;

    public AuthController(LoginHandler loginHandler)
    {
        _loginHandler = loginHandler;
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login(LoginRequest loginRequest, CancellationToken cancellationToken)
    {
        var loginCommand = new LoginCommand(loginRequest.Email, loginRequest.Password);
        var token = await _loginHandler.HandleAsync(loginCommand, cancellationToken);

        if (token == null)
        {
            return Unauthorized();
        }

        return Ok(new { Token = token });
    }
}