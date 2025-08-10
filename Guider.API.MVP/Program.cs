
using Guider.API.MVP;
using Guider.API.MVP.Data;
using Guider.API.MVP.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using System.Reflection;
using Microsoft.AspNetCore.Identity.UI.Services;
using Guider.API.MVP.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Guider.API.MVP.Filters;
using Guider.API.MVP.Utility;
using Microsoft.AspNetCore.Http.Features;

// Загружаем .env файлы перед созданием builder
EnvLoader.LoadEnvFiles();

var builder = WebApplication.CreateBuilder(args);

// Добавление конфигурации из переменных окружения
builder.Configuration.AddEnvironmentVariables();

// Диагностика конфигурации в режиме разработки
if (builder.Environment.IsDevelopment())
{
    Console.WriteLine("🔧 === DEVELOPMENT CONFIGURATION DEBUG ===");
    Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
    Console.WriteLine($"Content Root: {builder.Environment.ContentRootPath}");

    // Диагностика ключевых настроек из переменных окружения
    Console.WriteLine("\n🔍 Environment Variables Status:");

    // PostgreSQL диагностика
    var postgresEnv = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__POSTGRESQL");
    Console.WriteLine($"  PostgreSQL Connection: {(string.IsNullOrEmpty(postgresEnv) ? "❌ MISSING" : "✅ OK")}");

    // MongoDB диагностика
    var mongoConnectionEnv = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
        ?? Environment.GetEnvironmentVariable("MONGODBSETTINGS__CONNECTIONSTRING");
    var mongoDatabaseEnv = Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME")
        ?? Environment.GetEnvironmentVariable("MONGODBSETTINGS__DATABASENAME");
    Console.WriteLine($"  MongoDB Connection: {(string.IsNullOrEmpty(mongoConnectionEnv) ? "❌ MISSING" : "✅ OK")}");
    Console.WriteLine($"  MongoDB Database: {(string.IsNullOrEmpty(mongoDatabaseEnv) ? "❌ MISSING" : "✅ OK")}");

    // JWT диагностика
    var jwtSecretEnv = Environment.GetEnvironmentVariable("API_SECRET_KEY")
        ?? Environment.GetEnvironmentVariable("APISETTINGS__SECRET");
    Console.WriteLine($"  JWT Secret: {(string.IsNullOrEmpty(jwtSecretEnv) ? "❌ MISSING" : "✅ OK")}");
    Console.WriteLine("==========================================\n");
}

// Add services to the container.

// PostgreSQL - только из переменных окружения
var postgresConnection = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__POSTGRESQL");

if (string.IsNullOrEmpty(postgresConnection))
{
    throw new InvalidOperationException(
        "PostgreSQL connection string is not configured. " +
        "Please set CONNECTIONSTRINGS__POSTGRESQL environment variable.");
}

// Диагностика подключения к PostgreSQL (только в Development)
if (builder.Environment.IsDevelopment())
{
    var maskedConnection = postgresConnection.Length > 30
        ? $"{postgresConnection.Substring(0, 30)}..."
        : postgresConnection;
    Console.WriteLine($"✅ PostgreSQL Connection: {maskedConnection}");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(postgresConnection));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
});

builder.Services.Configure<IdentityOptions>(options =>
{
    // Убираем ограничения на UserName
    options.User.AllowedUserNameCharacters = null;
    options.User.RequireUniqueEmail = false;
    // Password settings
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
    options.Lockout.MaxFailedAccessAttempts = 20;
    options.Lockout.AllowedForNewUsers = true;
    // User settings
    options.User.RequireUniqueEmail = true;
});

// JWT настройки - только из переменных окружения
var jwtSecret = Environment.GetEnvironmentVariable("API_SECRET_KEY")
    ?? Environment.GetEnvironmentVariable("APISETTINGS__SECRET");

if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException(
        "JWT Secret is not configured. " +
        "Please set API_SECRET_KEY or APISETTINGS__SECRET environment variable.");
}

