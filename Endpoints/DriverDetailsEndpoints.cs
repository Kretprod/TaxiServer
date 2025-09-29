using Microsoft.AspNetCore.Authorization;
using server.Models.Dtos;
using server.Services;
using System.Security.Claims;
namespace server.Endpoints
{
    public static class DriverDetailsEndpoints
    {
        public static void MapDriverDetailsEndpoints(this WebApplication app)
        {
            app.MapPost("/api/drivers/details", [Authorize] async (HttpRequest request, IDriverDetails driverDetailsService, ClaimsPrincipal user, IWebHostEnvironment env) =>
            {
                var userIdClaim = user.FindFirst("id");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int driverId))
                {
                    return Results.Unauthorized();
                }

                if (!request.HasFormContentType)
                    return Results.BadRequest(new { error = "Неверный тип контента" });

                var (successss, errorrr) = await driverDetailsService.ChecStatusDriverDetailsAsync(driverId);
                if (!successss)
                    return Results.BadRequest(new { error = errorrr });

                var form = await request.ReadFormAsync();

                // Считываем текстовые поля
                var firstName = form["FirstName"].ToString();
                var lastName = form["LastName"].ToString();
                var carNumber = form["CarNumber"].ToString();
                var driverLicenseNumber = form["DriverLicenseNumber"].ToString();

                // Получаем файлы
                var carPhoto = form.Files["CarPhoto"];
                var driverLicensePhoto = form.Files["DriverLicensePhoto"];

                // Проверяем обязательные поля
                if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName)
                    || string.IsNullOrEmpty(carNumber) || string.IsNullOrEmpty(driverLicenseNumber)
                     || carPhoto == null || driverLicensePhoto == null)
                {
                    return Results.BadRequest(new { error = "Не все поля заполнены" });
                }

                var (successs, errorr) = await driverDetailsService.DelPhotoDriverDetailsAsync(driverId, env);

                // Сохраняем файлы на диск и получаем URL
                string SaveFile(IFormFile file)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                    // Если расширение пустое или недопустимое, устанавливаем дефолт (.jpg для фото)
                    if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                    {
                        extension = ".jpg"; // Дефолт для image/jpeg
                    }

                    if (!allowedExtensions.Contains(extension))
                        throw new InvalidOperationException("Недопустимый формат файла");

                    if (file.Length > 5 * 1024 * 1024) // 5 МБ лимит
                        throw new InvalidOperationException("Файл слишком большой");

                    var uploadsFolder = Path.Combine(env.WebRootPath, "uploads", "drivers", driverId.ToString());
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    file.CopyTo(stream);

                    return $"/uploads/drivers/{driverId}/{fileName}";
                }

                var carPhotoUrl = SaveFile(carPhoto);
                var driverLicensePhotoUrl = SaveFile(driverLicensePhoto);

                // Создаём DTO с URL
                var dto = new DriverDetailsDto
                {
                    FirstName = firstName,
                    LastName = lastName,
                    CarNumber = carNumber,
                    DriverLicenseNumber = driverLicenseNumber,
                    CarPhotoUrl = carPhotoUrl,
                    DriverLicensePhotoUrl = driverLicensePhotoUrl,
                };

                var (success, error) = await driverDetailsService.UpdateDriverDetailsAsync(driverId, dto);
                if (!success)
                    return Results.BadRequest(new { error });

                return Results.Ok(new { message = "Дополнительная информация успешно сохранена" });
            });

            app.MapGet("/api/drivers/detailsInfo", [Authorize] async (IDriverDetails driverDetailsService, ClaimsPrincipal user) =>
            {
                var userIdClaim = user.FindFirst("id");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int driverId))
                {
                    return Results.Unauthorized();
                }

                var details = await driverDetailsService.GetDriverDetailsAsync(driverId);
                if (details == null)
                {
                    return Results.NotFound(); // Или Results.Ok(null) — фронтенд обработает null
                }

                return Results.Ok(details);
            });

        }
    }
}
