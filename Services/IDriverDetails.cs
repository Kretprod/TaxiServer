using server.Models.Dtos;
using server.Models;

namespace server.Services
{
    public interface IDriverDetails
    {
        Task<(bool Success, string? ErrorMessage)> UpdateDriverDetailsAsync(int driverId, DriverDetailsDto dto);
        Task<(bool Success, string? ErrorMessage)> DelPhotoDriverDetailsAsync(int driverId, IWebHostEnvironment env);
        Task<(bool Success, string? ErrorMessage)> ChecStatusDriverDetailsAsync(int driverId);
        Task<DriverDetailsDtoFront?> GetDriverDetailsAsync(int driverId);
    }
}
