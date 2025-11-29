using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Users;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Users.Login;

internal sealed class LoginCommandHandler(
   IApplicationDbContext context,
   IDateTimeProvider dateTimeProvider,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider
    )
    : ICommandHandler<LoginCommand, ApiResponse<LoginResponse>>
{
    public async Task<Result<ApiResponse<LoginResponse>>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {


        // Store the value in a local variable to ensure proper parameter binding
        string userLogin = command.UserLogin;


        // Try alternative query approach with explicit parameter
        User? user = await context.Users
            .AsNoTracking()
            .Where(u => u.UserLogin == userLogin)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Result.Failure<ApiResponse<LoginResponse>>(UserErrors.NotFoundByUserLogin(command.UserLogin));
        }

        // Debug logging
        Console.WriteLine($"LoginCommandHandler - UserLogin: '{command.UserLogin}', Password: '{command.Password}'");

        bool verified = passwordHasher.Verify(command.Password, user.PasswordHash);
        Console.WriteLine($"LoginCommandHandler - Verified: '{verified}'");

        if (!verified)
        {
            return Result.Failure<ApiResponse<LoginResponse>>(UserErrors.InvalidCredentials);
        }


        // Generate JWT token
        string token = tokenProvider.Create(user);

        // Update last login date
        user.LastLoginDate = dateTimeProvider.GetUtcNow();
        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new ApiResponse<LoginResponse>
        {
            Data = new LoginResponse
            {
                Token = token,
                UserId = user.Id,
                LastLoginDate = user.LastLoginDate
            },
            Message = "Login successful",
            IsSuccess = true
        });
    }
}
