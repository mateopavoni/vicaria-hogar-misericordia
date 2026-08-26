using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.Auth;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterDto> _validator;
    private readonly IValidator<ApproveUserDto> _approveValidator;
    private readonly IValidator<RejectUserDto> _rejectValidator;
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly IValidator<RefreshTokenDto> _refreshValidator;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterDto> validator,
        IValidator<ApproveUserDto> approveValidator,
        IValidator<RejectUserDto> rejectValidator,
        IValidator<LoginDto> loginValidator,
        IValidator<RefreshTokenDto> refreshValidator)
    {
        _authService = authService;
        _validator = validator;
        _approveValidator = approveValidator;
        _rejectValidator = rejectValidator;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _authService.RegisterAsync(dto, cancellationToken);
        if (!result.Success)
        {
            return Conflict(new { message = result.ErrorMessage });
        }

        return CreatedAtAction(nameof(Register), new { id = result.UsuarioId }, new { id = result.UsuarioId });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _loginValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _authService.LoginAsync(dto, cancellationToken);
        if (!result.Success)
        {
            if (result.Error == LoginError.AccountNotApproved)
            {
                return StatusCode(403, new { estado = result.Estado, message = result.ErrorMessage });
            }
            // mismo shape que AccountNotApproved (estado = "Bloqueada") para que el frontend lo reconozca igual
            if (result.Error == LoginError.AccountLocked)
            {
                return StatusCode(403, new { estado = result.Estado, message = result.ErrorMessage, lockoutEnd = result.LockoutEnd });
            }
            return Unauthorized(new { message = result.ErrorMessage });
        }

        return Ok(new
        {
            token = result.Token,
            refreshToken = result.RefreshToken,
            user = new
            {
                id = result.UsuarioId,
                nombre = result.Nombre,
                apellido = result.Apellido,
                email = result.Email,
                rol = result.Rol
            }
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _refreshValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _authService.RefreshTokenAsync(dto, cancellationToken);
        if (!result.Success)
        {
            return Unauthorized(new { message = result.ErrorMessage });
        }

        return Ok(new { token = result.Token, refreshToken = result.RefreshToken });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(ActorId, cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            nombre = User.FindFirstValue(ClaimTypes.Name),
            email = User.FindFirstValue(ClaimTypes.Email),
            rol = User.FindFirstValue(ClaimTypes.Role)
        });
    }

    [HttpGet("users/pending")]
    [Authorize(Roles = RolNombres.Referente)]
    public async Task<IActionResult> GetPendingUsers(CancellationToken cancellationToken)
    {
        var usuarios = await _authService.GetPendingUsersAsync(cancellationToken);
        return Ok(usuarios);
    }

    [HttpPost("users/{id}/approve")]
    [Authorize(Roles = RolNombres.Referente)]
    public async Task<IActionResult> ApproveUser(Guid id, [FromBody] ApproveUserDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _approveValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _authService.ApproveUserAsync(id, dto, ActorId, cancellationToken);
        return result.Error switch
        {
            null => NoContent(),
            ApproveUserError.UserNotFound => NotFound(new { message = result.ErrorMessage }),
            ApproveUserError.InvalidRole => BadRequest(new { message = result.ErrorMessage }),
            _ => Conflict(new { message = result.ErrorMessage })
        };
    }

    [HttpPost("users/{id}/reject")]
    [Authorize(Roles = RolNombres.Referente)]
    public async Task<IActionResult> RejectUser(Guid id, [FromBody] RejectUserDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _rejectValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _authService.RejectUserAsync(id, dto, ActorId, cancellationToken);
        return result.Error switch
        {
            null => NoContent(),
            RejectUserError.UserNotFound => NotFound(new { message = result.ErrorMessage }),
            _ => Conflict(new { message = result.ErrorMessage })
        };
    }

    [HttpPatch("users/{id}/deactivate")]
    [Authorize(Roles = RolNombres.Referente)]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _authService.DeactivateUserAsync(id, ActorId, cancellationToken);
        return result.Error switch
        {
            null => NoContent(),
            UserStatusError.UserNotFound => NotFound(new { message = result.ErrorMessage }),
            _ => Conflict(new { message = result.ErrorMessage })
        };
    }

    [HttpPatch("users/{id}/reactivate")]
    [Authorize(Roles = RolNombres.Referente)]
    public async Task<IActionResult> ReactivateUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _authService.ReactivateUserAsync(id, ActorId, cancellationToken);
        return result.Error switch
        {
            null => NoContent(),
            UserStatusError.UserNotFound => NotFound(new { message = result.ErrorMessage }),
            _ => Conflict(new { message = result.ErrorMessage })
        };
    }
}
