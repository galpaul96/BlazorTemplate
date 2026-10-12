using Blazor.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Blazor.Web.Components.Admin;

public sealed class IdentityAdminService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ApplicationDbContext dbContext)
{
    private const int MaxPageSize = 50;

    private static readonly TimeSpan DisabledLockoutDuration = TimeSpan.FromDays(36500);

    public async Task<PagedResult<UserCatalogItem>> GetUsersAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? roleName = null,
        UserCatalogStatusFilter statusFilter = UserCatalogStatusFilter.All)
    {
        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var now = DateTimeOffset.UtcNow;
        var disabledThreshold = now.AddYears(50);

        var usersQuery = userManager.Users
            .AsNoTracking()
            .AsQueryable();

        var normalizedSearchTerm = searchTerm?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearchTerm))
        {
            var loweredSearchTerm = normalizedSearchTerm.ToLower();
            usersQuery = usersQuery.Where(user =>
                (user.Email != null && user.Email.ToLower().Contains(loweredSearchTerm)) ||
                (user.UserName != null && user.UserName.ToLower().Contains(loweredSearchTerm)) ||
                (user.PhoneNumber != null && user.PhoneNumber.ToLower().Contains(loweredSearchTerm)) ||
                user.Id.ToLower().Contains(loweredSearchTerm));
        }

        if (!string.IsNullOrWhiteSpace(roleName))
        {
            var role = await roleManager.FindByNameAsync(roleName.Trim());
            if (role is null)
            {
                return new PagedResult<UserCatalogItem>([], safePageNumber, safePageSize, 0);
            }

            usersQuery = usersQuery.Where(user =>
                dbContext.UserRoles.Any(userRole => userRole.UserId == user.Id && userRole.RoleId == role.Id));
        }

        usersQuery = statusFilter switch
        {
            UserCatalogStatusFilter.Active => usersQuery.Where(user =>
                (!user.LockoutEnd.HasValue || user.LockoutEnd <= now) && user.EmailConfirmed),
            UserCatalogStatusFilter.EmailConfirmed => usersQuery.Where(user => user.EmailConfirmed),
            UserCatalogStatusFilter.EmailPending => usersQuery.Where(user => !user.EmailConfirmed),
            UserCatalogStatusFilter.Locked => usersQuery.Where(user => user.LockoutEnd.HasValue && user.LockoutEnd > now),
            UserCatalogStatusFilter.Disabled => usersQuery.Where(user => user.LockoutEnd.HasValue && user.LockoutEnd > disabledThreshold),
            UserCatalogStatusFilter.LockoutDisabled => usersQuery.Where(user => !user.LockoutEnabled),
            UserCatalogStatusFilter.TwoFactorEnabled => usersQuery.Where(user => user.TwoFactorEnabled),
            UserCatalogStatusFilter.AccessFailures => usersQuery.Where(user => user.AccessFailedCount > 0),
            UserCatalogStatusFilter.NoRoles => usersQuery.Where(user =>
                !dbContext.UserRoles.Any(userRole => userRole.UserId == user.Id)),
            _ => usersQuery
        };

        usersQuery = usersQuery.OrderBy(user => user.Email ?? user.UserName ?? user.Id);

        var totalCount = await usersQuery.CountAsync();
        var users = await usersQuery
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync();

        var items = await CreateUserCatalogItemsAsync(users);
        return new PagedResult<UserCatalogItem>(items, safePageNumber, safePageSize, totalCount);
    }

    public async Task<UserCatalogItem?> GetUserAsync(string userId)
    {
        var users = await userManager.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .ToListAsync();

        return (await CreateUserCatalogItemsAsync(users)).SingleOrDefault();
    }

    private async Task<IReadOnlyList<UserCatalogItem>> CreateUserCatalogItemsAsync(IReadOnlyList<ApplicationUser> users)
    {
        var userIds = users.Select(user => user.Id).ToList();

        var roles = await (
                from userRole in dbContext.UserRoles.AsNoTracking()
                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userIds.Contains(userRole.UserId)
                select new
                {
                    userRole.UserId,
                    RoleName = role.Name ?? role.Id
                })
            .ToListAsync();

        var claims = await dbContext.UserClaims
            .AsNoTracking()
            .Where(claim => userIds.Contains(claim.UserId))
            .OrderBy(claim => claim.ClaimType)
            .ThenBy(claim => claim.ClaimValue)
            .Select(claim => new
            {
                claim.Id,
                claim.UserId,
                Type = claim.ClaimType ?? string.Empty,
                Value = claim.ClaimValue ?? string.Empty
            })
            .ToListAsync();

        var rolesByUser = roles
            .GroupBy(role => role.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .Select(role => role.RoleName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(roleName => roleName)
                    .ToList());

        var claimsByUser = claims
            .GroupBy(claim => claim.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<UserClaimSummary>)group
                    .Select(claim => new UserClaimSummary(claim.Id, claim.Type, claim.Value))
                    .ToList());

        var disabledThreshold = DateTimeOffset.UtcNow.AddYears(50);

        return users
            .Select(user => new UserCatalogItem(
                user.Id,
                user.UserName,
                user.Email,
                user.PhoneNumber,
                user.EmailConfirmed,
                user.PhoneNumberConfirmed,
                user.TwoFactorEnabled,
                user.LockoutEnabled,
                user.LockoutEnd,
                user.AccessFailedCount,
                user.LockoutEnd.HasValue && user.LockoutEnd.Value > disabledThreshold,
                rolesByUser.GetValueOrDefault(user.Id, []),
                claimsByUser.GetValueOrDefault(user.Id, [])))
            .ToList();
    }

    public async Task<IReadOnlyList<RoleSummary>> GetRolesAsync()
    {
        var roles = await roleManager.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .ToListAsync();

        var userCounts = await dbContext.UserRoles
            .AsNoTracking()
            .GroupBy(userRole => userRole.RoleId)
            .Select(group => new { RoleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.RoleId, item => item.Count);

        var claimCounts = await dbContext.RoleClaims
            .AsNoTracking()
            .GroupBy(claim => claim.RoleId)
            .Select(group => new { RoleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.RoleId, item => item.Count);

        return roles
            .Select(role => new RoleSummary(
                role.Id,
                role.Name ?? role.Id,
                userCounts.GetValueOrDefault(role.Id),
                claimCounts.GetValueOrDefault(role.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<UserOption>> GetUserOptionsAsync(int maxUsers = 200)
    {
        return await userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.Email ?? user.UserName ?? user.Id)
            .Take(Math.Clamp(maxUsers, 1, 500))
            .Select(user => new UserOption(user.Id, user.Email ?? user.UserName ?? user.Id))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ClaimAssignmentSummary>> GetClaimAssignmentsAsync()
    {
        var roleClaims = await (
                from claim in dbContext.RoleClaims.AsNoTracking()
                join role in dbContext.Roles.AsNoTracking() on claim.RoleId equals role.Id
                select new ClaimAssignmentSummary(
                    claim.Id,
                    IdentityClaimSubject.Role,
                    role.Id,
                    role.Name ?? role.Id,
                    claim.ClaimType ?? string.Empty,
                    claim.ClaimValue ?? string.Empty))
            .ToListAsync();

        var userClaims = await (
                from claim in dbContext.UserClaims.AsNoTracking()
                join user in dbContext.Users.AsNoTracking() on claim.UserId equals user.Id
                select new ClaimAssignmentSummary(
                    claim.Id,
                    IdentityClaimSubject.User,
                    user.Id,
                    user.Email ?? user.UserName ?? user.Id,
                    claim.ClaimType ?? string.Empty,
                    claim.ClaimValue ?? string.Empty))
            .ToListAsync();

        return roleClaims
            .Concat(userClaims)
            .OrderBy(claim => claim.Subject)
            .ThenBy(claim => claim.SubjectName)
            .ThenBy(claim => claim.Type)
            .ThenBy(claim => claim.Value)
            .ToList();
    }

    public async Task<AdminOperationResult> CreateRoleAsync(string? roleName)
    {
        var normalizedRoleName = NormalizeRequired(roleName, "Role name");
        if (normalizedRoleName is null)
        {
            return AdminOperationResult.Failure("Role name is required.");
        }

        var existingRole = await roleManager.FindByNameAsync(normalizedRoleName);
        if (existingRole is not null)
        {
            return AdminOperationResult.Failure($"Role '{normalizedRoleName}' already exists.");
        }

        var result = await roleManager.CreateAsync(new IdentityRole(normalizedRoleName));
        return FromIdentityResult(result, $"Role '{normalizedRoleName}' created.");
    }

    public async Task<AdminOperationResult> UpdateRoleAsync(string roleId, string? roleName)
    {
        var normalizedRoleName = NormalizeRequired(roleName, "Role name");
        if (normalizedRoleName is null)
        {
            return AdminOperationResult.Failure("Role name is required.");
        }

        var role = await roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            return AdminOperationResult.Failure("Role was not found.");
        }

        var existingRole = await roleManager.FindByNameAsync(normalizedRoleName);
        if (existingRole is not null && existingRole.Id != role.Id)
        {
            return AdminOperationResult.Failure($"Role '{normalizedRoleName}' already exists.");
        }

        role.Name = normalizedRoleName;
        var result = await roleManager.UpdateAsync(role);
        return FromIdentityResult(result, $"Role '{normalizedRoleName}' updated.");
    }

    public async Task<AdminOperationResult> DeleteRoleAsync(string roleId)
    {
        var role = await roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            return AdminOperationResult.Failure("Role was not found.");
        }

        var roleName = role.Name ?? role.Id;
        var result = await roleManager.DeleteAsync(role);
        return FromIdentityResult(result, $"Role '{roleName}' removed.");
    }

    public async Task<AdminOperationResult> AssignRoleToUserAsync(string userId, string? roleName)
    {
        var normalizedRoleName = NormalizeRequired(roleName, "Role");
        if (normalizedRoleName is null)
        {
            return AdminOperationResult.Failure("Select a role to assign.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return AdminOperationResult.Failure("User was not found.");
        }

        if (!await roleManager.RoleExistsAsync(normalizedRoleName))
        {
            return AdminOperationResult.Failure($"Role '{normalizedRoleName}' does not exist.");
        }

        if (await userManager.IsInRoleAsync(user, normalizedRoleName))
        {
            return AdminOperationResult.Success($"{DisplayUser(user)} already has role '{normalizedRoleName}'.");
        }

        var result = await userManager.AddToRoleAsync(user, normalizedRoleName);
        return FromIdentityResult(result, $"Assigned role '{normalizedRoleName}' to {DisplayUser(user)}.");
    }

    public async Task<AdminOperationResult> RemoveRoleFromUserAsync(string userId, string roleName)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return AdminOperationResult.Failure("User was not found.");
        }

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            return AdminOperationResult.Success($"{DisplayUser(user)} does not have role '{roleName}'.");
        }

        var result = await userManager.RemoveFromRoleAsync(user, roleName);
        return FromIdentityResult(result, $"Removed role '{roleName}' from {DisplayUser(user)}.");
    }

    public async Task<AdminOperationResult> UpdateUserProfileAsync(
        string userId,
        string? userName,
        string? email,
        bool emailConfirmed,
        string? phoneNumber,
        bool phoneNumberConfirmed)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return AdminOperationResult.Failure("User was not found.");
        }

        var normalizedUserName = NormalizeRequired(userName, "User name");
        if (normalizedUserName is null)
        {
            return AdminOperationResult.Failure("User name is required.");
        }

        var normalizedEmail = NormalizeRequired(email, "Email");
        user.UserName = normalizedUserName;
        user.Email = normalizedEmail;
        user.EmailConfirmed = normalizedEmail is not null && emailConfirmed;
        user.PhoneNumber = NormalizeOptional(phoneNumber);
        user.PhoneNumberConfirmed = user.PhoneNumber is not null && phoneNumberConfirmed;

        var result = await userManager.UpdateAsync(user);
        return FromIdentityResult(result, $"Updated {DisplayUser(user)}.");
    }

    public async Task<AdminOperationResult> LockUserAsync(string userId, int days)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return AdminOperationResult.Failure("User was not found.");
        }

        var safeDays = Math.Clamp(days, 1, 3650);
        var enableLockoutResult = await userManager.SetLockoutEnabledAsync(user, true);
        if (!enableLockoutResult.Succeeded)
        {
            return FromIdentityResult(enableLockoutResult, string.Empty);
        }

        var lockoutEnd = DateTimeOffset.UtcNow.AddDays(safeDays);
        var result = await userManager.SetLockoutEndDateAsync(user, lockoutEnd);
        return FromIdentityResult(result, $"Locked {DisplayUser(user)} until {lockoutEnd.LocalDateTime:g}.");
    }

    public async Task<AdminOperationResult> DisableUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return AdminOperationResult.Failure("User was not found.");
        }

        var enableLockoutResult = await userManager.SetLockoutEnabledAsync(user, true);
        if (!enableLockoutResult.Succeeded)
        {
            return FromIdentityResult(enableLockoutResult, string.Empty);
        }

        var lockoutEnd = DateTimeOffset.UtcNow.Add(DisabledLockoutDuration);
        var result = await userManager.SetLockoutEndDateAsync(user, lockoutEnd);
        return FromIdentityResult(result, $"Disabled {DisplayUser(user)}.");
    }

    public async Task<AdminOperationResult> EnableUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return AdminOperationResult.Failure("User was not found.");
        }

        var enableLockoutResult = await userManager.SetLockoutEnabledAsync(user, true);
        if (!enableLockoutResult.Succeeded)
        {
            return FromIdentityResult(enableLockoutResult, string.Empty);
        }

        var unlockResult = await userManager.SetLockoutEndDateAsync(user, null);
        if (!unlockResult.Succeeded)
        {
            return FromIdentityResult(unlockResult, string.Empty);
        }

        var resetFailuresResult = await userManager.ResetAccessFailedCountAsync(user);
        return FromIdentityResult(resetFailuresResult, $"Enabled {DisplayUser(user)}.");
    }

    public async Task<AdminOperationResult> SetUserLockoutEnabledAsync(string userId, bool enabled)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return AdminOperationResult.Failure("User was not found.");
        }

        var result = await userManager.SetLockoutEnabledAsync(user, enabled);
        if (!result.Succeeded)
        {
            return FromIdentityResult(result, string.Empty);
        }

        if (!enabled)
        {
            var unlockResult = await userManager.SetLockoutEndDateAsync(user, null);
            if (!unlockResult.Succeeded)
            {
                return FromIdentityResult(unlockResult, string.Empty);
            }
        }

        return AdminOperationResult.Success(enabled
            ? $"Enabled lockout protection for {DisplayUser(user)}."
            : $"Disabled lockout protection for {DisplayUser(user)}.");
    }

    public async Task<AdminOperationResult> ResetUserAccessFailuresAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return AdminOperationResult.Failure("User was not found.");
        }

        var result = await userManager.ResetAccessFailedCountAsync(user);
        return FromIdentityResult(result, $"Reset failed access count for {DisplayUser(user)}.");
    }

    public async Task<AdminOperationResult> SetTemporaryPasswordAsync(string userId, string? temporaryPassword)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return AdminOperationResult.Failure("User was not found.");
        }

        var normalizedPassword = NormalizeRequired(temporaryPassword, "Temporary password");
        if (normalizedPassword is null)
        {
            return AdminOperationResult.Failure("Temporary password is required.");
        }

        var result = await userManager.HasPasswordAsync(user)
            ? await userManager.ResetPasswordAsync(user, await userManager.GeneratePasswordResetTokenAsync(user), normalizedPassword)
            : await userManager.AddPasswordAsync(user, normalizedPassword);

        return FromIdentityResult(result, $"Set a temporary password for {DisplayUser(user)}.");
    }

    public async Task<AdminOperationResult> ClearUserPhoneAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return AdminOperationResult.Failure("User was not found.");
        }

        user.PhoneNumber = null;
        user.PhoneNumberConfirmed = false;

        var result = await userManager.UpdateAsync(user);
        return FromIdentityResult(result, $"Cleared phone details for {DisplayUser(user)}.");
    }

    public async Task<AdminOperationResult> ResetUserAuthenticatorAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return AdminOperationResult.Failure("User was not found.");
        }

        var disableTwoFactorResult = await userManager.SetTwoFactorEnabledAsync(user, false);
        if (!disableTwoFactorResult.Succeeded)
        {
            return FromIdentityResult(disableTwoFactorResult, string.Empty);
        }

        var resetAuthenticatorResult = await userManager.ResetAuthenticatorKeyAsync(user);
        return FromIdentityResult(resetAuthenticatorResult, $"Reset authenticator settings for {DisplayUser(user)}.");
    }

    public async Task<AdminOperationResult> RevokeUserSessionsAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return AdminOperationResult.Failure("User was not found.");
        }

        var result = await userManager.UpdateSecurityStampAsync(user);
        return FromIdentityResult(result, $"Revoked active sessions for {DisplayUser(user)}.");
    }

    public async Task<AdminOperationResult> AddUserClaimAsync(string userId, string? claimType, string? claimValue)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return AdminOperationResult.Failure("User was not found.");
        }

        var type = NormalizeRequired(claimType, "Claim type");
        if (type is null)
        {
            return AdminOperationResult.Failure("Claim type is required.");
        }

        var value = NormalizeClaimValue(claimValue);
        var exists = await dbContext.UserClaims.AnyAsync(claim =>
            claim.UserId == user.Id &&
            claim.ClaimType == type &&
            claim.ClaimValue == value);

        if (exists)
        {
            return AdminOperationResult.Failure($"{DisplayUser(user)} already has that claim.");
        }

        dbContext.UserClaims.Add(new IdentityUserClaim<string>
        {
            UserId = user.Id,
            ClaimType = type,
            ClaimValue = value
        });

        await dbContext.SaveChangesAsync();
        return AdminOperationResult.Success($"Assigned claim '{type}' to {DisplayUser(user)}.");
    }

    public async Task<AdminOperationResult> RemoveUserClaimAsync(int claimId)
    {
        var claim = await dbContext.UserClaims.FindAsync(claimId);
        if (claim is null)
        {
            return AdminOperationResult.Failure("User claim was not found.");
        }

        dbContext.UserClaims.Remove(claim);
        await dbContext.SaveChangesAsync();
        return AdminOperationResult.Success("User claim removed.");
    }

    public async Task<AdminOperationResult> CreateClaimAsync(
        IdentityClaimSubject subject,
        string? subjectId,
        string? claimType,
        string? claimValue)
    {
        return subject == IdentityClaimSubject.Role
            ? await AddRoleClaimAsync(subjectId, claimType, claimValue)
            : await AddUserClaimAsync(subjectId ?? string.Empty, claimType, claimValue);
    }

    public async Task<AdminOperationResult> UpdateClaimAsync(
        IdentityClaimSubject subject,
        int claimId,
        string? claimType,
        string? claimValue)
    {
        return subject == IdentityClaimSubject.Role
            ? await UpdateRoleClaimAsync(claimId, claimType, claimValue)
            : await UpdateUserClaimAsync(claimId, claimType, claimValue);
    }

    public async Task<AdminOperationResult> RemoveClaimAsync(IdentityClaimSubject subject, int claimId)
    {
        return subject == IdentityClaimSubject.Role
            ? await RemoveRoleClaimAsync(claimId)
            : await RemoveUserClaimAsync(claimId);
    }

    private async Task<AdminOperationResult> AddRoleClaimAsync(string? roleId, string? claimType, string? claimValue)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return AdminOperationResult.Failure("Select a role for the claim.");
        }

        var role = await roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            return AdminOperationResult.Failure("Role was not found.");
        }

        var type = NormalizeRequired(claimType, "Claim type");
        if (type is null)
        {
            return AdminOperationResult.Failure("Claim type is required.");
        }

        var value = NormalizeClaimValue(claimValue);
        var exists = await dbContext.RoleClaims.AnyAsync(claim =>
            claim.RoleId == role.Id &&
            claim.ClaimType == type &&
            claim.ClaimValue == value);

        if (exists)
        {
            return AdminOperationResult.Failure($"Role '{role.Name}' already has that claim.");
        }

        dbContext.RoleClaims.Add(new IdentityRoleClaim<string>
        {
            RoleId = role.Id,
            ClaimType = type,
            ClaimValue = value
        });

        await dbContext.SaveChangesAsync();
        return AdminOperationResult.Success($"Assigned claim '{type}' to role '{role.Name ?? role.Id}'.");
    }

    private async Task<AdminOperationResult> UpdateUserClaimAsync(int claimId, string? claimType, string? claimValue)
    {
        var claim = await dbContext.UserClaims.FindAsync(claimId);
        if (claim is null)
        {
            return AdminOperationResult.Failure("User claim was not found.");
        }

        var type = NormalizeRequired(claimType, "Claim type");
        if (type is null)
        {
            return AdminOperationResult.Failure("Claim type is required.");
        }

        var value = NormalizeClaimValue(claimValue);
        var duplicateExists = await dbContext.UserClaims.AnyAsync(otherClaim =>
            otherClaim.Id != claim.Id &&
            otherClaim.UserId == claim.UserId &&
            otherClaim.ClaimType == type &&
            otherClaim.ClaimValue == value);

        if (duplicateExists)
        {
            return AdminOperationResult.Failure("That user already has the updated claim.");
        }

        claim.ClaimType = type;
        claim.ClaimValue = value;
        await dbContext.SaveChangesAsync();
        return AdminOperationResult.Success("User claim updated.");
    }

    private async Task<AdminOperationResult> UpdateRoleClaimAsync(int claimId, string? claimType, string? claimValue)
    {
        var claim = await dbContext.RoleClaims.FindAsync(claimId);
        if (claim is null)
        {
            return AdminOperationResult.Failure("Role claim was not found.");
        }

        var type = NormalizeRequired(claimType, "Claim type");
        if (type is null)
        {
            return AdminOperationResult.Failure("Claim type is required.");
        }

        var value = NormalizeClaimValue(claimValue);
        var duplicateExists = await dbContext.RoleClaims.AnyAsync(otherClaim =>
            otherClaim.Id != claim.Id &&
            otherClaim.RoleId == claim.RoleId &&
            otherClaim.ClaimType == type &&
            otherClaim.ClaimValue == value);

        if (duplicateExists)
        {
            return AdminOperationResult.Failure("That role already has the updated claim.");
        }

        claim.ClaimType = type;
        claim.ClaimValue = value;
        await dbContext.SaveChangesAsync();
        return AdminOperationResult.Success("Role claim updated.");
    }

    private async Task<AdminOperationResult> RemoveRoleClaimAsync(int claimId)
    {
        var claim = await dbContext.RoleClaims.FindAsync(claimId);
        if (claim is null)
        {
            return AdminOperationResult.Failure("Role claim was not found.");
        }

        dbContext.RoleClaims.Remove(claim);
        await dbContext.SaveChangesAsync();
        return AdminOperationResult.Success("Role claim removed.");
    }

    private static string? NormalizeRequired(string? value, string fieldName)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue) ? null : normalizedValue;
    }

    private static string NormalizeClaimValue(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalizedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(normalizedValue) ? null : normalizedValue;
    }

    private static AdminOperationResult FromIdentityResult(IdentityResult result, string successMessage)
    {
        if (result.Succeeded)
        {
            return AdminOperationResult.Success(successMessage);
        }

        var errors = string.Join(" ", result.Errors.Select(error => error.Description));
        return AdminOperationResult.Failure(string.IsNullOrWhiteSpace(errors) ? "Identity operation failed." : errors);
    }

    private static string DisplayUser(ApplicationUser user)
    {
        return user.Email ?? user.UserName ?? user.Id;
    }
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record UserCatalogItem(
    string Id,
    string? UserName,
    string? Email,
    string? PhoneNumber,
    bool EmailConfirmed,
    bool PhoneNumberConfirmed,
    bool TwoFactorEnabled,
    bool LockoutEnabled,
    DateTimeOffset? LockoutEnd,
    int AccessFailedCount,
    bool IsDisabled,
    IReadOnlyList<string> Roles,
    IReadOnlyList<UserClaimSummary> Claims);

public sealed record UserClaimSummary(int Id, string Type, string Value);

public sealed record RoleSummary(string Id, string Name, int UserCount, int ClaimCount);

public sealed record UserOption(string Id, string DisplayName);

public enum UserCatalogStatusFilter
{
    All,
    Active,
    EmailConfirmed,
    EmailPending,
    Locked,
    Disabled,
    LockoutDisabled,
    TwoFactorEnabled,
    AccessFailures,
    NoRoles
}

public enum IdentityClaimSubject
{
    Role,
    User
}

public sealed record ClaimAssignmentSummary(
    int Id,
    IdentityClaimSubject Subject,
    string SubjectId,
    string SubjectName,
    string Type,
    string Value);

public sealed record AdminOperationResult(bool Succeeded, string Message)
{
    public static AdminOperationResult Success(string message)
    {
        return new AdminOperationResult(true, message);
    }

    public static AdminOperationResult Failure(string message)
    {
        return new AdminOperationResult(false, message);
    }
}
