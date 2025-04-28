# Guider.API.MVP

## Описание проекта
Guider.API.MVP — это API-сервис, разработанный на ASP.NET Core (.NET 8), предназначенный для управления изображениями, тегами и другими сущностями. Проект использует PostgreSQL для хранения данных ASP.NET Identity и MongoDB для работы с коллекциями данных. API поддерживает аутентификацию с использованием JWT и предоставляет удобный интерфейс для работы с данными через RESTful API.

---

## Особенности проекта
- **ASP.NET Identity**: Реализована система аутентификации и авторизации.
- **PostgreSQL**: Используется для хранения данных пользователей и ролей.
- **MongoDB**: Используется для хранения коллекций данных.
- **Swagger**: Для документирования и тестирования API.
- **Docker**: Поддержка контейнеризации для удобного развертывания.

---

## Установка и настройка

### 1. Клонирование репозитория

```shell
git clone <URL вашего репозитория>
cd Guider.API.MVP

```

---

## Настройка Docker

### 2. Создание Docker-образа
Для создания Docker-образа выполните следующие шаги:

1. Убедитесь, что у вас установлен Docker.
2. Создайте файл `Dockerfile` в корне проекта (если его нет):
   
```docker
   FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
   WORKDIR /app
   EXPOSE 80

   FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
   WORKDIR /src
   COPY ["Guider.API.MVP/Guider.API.MVP.csproj", "Guider.API.MVP/"]
   RUN dotnet restore "Guider.API.MVP/Guider.API.MVP.csproj"
   COPY . .
   WORKDIR "/src/Guider.API.MVP"
   RUN dotnet build -c Release -o /app/build

   FROM build AS publish
   RUN dotnet publish -c Release -o /app/publish

   FROM base AS final
   WORKDIR /app
   COPY --from=publish /app/publish .
   ENTRYPOINT ["dotnet", "Guider.API.MVP.dll"]
   
```

3. Соберите Docker-образ:
   
```shell
   docker build -t guider-api-mvp .
   
```

4. Запустите контейнер:
   
```shell
   docker run -d -p 5000:80 --name guider-api-mvp-container guider-api-mvp
   
```

---

## Настройка подключения к PostgreSQL

### 3. Конфигурация PostgreSQL
1. Убедитесь, что PostgreSQL установлен на удалённом сервере.
2. В файле `appsettings.json` укажите строку подключения:
   
```json
   "ConnectionStrings": {
       "PostgreSQL": "Host=<REMOTE_HOST>;Port=5432;Database=<DB_NAME>;Username=<USERNAME>;Password=<PASSWORD>"
   }
   
```

3. Выполните миграции для создания таблиц ASP.NET Identity:
   
```shell
   dotnet ef migrations add InitialIdentityMigration
   dotnet ef database update
   
```

---

## Настройка подключения к MongoDB

### 4. Конфигурация MongoDB
1. Убедитесь, что MongoDB установлен на удалённом сервере.
2. В файле `appsettings.json` укажите настройки MongoDB:
   
```json
   "MongoDbSettings": {
       "ConnectionString": "mongodb://<USERNAME>:<PASSWORD>@<REMOTE_HOST>:27017",
       "DatabaseName": "guider",
       "Collections": {
          "Places": "places_clear",
           "Tags": "tags",
           "Provinces": "provinces",
           "Cities": "cities"
       }
   }
   
```

3. Проверьте, что сервисы MongoDB зарегистрированы в `Program.cs`:
   
```csharp
   builder.Services.Configure<MongoDbSettings>(
       builder.Configuration.GetSection("MongoDbSettings"));
   builder.Services.AddSingleton<PlaceService>();
   builder.Services.AddSingleton<TagsService>();
   builder.Services.AddSingleton<ProvinceService>();
   builder.Services.AddSingleton<CitiesService>();
   
```// Регистрация сервиса для работы с изображениями
   builder.Services.AddScoped<IImageService, ImageService>();

---

## Запуск приложения
1. Запустите приложение локально:
   
```shell
   dotnet run
   
```

2. Откройте Swagger для тестирования API:
   
```
   http://localhost:5000/swagger
   
```

---

## Примечания
- Убедитесь, что порты PostgreSQL и MongoDB открыты для удалённого подключения.
- Используйте безопасные пароли и настройте брандмауэр для защиты баз данных.
- Для продакшн-среды рекомендуется использовать Docker Compose для управления зависимостями.