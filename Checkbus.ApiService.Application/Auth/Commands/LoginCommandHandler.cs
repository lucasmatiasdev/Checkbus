using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Audit;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Authentication;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.Auth.Commands
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginCommandResult>
    {
        private const int MaxFailedLoginAttempts = 3;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtGenerator _jwtGenerator;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtGenerator jwtGenerator,
            IAuditLogRepository auditLogRepository,
            ILogger<LoginCommandHandler> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtGenerator = jwtGenerator;
            _auditLogRepository = auditLogRepository;
            _logger = logger;
        }

        public async Task<LoginCommandResult> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);
            if (user is null)
            {
                _logger.LogWarning("LoginFailed with FailureReason {FailureReason}", "UserNotFound");

                // CU-16 (odd/tasks/auditoria-sistema.md S4): asunción — un email que no coincide
                // con ningún User no tiene una entidad real para referenciar, así que se audita
                // sin ActorUserId/OrganizationId, dejando solo el email que ingresó el intento.
                await WriteAuditLogAsync(
                    AuditAction.LoginFallido,
                    organizationId: null,
                    actorUserId: null,
                    actorEmail: request.Email,
                    entityId: Guid.Empty,
                    description: "Intento de inicio de sesión con un email no registrado",
                    cancellationToken);

                throw new UserNotFoundException();
            }

            if (!user.IsActive)
            {
                _logger.LogWarning(
                    "LoginFailed for UserId {UserId} in OrganizationId {OrganizationId} with FailureReason {FailureReason}",
                    user.Id, user.OrganizationId, "UserInactive");

                await WriteAuditLogAsync(
                    AuditAction.LoginFallido, user.OrganizationId, user.Id, user.Email,
                    user.Id, "Intento de inicio de sesión con un usuario inactivo", cancellationToken);

                throw new UserInactiveException();
            }

            if (user.Organization is { IsActive: false })
            {
                _logger.LogWarning(
                    "LoginFailed for UserId {UserId} in OrganizationId {OrganizationId} with FailureReason {FailureReason}",
                    user.Id, user.OrganizationId, "OrganizationInactive");

                await WriteAuditLogAsync(
                    AuditAction.LoginFallido, user.OrganizationId, user.Id, user.Email,
                    user.Id, "Intento de inicio de sesión con la organización inactiva", cancellationToken);

                throw new OrganizationInactiveException();
            }

            if (user.LockedUntil is { } lockedUntil && lockedUntil > DateTime.UtcNow)
            {
                _logger.LogWarning(
                    "LoginFailed for UserId {UserId} in OrganizationId {OrganizationId} with FailureReason {FailureReason}",
                    user.Id, user.OrganizationId, "AccountLocked");

                await WriteAuditLogAsync(
                    AuditAction.LoginFallido, user.OrganizationId, user.Id, user.Email,
                    user.Id, "Intento de inicio de sesión con la cuenta bloqueada", cancellationToken);

                throw new AccountLockedException(lockedUntil);
            }

            if (!_passwordHasher.Verify(user.PasswordHash, request.Password))
            {
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
                {
                    var lockUntil = DateTime.UtcNow + LockoutDuration;
                    user.LockedUntil = lockUntil;
                    await _userRepository.UpdateAsync(user, cancellationToken);

                    _logger.LogWarning(
                        "LoginFailed for UserId {UserId} in OrganizationId {OrganizationId} with FailureReason {FailureReason}",
                        user.Id, user.OrganizationId, "AccountLocked");

                    await WriteAuditLogAsync(
                        AuditAction.LoginFallido, user.OrganizationId, user.Id, user.Email,
                        user.Id, "Cuenta bloqueada tras superar el máximo de intentos fallidos de inicio de sesión",
                        cancellationToken);

                    throw new AccountLockedException(lockUntil);
                }

                await _userRepository.UpdateAsync(user, cancellationToken);

                _logger.LogWarning(
                    "LoginFailed for UserId {UserId} in OrganizationId {OrganizationId} with FailureReason {FailureReason}",
                    user.Id, user.OrganizationId, "InvalidCredentials");

                await WriteAuditLogAsync(
                    AuditAction.LoginFallido, user.OrganizationId, user.Id, user.Email,
                    user.Id, "Intento de inicio de sesión con contraseña incorrecta", cancellationToken);

                throw new InvalidCredentialsException();
            }

            var token = _jwtGenerator.GenerateToken(user);

            user.LastLoginAt = DateTime.UtcNow;
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation(
                "LoginSucceeded for UserId {UserId} in OrganizationId {OrganizationId} with Role {Role}",
                user.Id, user.OrganizationId, user.Role.ToString());

            await WriteAuditLogAsync(
                AuditAction.LoginExitoso, user.OrganizationId, user.Id, user.Email,
                user.Id, "Inicio de sesión exitoso", cancellationToken);

            return new LoginCommandResult
            {
                Token = token,
                UserId = user.Id,
                OrganizationId = user.OrganizationId,
                Role = user.Role.ToString(),
                Email = user.Email,
                MustChangePassword = user.MustChangePassword
            };
        }

        // CU-16: no bloqueante — si falla la escritura del AuditLogEntry, se loguea y el login
        // (éxito o fallo) sigue su curso normal (odd/tasks/auditoria-sistema.md S5).
        private async Task WriteAuditLogAsync(
            AuditAction action,
            Guid? organizationId,
            Guid? actorUserId,
            string actorEmail,
            Guid entityId,
            string description,
            CancellationToken cancellationToken)
        {
            try
            {
                await _auditLogRepository.AddAsync(new AuditLogEntry
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    ActorUserId = actorUserId,
                    ActorEmail = actorEmail,
                    Action = action,
                    EntityType = "User",
                    EntityId = entityId,
                    Description = description,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write audit log entry for {Action}", action);
            }
        }
    }
}
