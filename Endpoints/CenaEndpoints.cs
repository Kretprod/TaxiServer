using Microsoft.AspNetCore.SignalR;
using server.Models.Dtos;
using server.Services;
using server.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace server.Endpoints
{
    public static class CenaEndpoints
    {
        public static void MapCenaEndpoints(this WebApplication app)
        {
            app.MapPost("/api/pricing/calculate", async (CenaDtos dto, ICena cenaService) =>
            {
                if (dto.distanceKm <= 0)
                    return Results.BadRequest("Расстояние должно быть положительным");

                var (isNight, isBadWeather) = await cenaService.GetConditionsAsync();
                var price = await cenaService.CalculatePriceAsync(dto.distanceKm, isNight, isBadWeather);

                var response = new CenaResponseDto
                {
                    Price = price,
                    IsNight = isNight,
                    IsBadWeather = isBadWeather
                };

                return Results.Ok(response);
            });



        }
    }
}
