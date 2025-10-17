using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Services;
using server.Hubs;
using server.Endpoints;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5251");



// Читаем настройки JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings.GetValue<string>("SecretKey");
if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT SecretKey is not configured.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
        ValidAudience = jwtSettings.GetValue<string>("Audience"),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),

        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Читаем настройки JWT

// Регистрируем контекст базы данных с PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрируем SignalR для real-time коммуникаций
builder.Services.AddSignalR();
builder.Services.AddHttpClient();


// Регистрируем сервисы бизнес-логики
builder.Services.AddScoped<IRideService, RideService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDriverDetails, DriverDetailsService>();
builder.Services.AddScoped<IAdminn, AdminnService>();
builder.Services.AddScoped<ICena, CenaService>();


// регестрируем сервис для ежедневной проверки старой истории поездок и их удаление
builder.Services.AddHostedService<RideHistoryCleanupService>();


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

// Настройка сериализации JSON для минимальных API: enum в строки
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Добавляем логирование в консоль
builder.Logging.AddConsole();


var app = builder.Build();

// Применяем миграции EF Core при старте приложения
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        dbContext.Database.Migrate();  // Применит все pending миграции, создаст таблицы
        Console.WriteLine("Migrations applied successfully.");
    }
    catch (Exception ex)
    {
        // Логируем ошибку, если миграции не удались (например, проблемы с подключением)
        Console.WriteLine($"Migration failed: {ex.Message}");
        // В проде можно выбросить исключение или обработать иначе
    }
}


app.UseCors(); // Включаем CORSA

app.UseStaticFiles();

app.MapOrdersEndpoints();  // подключаем маршруты заказов
app.MapAuthEndpoints();    // подключаем маршруты регистрации
app.MapDriverDetailsEndpoints();
app.MapAdminnEndpoints();
app.MapCenaEndpoints();

// Подключаем SignalR хаб для real-time обновлений заказов
app.MapHub<OrdersHub>("/ordersHub");

app.Run();
