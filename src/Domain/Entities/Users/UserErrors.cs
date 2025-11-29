using SharedKernel;

namespace Domain.Users;

public static class UserErrors
{
    public static Error NotFound(Guid userId) => Error.NotFound(
       "User.NotFound",
       $"The user with the identifier {userId} was not found");

    public static Error NotFoundByUserLogin(string userLogin) => Error.NotFound(
        "User.NotFoundByEmail",
        $"The user with the email {userLogin} was not found");

    public static Error Unauthorized() => Error.Forbidden(
        "User.Unauthorized",
        "The user is not authorized to perform this action");

    public static readonly Error EmailNotUnique = Error.Conflict(
        "User.EmailNotUnique",
        "The provided email is not unique");

    public static readonly Error InvalidCredentials = Error.Problem(
        "User.InvalidCredentials",
        "The provided credentials are invalid");

    public static Error InvalidCurrentPassword(string currentPassword) => Error.Failure(
        "Users.InvalidCurrentPassword",
        $"The provided current password '{currentPassword}' is invalid");


    public static Error AccountInactive() => Error.Failure(
        "Users.AccountInactive",
        "Your account is inactive. Please contact support for assistance.");

    public static Error InvalidRole() => Error.Failure(
        "Users.InvalidRole",
        "The provided role is invalid.");

    public static Error InvalidStatus() => Error.Failure(
        "Users.InvalidStatus",
        "The provided status is invalid.");

    public static Error NewPasswordSameAsCurrent() => Error.Failure(
        "Users.NewPasswordSameAsCurrent",
        "The new password is the same as the current password.");

    public static Error InvalidPermission(string permission) => Error.Failure(
        "Users.InvalidPermission",
        $"The provided permission '{permission}' is invalid.");

    public static Error UserLoginAlreadyExists(string userLogin) => Error.Conflict(
        "Users.UserLoginAlreadyExists",
        $"A user with login '{userLogin}' already exists");

    public static Error CannotDeleteSelf() => Error.Failure(
        "Users.CannotDeleteSelf",
        "You cannot delete your own account");

    public static Error HasAssociatedData(Guid userId) => Error.Failure(
        "Users.HasAssociatedData",
        $"Cannot delete user {userId} because they have associated data");
}
