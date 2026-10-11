using RaceDay.Api.Contracts;

namespace RaceDay.Api.Services;

public interface IAuthenticationService
{
    Task<UserResponse> RegisterParticipantAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<UserResponse> ProvisionOrganiserAsync(
        ProvisionOrganiserRequest request,
        CancellationToken cancellationToken);

    Task<UserResponse?> ValidateCredentialsAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<UserResponse?> GetProfileAsync(int userId, CancellationToken cancellationToken);

    Task<UserResponse?> UpdateProfileAsync(
        int userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken);
}
