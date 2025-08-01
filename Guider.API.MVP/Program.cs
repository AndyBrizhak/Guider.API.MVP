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

// Диагностика конфигурации
if (builder.Environment.IsDevelopment())
{
    Console.WriteLine("🔧 === DEVELOPMENT CONFIGURATION DEBUG ===");
    Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
    Console.WriteLine($"Content Root: {builder.Environment.ContentRootPath}");

    // Показываем источники конфигурации
    Console.WriteLine("\n📚 Configuration Sources:");
    foreach (var source in builder.Configuration.Sources)
    {
        Console.WriteLine($"  - {source.GetType().Name}");
    }

    // Диагностика ключевых настроек
    var allSources = new Dictionary<string, string>
    {
        ["MongoDB (ENV)"] = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING") ?? "не установлена",
        ["MongoDB (Config)"] = builder.Configuration["MongoDBSettings:ConnectionString"] ?? "не найдена",
        ["PostgreSQL (ENV)"] = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__POSTGRESQL") ?? "не установлена",
        ["PostgreSQL (Config)"] = builder.Configuration.GetConnectionString("PostgreSQL") ?? "не найдена",
        ["JWT (ENV)"] = Environment.GetEnvironmentVariable("API_SECRET_KEY") ?? "не установлена",
        ["JWT (Config)"] = builder.Configuration["ApiSettings:Secret"] ?? "не найдена"
    };

    Console.WriteLine("\n🔍 Configuration Values:");
    foreach (var kvp in allSources)
    {
        var value = kvp.Value;
        var displayValue = value.Length > 20 ? $"{value.Substring(0, 20)}..." : value;
        Console.WriteLine($"  {kvp.Key}: {displayValue}");
    }
    Console.WriteLine("==========================================\n");
}

// Add services to the container.

// PostgreSQL - приоритет переменным окружения
var postgresConnection = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__POSTGRESQL")
    ?? builder.Configuration.GetConnectionString("PostgreSQL");

