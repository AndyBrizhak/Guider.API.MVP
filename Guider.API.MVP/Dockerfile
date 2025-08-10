# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble AS base
USER root
# Создаем папку для приложения и устанавливаем права
RUN mkdir -p /app && chown -R app:app /app
USER app
WORKDIR /app
#EXPOSE 8080
EXPOSE 8081

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Guider.API.MVP/Guider.API.MVP.csproj", "Guider.API.MVP/"]
RUN dotnet restore "./Guider.API.MVP/Guider.API.MVP.csproj"
COPY . .
WORKDIR "/src/Guider.API.MVP"
RUN dotnet build "./Guider.API.MVP.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Guider.API.MVP.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Копируем .env.docker файл в контейнер
#COPY .env.docker .env.docker

# Копируем все доступные .env файлы в контейнер
# Docker автоматически пропустит несуществующие файлы при использовании wildcards
COPY .env* ./

# Устанавливаем переменные окружения по умолчанию для ASP.NET Core
ENV ASPNETCORE_ENVIRONMENT=Development
#ENV ASPNETCORE_URLS=http://+:8080;https://+:8081
ENV ASPNETCORE_URLS=https://+:8081
ENV ASPNETCORE_HTTPS_PORT=8081

# Создаем папку для логов и статических файлов
USER root
RUN mkdir -p /app/wwwroot/images && chown -R app:app /app/wwwroot
USER app

ENTRYPOINT ["dotnet", "Guider.API.MVP.dll"]