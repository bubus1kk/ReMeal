using Application.DTOs.Users;

namespace Application.Interfaces
{
    public interface IUserProfileService
    {
        Task<UserProfileDto?> GetCurrentProfileAsync(CancellationToken cancellationToken = default);

        Task<UserProfileDto?> UpdateCurrentProfileAsync(UpdateUserProfileRequest request, CancellationToken cancellationToken = default);
    }
}