if (string.IsNullOrEmpty(postgresConnection))
{
    throw new InvalidOperationException(
        "PostgreSQL connection string is not configured. " +
        "Please set CONNECTIONSTRINGS__POSTGRESQL environment variable or " +
        "configure ConnectionStrings:PostgreSQL in appsettings.json");
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

// JWT настройки - приоритет переменным окружения
var jwtSecret = Environment.GetEnvironmentVariable("API_SECRET_KEY")
    ?? Environment.GetEnvironmentVariable("APISETTINGS__SECRET")
    ?? builder.Configuration["ApiSettings:Secret"];

if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException(
        "JWT Secret is not configured. " +
        "Please set API_SECRET_KEY or APISETTINGS__SECRET environment variable or " +
        "configure ApiSettings:Secret in appsettings.json");
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

// MongoDB настройки - приоритет переменным окружения
builder.Services.Configure<Guider.API.MVP.Data.MongoDbSettings>(options =>
{
    // Приоритет: переменные окружения -> appsettings
    options.ConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
        ?? Environment.GetEnvironmentVariable("MONGODBSETTINGS__CONNECTIONSTRING")
        ?? builder.Configuration["MongoDBSettings:ConnectionString"];

    options.DatabaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME")
        ?? Environment.GetEnvironmentVariable("MONGODBSETTINGS__DATABASENAME")
        ?? builder.Configuration["MongoDBSettings:DatabaseName"];

    // Настройка коллекций из переменных окружения
    options.Collections = new Dictionary<string, string>();

    var places = Environment.GetEnvironmentVariable("MONGODB_PLACES_COLLECTION")
        ?? Environment.GetEnvironmentVariable("MONGODBSETTINGS__COLLECTIONS__PLACES")
        ?? builder.Configuration["MongoDBSettings:Collections:Places"];
    if (!string.IsNullOrEmpty(places))
        options.Collections["Places"] = places;

    var cities = Environment.GetEnvironmentVariable("MONGODB_CITIES_COLLECTION")
        ?? Environment.GetEnvironmentVariable("MONGODBSETTINGS__COLLECTIONS__CITIES")
        ?? builder.Configuration["MongoDBSettings:Collections:Cities"];
    if (!string.IsNullOrEmpty(cities))
        options.Collections["Cities"] = cities;

    var provinces = Environment.GetEnvironmentVariable("MONGODB_PROVINCES_COLLECTION")
        ?? Environment.GetEnvironmentVariable("MONGODBSETTINGS__COLLECTIONS__PROVINCES")
        ?? builder.Configuration["MongoDBSettings:Collections:Provinces"];
    if (!string.IsNullOrEmpty(provinces))
        options.Collections["Provinces"] = provinces;

    var tags = Environment.GetEnvironmentVariable("MONGODB_TAGS_COLLECTION")
        ?? Environment.GetEnvironmentVariable("MONGODBSETTINGS__COLLECTIONS__TAGS")
        ?? builder.Configuration["MongoDBSettings:Collections:Tags"];
    if (!string.IsNullOrEmpty(tags))
        options.Collections["Tags"] = tags;

    var images = Environment.GetEnvironmentVariable("MONGODB_IMAGES_COLLECTION")
        ?? Environment.GetEnvironmentVariable("MONGODBSETTINGS__COLLECTIONS__IMAGES")
        ?? builder.Configuration["MongoDBSettings:Collections:Images"];
    if (!string.IsNullOrEmpty(images))
        options.Collections["Images"] = images;

    if (string.IsNullOrEmpty(options.ConnectionString))
    {
        throw new InvalidOperationException(
            "MongoDB ConnectionString is not configured.");
    }

    if (string.IsNullOrEmpty(options.DatabaseName))
    {
        throw new InvalidOperationException(
            "MongoDB DatabaseName is not configured.");
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

// Конфигурация MinIO - приоритет переменным окружения
var minioSettings = new MinioSettings
{
    Endpoint = Environment.GetEnvironmentVariable("MINIOSETTINGS__ENDPOINT")
        ?? builder.Configuration["MinioSettings:Endpoint"],
    Port = int.TryParse(Environment.GetEnvironmentVariable("MINIOSETTINGS__PORT"), out var port)
        ? port
        : (builder.Configuration.GetValue<int?>("MinioSettings:Port") ?? 9000),
    AccessKey = Environment.GetEnvironmentVariable("MINIOSETTINGS__ACCESSKEY")
        ?? builder.Configuration["MinioSettings:AccessKey"],
    SecretKey = Environment.GetEnvironmentVariable("MINIOSETTINGS__SECRETKEY")
        ?? builder.Configuration["MinioSettings:SecretKey"],
    BucketName = Environment.GetEnvironmentVariable("MINIOSETTINGS__BUCKETNAME")
        ?? builder.Configuration["MinioSettings:BucketName"],
    UseSSL = bool.TryParse(Environment.GetEnvironmentVariable("MINIOSETTINGS__USESSL"), out var useSSL)
        ? useSSL
        : (builder.Configuration.GetValue<bool?>("MinioSettings:UseSSL") ?? false)
};

// Валидация конфигурации MinIO
if (string.IsNullOrEmpty(minioSettings.Endpoint) ||
    string.IsNullOrEmpty(minioSettings.AccessKey) ||
    string.IsNullOrEmpty(minioSettings.SecretKey) ||
    string.IsNullOrEmpty(minioSettings.BucketName))
{
    throw new InvalidOperationException(
        "MinIO settings are not properly configured.");
}

// Регистрация MinIO настроек
builder.Services.AddSingleton(minioSettings);
builder.Services.AddScoped<IMinioService, MinioService>();

// Диагностика MinIO настроек (только в Development)
if (builder.Environment.IsDevelopment())
{
    Console.WriteLine("🔍 MinIO Configuration Validation:");
    Console.WriteLine($"  - Endpoint: {(string.IsNullOrEmpty(minioSettings.Endpoint) ? "❌ MISSING" : "✅ OK")}");
    Console.WriteLine($"  - Port: {minioSettings.Port}");
    Console.WriteLine($"  - AccessKey: {(string.IsNullOrEmpty(minioSettings.AccessKey) ? "❌ MISSING" : "✅ OK")}");
    Console.WriteLine($"  - SecretKey: {(string.IsNullOrEmpty(minioSettings.SecretKey) ? "❌ MISSING" : "✅ OK")}");
    Console.WriteLine($"  - BucketName: {(string.IsNullOrEmpty(minioSettings.BucketName) ? "❌ MISSING" : "✅ OK")}");
    Console.WriteLine($"  - UseSSL: {minioSettings.UseSSL}");
}

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
    Console.WriteLine($"MinIO configured: ✅");
    Console.WriteLine("===============================");
}

app.UseHttpsRedirection();
app.UseCors(builder =>
{
    builder.AllowAnyOrigin()
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
