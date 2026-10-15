using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public sealed class AuthenticationService(
    RaceDayDbContext dbContext,
    IPasswordHasher<User> passwordHasher) : IAuthenticationService
{
    // Creates the account with the role the user selected on the registration form
    public Task<UserResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken) =>
        CreateUserAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            request.PhoneNumber,
            request.DateOfBirth,
            request.Role,
            cancellationToken);

    public Task<UserResponse> ProvisionOrganiserAsync(
        ProvisionOrganiserRequest request,
        CancellationToken cancellationToken) =>
        CreateUserAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            request.PhoneNumber,
            request.DateOfBirth,
            "Organiser",
            cancellationToken);

    public async Task<UserResponse?> ValidateCredentialsAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        if (user is null ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
            == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return ToResponse(user);
    }

    public async Task<UserResponse?> GetProfileAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        return user is null ? null : ToResponse(user);
    }

    public async Task<UserResponse?> UpdateProfileAsync(
        int userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber.Trim();
        user.DateOfBirth = request.DateOfBirth;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(user);
    }

    private async Task<UserResponse> CreateUserAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        string phoneNumber,
        DateTime dateOfBirth,
        string role,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (await dbContext.Users.AnyAsync(
                candidate => candidate.Email == normalizedEmail,
                cancellationToken))
        {
            throw new DuplicateEmailException();
        }

        var user = new User
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = phoneNumber.Trim(),
            DateOfBirth = dateOfBirth,
            Role = role
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(user);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static UserResponse ToResponse(User user) =>
        new(
            user.UserId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.DateOfBirth,
            user.Role);
}
