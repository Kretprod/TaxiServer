using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Services;
using server.Hubs;
using server.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5251");

// Регистрируем контекст базы данных с PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрируем SignalR для real-time коммуникаций
builder.Services.AddSignalR();

// Регистрируем сервис для отправки email (EmailService)
builder.Services.AddScoped<EmailService>();

// Настройка CORS — разрешаем любые заголовки, методы и источники (для разработки)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin();
    });
});

// Добавляем логирование в консоль
builder.Logging.AddConsole();

var app = builder.Build();

app.UseCors(); // Включаем CORS

app.MapOrdersEndpoints();  // подключаем маршруты заказов
app.MapAuthEndpoints();    // подключаем маршруты регистрации

// Подключаем SignalR хаб для real-time обновлений заказов
app.MapHub<OrdersHub>("/ordersHub");

app.Run();
