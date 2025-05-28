
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

// PostgreSQL - используем переменные окружения с приоритетом, затем appsettings
var postgresConnection = builder.Configuration.GetConnectionString("PostgreSQL");
if (string.IsNullOrEmpty(postgresConnection))
{
    throw new InvalidOperationException(
        "PostgreSQL connection string is not configured. " +
        "Please set CONNECTIONSTRINGS__POSTGRESQL environment variable or " +
        "configure ConnectionStrings:PostgreSQL in appsettings.json");
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
    options.User.AllowedUserNameCharacters = null; // Разрешить любые символы
    options.User.RequireUniqueEmail = false; // Отключить требование уникальности Email (если нужно)
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
var jwtSecret = builder.Configuration["ApiSettings:Secret"];
if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException(
        "JWT Secret is not configured. " +
        "Please set API_SECRET_KEY environment variable or " +
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

// MongoDB настройки - гибкая конфигурация для dev и prod
builder.Services.Configure<Guider.API.MVP.Data.MongoDbSettings>(options =>
{
    // Приоритет: переменные окружения -> appsettings
    options.ConnectionString = builder.Configuration["MongoDBSettings:ConnectionString"];
    options.DatabaseName = builder.Configuration["MongoDBSettings:DatabaseName"];

    // Поддержка коллекций из конфигурации
    var collectionsSection = builder.Configuration.GetSection("MongoDBSettings:Collections");
    if (collectionsSection.Exists())
    {
        // Если используется новая структура с коллекциями
        options.Collections = new Dictionary<string, string>();
        foreach (var collection in collectionsSection.GetChildren())
        {
            options.Collections[collection.Key] = collection.Value;
        }
    }
    else
    {
        // Fallback для старой структуры или переменных окружения
        options.CollectionName = builder.Configuration["MongoDBSettings:CollectionName"];
    }

    if (string.IsNullOrEmpty(options.ConnectionString))
    {
        throw new InvalidOperationException(
            "MongoDB ConnectionString is not configured. " +
            "Please set MONGODB_CONNECTION_STRING environment variable or " +
            "configure MongoDBSettings:ConnectionString in appsettings.json");
    }

    if (string.IsNullOrEmpty(options.DatabaseName))
    {
        throw new InvalidOperationException(
            "MongoDB DatabaseName is not configured. " +
            "Please set MONGODB_DATABASE_NAME environment variable or " +
            "configure MongoDBSettings:DatabaseName in appsettings.json");
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
        else if (!string.IsNullOrEmpty(options.CollectionName))
        {
            Console.WriteLine($"✅ MongoDB Collection: {options.CollectionName}");
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
    // Set the comments path for the Swagger JSON and UI.
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
    // Регистрируем наш фильтр для корректной обработки загрузки файлов
    c.OperationFilter<FileUploadOperationFilter>();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // Проверяем, есть ли неприменённые миграции
    var pendingMigrations = db.Database.GetPendingMigrations();
    if (pendingMigrations.Any())
    {
        Console.WriteLine("Выполняется автоматическая миграция базы данных...");
        db.Database.Migrate();
        Console.WriteLine("Миграция завершена.");
    }
}



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Дополнительная информация для разработчиков
    Console.WriteLine("🚀 === APPLICATION STARTED ===");
    Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
    Console.WriteLine($"PostgreSQL configured: ✅");
    Console.WriteLine($"JWT configured: ✅");
    Console.WriteLine($"MongoDB configured: ✅");
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

app.Run();