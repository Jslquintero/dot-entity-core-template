using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Template.Api.Models;
using Template.Api.Services;
using Template.Data;
using Template.Model.Entities;

namespace Template.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(
    UserManager<User> users,
    SignInManager<User> signInManager,
    TokenService tokens,
    ApplicationDbContext context) : ControllerBase
{
    /// <summary>Authenticates an active user and returns a short-lived JWT.</summary>
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> LoginAsync(LoginViewModel model)
    {
        var user = await users.FindByEmailAsync(model.Email);
        if (user is null || !user.IsActive || user.TwoFactorEnabled)
            return Unauthorized();
        var result = await signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Unauthorized();
        return Ok(await tokens.CreateAsync(user));
    }

    /// <summary>Returns the authenticated user's profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType<UserViewModel>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserViewModel>> MeAsync()
    {
        var user = await users.GetUserAsync(User);
        if (user is null)
            return Unauthorized();
        return Ok(new UserViewModel(user.Id, user.Email ?? string.Empty, user.Name, user.LastName,
            await users.GetRolesAsync(user)));
    }

    /// <summary>Revokes all existing JWT sessions for the authenticated user.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAsync()
    {
        var user = await users.GetUserAsync(User);
        if (user is null)
            return Unauthorized();
        var result = await users.UpdateSecurityStampAsync(user);
        return result.Succeeded ? NoContent() : Problem("Unable to revoke sessions.");
    }

    /// <summary>Creates a regular user. Only system administrators may create accounts.</summary>
    [Authorize(Roles = "SysAdmin")]
    [HttpPost]
    [ProducesResponseType<UserViewModel>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserViewModel>> CreateAsync(CreateUserRequest request)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            Name = request.Name,
            LastName = request.LastName
        };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(error.Code, error.Description);
            return ValidationProblem(ModelState);
        }
        result = await users.AddToRoleAsync(user, "User");
        if (!result.Succeeded)
            return Problem("Unable to assign the user role.");
        await transaction.CommitAsync();
        return StatusCode(StatusCodes.Status201Created,
            new UserViewModel(user.Id, user.Email!, user.Name, user.LastName, ["User"]));
    }
}
