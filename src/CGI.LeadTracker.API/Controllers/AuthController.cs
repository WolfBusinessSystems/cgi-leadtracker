using CGI.LeadTracker.API.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CGI.LeadTracker.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    /// <summary>POST /api/auth/login — retorna JWT</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Unauthorized(result.Errors.Select(e => e.Message));
    }
}
