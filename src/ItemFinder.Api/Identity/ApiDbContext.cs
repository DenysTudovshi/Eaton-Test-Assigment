using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ItemFinder.Api.Identity;

/// <summary>Users, roles, and tokens for the API; nothing item-related lives here.</summary>
public sealed class ApiDbContext(DbContextOptions<ApiDbContext> options)
    : IdentityDbContext<ApiUser>(options)
{
}