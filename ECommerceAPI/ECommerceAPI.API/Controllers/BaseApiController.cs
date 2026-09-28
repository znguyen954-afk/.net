using ECommerceAPI.Application.Common;
using ECommerceAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerceAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected Guid CurrentUserId
    {
        get
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                     ?? User.FindFirst("sub");
            return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
        }
    }

    protected bool IsAdmin => User.IsInRole("Admin");

    protected IActionResult ApiOk<T>(ApiResponse<T> response) =>
        response.Success ? Ok(response) : BadRequest(response);

    protected IActionResult ApiCreated<T>(ApiResponse<T> response, string? location = null) =>
        response.Success ? Created(location ?? string.Empty, response) : BadRequest(response);
}
