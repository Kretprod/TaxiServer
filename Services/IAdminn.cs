using server.Models.Dtos;
using server.Models;

namespace server.Services
{
    public interface IAdminn
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task<List<DriverPendingDto>> GetPendingDriversAsync();
        Task<List<DriverActiveDto>> GetActiveDriversAsync();
        Task UpdateDriverStatusAsync(int driverId, DriverStatus newStatus);
        Task<PricingSettings> GetPricingSettingsAsync();
        Task UpdatePricingSettingsAsync(UpdatePricingSettingsDto dto);
    }
}
