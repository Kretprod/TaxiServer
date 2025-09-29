using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using server.Data;
using server.Models;
using server.Models.Dtos;

namespace server.Services
{
    public class DriverDetailsService : IDriverDetails
    {
        private readonly AppDbContext _db;

        public DriverDetailsService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateDriverDetailsAsync(int driverId, DriverDetailsDto dto)
        {
            try
            {
                var driver = await _db.Drivers.FindAsync(driverId);
                if (driver == null)
                    return (false, "Водитель не найден");

                var details = await _db.DriverDetails.FindAsync(driverId);

                if (details == null)
                {
                    details = new DriverDetails
                    {
                        DriverId = driverId,
                        FirstName = dto.FirstName,
                        LastName = dto.LastName,
                        CarNumber = dto.CarNumber,
                        DriverLicenseNumber = dto.DriverLicenseNumber,
                        CarPhotoUrl = dto.CarPhotoUrl,
                        DriverLicensePhotoUrl = dto.DriverLicensePhotoUrl,
                        Status = DriverStatus.НаПроверке,
                    };
                    _db.DriverDetails.Add(details);
                }
                else
                {
                    details.FirstName = dto.FirstName;
                    details.LastName = dto.LastName;
                    details.CarNumber = dto.CarNumber;
                    details.DriverLicenseNumber = dto.DriverLicenseNumber;
                    details.CarPhotoUrl = dto.CarPhotoUrl;
                    details.DriverLicensePhotoUrl = dto.DriverLicensePhotoUrl;
                    details.Status = DriverStatus.НаПроверке;
                }

                await _db.SaveChangesAsync();
                return (true, null);
            }
            catch
            {
                return (false, "Внутренняя ошибка сервера");
            }
        }
        public async Task<(bool Success, string? ErrorMessage)> ChecStatusDriverDetailsAsync(int driverId)
        {
            var details = await _db.DriverDetails.FindAsync(driverId);
            try
            {
                if (details != null && (details.Status == DriverStatus.Активен || details.Status == DriverStatus.НаПроверке))
                {
                    return (false, "Вы уже отправили дополнительную информацию");
                }
                return (true, null);
            }
            catch
            {
                return (false, "Внутренняя ошибка сервера");
            }
        }
        public async Task<(bool Success, string? ErrorMessage)> DelPhotoDriverDetailsAsync(int driverId, IWebHostEnvironment env)
        {
            void DeleteFile(string url)
            {
                if (!string.IsNullOrEmpty(url))
                {
                    var filePath = Path.Combine(env.WebRootPath, url.TrimStart('/'));
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
            try
            {
                var currentDetails = await _db.DriverDetails.FindAsync(driverId);
                if (currentDetails != null)
                {
                    // Удаляем старые файлы перед сохранением новых
                    DeleteFile(currentDetails.CarPhotoUrl);
                    DeleteFile(currentDetails.DriverLicensePhotoUrl);
                }
                return (true, null);
            }
            catch
            {
                return (false, "Внутренняя ошибка сервера");
            }
        }


        public async Task<DriverDetailsDtoFront?> GetDriverDetailsAsync(int driverId)
        {
            try
            {
                var details = await _db.DriverDetails.FindAsync(driverId);
                if (details == null)
                    return null;

                return new DriverDetailsDtoFront
                {
                    FirstName = details.FirstName,
                    LastName = details.LastName,
                    CarNumber = details.CarNumber,
                    DriverLicenseNumber = details.DriverLicenseNumber,
                    Status = details.Status
                };
            }
            catch
            {
                return null; // Или бросить исключение, но для простоты null
            }
        }

    }
}
