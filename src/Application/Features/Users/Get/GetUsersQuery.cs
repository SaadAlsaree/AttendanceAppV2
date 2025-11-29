using Application.Abstractions.Messaging;
using Domain.Enums;
using SharedKernel;

namespace Application.Features.Users.Get;

public sealed record GetUsersQuery(
    int Page = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    Role? Role = null,
    UserStatus? Status = null,
    bool? IsActive = null,
    string? SortBy = null) : IQuery<PaginatedResponse<UserResponse>>;