builder.Services.AddAuthentication(u =>
{
    u.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    u.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(u =>
{
    u.RequireHttpsMetadata = false;
    u.SaveToken = true;
    u.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddCors();

// MongoDB настройки - только из переменных окружения
builder.Services.Configure<Guider.API.MVP.Data.MongoDbSettings>(options =>
{
    // Только переменные окружения
    options.ConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
        ?? Environment.GetEnvironmentVariable("MONGODBSETTINGS__CONNECTIONSTRING");

    options.DatabaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME")
        ?? Environment.GetEnvironmentVariable("MONGODBSETTINGS__DATABASENAME");

    // Настройка коллекций из переменных окружения
    options.Collections = new Dictionary<string, string>();

    var places = Environment.GetEnvironmentVariable("MONGODB_PLACES_COLLECTION")
        ?? Environment.GetEnvironmentVariable("MONGODBSETTINGS__COLLECTIONS__PLACES");
    if (!string.IsNullOrEmpty(places))
        options.Collections["Places"] = places;

    var cities = Environment.GetEnvironmentVariable("MONGODB_CITIES_COLLECTION")
        ?? Environment.GetEnvironmentVariable("MONGODBSETTINGS__COLLECTIONS__CITIES");
    if (!string.IsNullOrEmpty(cities))
        options.Collections["Cities"] = cities;

    var provinces = Environment.GetEnvironmentVariable("MONGODB_PROVINCES_COLLECTION")
        ?? Environment.GetEnvironmentVariable("MONGODBSETTINGS__COLLECTIONS__PROVINCES");
    if (!string.IsNullOrEmpty(provinces))
        options.Collections["Provinces"] = provinces;

    var tags = Environment.GetEnvironmentVariable("MONGODB_TAGS_COLLECTION")
        ?? Environment.GetEnvironmentVariable("MONGODBSETTINGS__COLLECTIONS__TAGS");
    if (!string.IsNullOrEmpty(tags))
        options.Collections["Tags"] = tags;

    var images = Environment.GetEnvironmentVariable("MONGODB_IMAGES_COLLECTION")
        ?? Environment.GetEnvironmentVariable("MONGODBSETTINGS__COLLECTIONS__IMAGES");
    if (!string.IsNullOrEmpty(images))
        options.Collections["Images"] = images;

    if (string.IsNullOrEmpty(options.ConnectionString))
    {
        throw new InvalidOperationException(
            "MongoDB ConnectionString is not configured. " +
            "Please set MONGODB_CONNECTION_STRING or MONGODBSETTINGS__CONNECTIONSTRING environment variable.");
    }

    if (string.IsNullOrEmpty(options.DatabaseName))
    {
        throw new InvalidOperationException(
            "MongoDB DatabaseName is not configured. " +
            "Please set MONGODB_DATABASE_NAME or MONGODBSETTINGS__DATABASENAME environment variable.");
    }

    // Логирование для отладки (только в Development)
    if (builder.Environment.IsDevelopment())
    {
        var maskedConnection = options.ConnectionString.Length > 30
            ? $"{options.ConnectionString.Substring(0, 30)}..."
            : options.ConnectionString;
        Console.WriteLine($"✅ MongoDB Connection: {maskedConnection}");
        Console.WriteLine($"✅ MongoDB Database: {options.DatabaseName}");

        if (options.Collections != null && options.Collections.Any())
        {
            Console.WriteLine("✅ MongoDB Collections:");
            foreach (var collection in options.Collections)
            {
                Console.WriteLine($"   - {collection.Key}: {collection.Value}");
            }
        }
    }
});

// Регистрация сервисов MongoClient в DI-контейнере
builder.Services.AddSingleton<PlaceService>();
builder.Services.AddSingleton<ProvinceService>();
builder.Services.AddSingleton<CitiesService>();
builder.Services.AddSingleton<TagsService>();

// Регистрация сервиса для работы с изображениями
builder.Services.AddScoped<IImageService, ImageService>();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API MVP", Version = "v1" });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    c.IncludeXmlComments(xmlPath);
    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n" +
                      "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
                      "Example: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = JwtBearerDefaults.AuthenticationScheme
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                },
                Scheme = "oauth2",
                Name = JwtBearerDefaults.AuthenticationScheme,
                In = ParameterLocation.Header
            },
            new string[] {}
        }
    });
    c.OperationFilter<FileUploadOperationFilter>();
});


builder.Services.AddSingleton<IMinioService, MinioService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    Console.WriteLine("🚀 === APPLICATION STARTED ===");
    Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
    Console.WriteLine($"PostgreSQL configured: ✅");
    Console.WriteLine($"JWT configured: ✅");
    Console.WriteLine($"MongoDB configured: ✅");
    Console.WriteLine("===============================");
}

//app.UseHttpsRedirection();

// Включаем поддержку CORS для всех источников, методов и заголовков
//app.UseCors(builder =>
//{
//    builder.AllowAnyOrigin()
//           .AllowAnyMethod()
//           .AllowAnyHeader();
//});

app.UseCors(builder =>
{
    builder.WithOrigins(
        "https://api.guider.pro",      // Swagger UI
        "https://guider.pro",          // NextJS клиент
        "https://vip-test-2.guider.pro", // React Admin
        "http://localhost:3000"        // Локальная разработка
    )
    .AllowAnyMethod()
    .AllowAnyHeader();
});

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Выполняем миграции базы данных после полной конфигурации приложения
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Console.WriteLine("Проверка подключения к базе данных PostgreSQL...");
        if (db.Database.CanConnect())
        {
            Console.WriteLine("✅ Подключение к PostgreSQL успешно установлено");

            var pendingMigrations = db.Database.GetPendingMigrations();
            if (pendingMigrations.Any())
            {
                Console.WriteLine($"Найдено {pendingMigrations.Count()} неприменённых миграций:");
                foreach (var migration in pendingMigrations)
                {
                    Console.WriteLine($"  - {migration}");
                }

                Console.WriteLine("Выполняется автоматическая миграция базы данных...");
                db.Database.Migrate();
                Console.WriteLine("✅ Миграция завершена успешно");
            }
            else
            {
                Console.WriteLine("✅ База данных актуальна, миграции не требуются");
            }
        }
        else
        {
            Console.WriteLine("❌ Не удалось подключиться к базе данных PostgreSQL");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Ошибка при работе с базой данных: {ex.Message}");
    }
}

app.Run();
