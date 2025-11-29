using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Users;
using Domain.Enums;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Users.SignUp;

internal sealed class SignUpCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IPasswordHasher passwordHasher)
    : ICommandHandler<SignUpCommand, ApiResponse<SignUpResponse>>
{
    public async Task<Result<ApiResponse<SignUpResponse>>> Handle(SignUpCommand command, CancellationToken cancellationToken)
    {
        // Check if user already exists
        bool userExists = await context.Users
            .AsNoTracking()
            .AnyAsync(u => u.UserLogin == command.UserLogin, cancellationToken);

        if (userExists)
        {
            return Result.Failure<ApiResponse<SignUpResponse>>(UserErrors.UserLoginAlreadyExists(command.UserLogin));
        }

        // Hash the password
        string passwordHash = passwordHasher.Hash(command.Password);

        // Create new user
        User user = new()
        {
            Id = Guid.NewGuid(),
            Username = command.Username,
            UserLogin = command.UserLogin,
            PasswordHash = passwordHash,
            Role = command.Role,
            IsActive = true,
            Status = UserStatus.Active,
            CreatedAt = dateTimeProvider.GetUtcNow(),
            LastLoginDate = dateTimeProvider.GetUtcNow(),
            OrganizationalUnitId = command.OrganizationalUnitId
        };

        // Add user to context
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new ApiResponse<SignUpResponse>
        {
            Data = new SignUpResponse
            {
                UserId = user.Id,
                UserLogin = user.UserLogin,
                Role = user.Role,
                CreatedDate = user.CreatedAt
            },
            Message = "User registered successfully",
            IsSuccess = true
        });
    }
}
