```markdown
# Guider.API.MVP

## Description

Guider.API.MVP is a backend application built with ASP.NET Core (.NET 8), implementing a REST API for managing users, roles, cities, provinces, tags, and other entities. The project supports JWT authentication and integrates with both MongoDB and PostgreSQL. File storage is handled via MinIO.

---

## Quick Start (Local Development)

1. **Clone the repository:**
```
   git clone <repository-url>
   cd Guider.API.MVP
```

2. **Create a local environment variables file `.env.local`:**
   Copy the example file and edit variable values for your environment.
```
   cp .env.example .env.local
```
> **Note:** Do not use real credentials in `.env.local` for development. Set your own values for connection strings, secrets, and admin credentials.

3. **Run the project:**
```
   dotnet run --project Guider.API.MVP
```

---

## Docker Deployment

### 1. Prepare the environment variables file

Create a `.env.docker` file in the project root.  
**Do not commit this file to the repository!**

Example structure (replace with your actual values):
```
MONGODB_CONNECTION_STRING=your_mongodb_connection_string
MONGODB_DATABASE_NAME=your_mongodb_database_name
MONGODB_PLACES_COLLECTION=places
MONGODB_CITIES_COLLECTION=cities
MONGODB_PROVINCES_COLLECTION=provinces
MONGODB_TAGS_COLLECTION=tags
MONGODB_IMAGES_COLLECTION=images
CONNECTIONSTRINGS__POSTGRESQL=your_postgresql_connection_string
API_SECRET_KEY=your_jwt_secret_key
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80
SUPERADMIN_USERNAME=your_superadmin_username
SUPERADMIN_EMAIL=your_superadmin_email
SUPERADMIN_PASSWORD=your_superadmin_password
MINIOSETTINGS__ENDPOINT=your_minio_endpoint
MINIOSETTINGS__PORT=9000
MINIOSETTINGS__ACCESSKEY=your_minio_access_key
MINIOSETTINGS__SECRETKEY=your_minio_secret_key
MINIOSETTINGS__BUCKETNAME=uploads
MINIOSETTINGS__USESSL=false
```

> **For remote/production deployment, you must configure the `.env.docker` file with secure values for all secrets and service credentials.**

### 2. Build the Docker image
```
docker build -t guider-api .
```

### 3. Run the container
```
docker run --env-file .env.docker -p 80:80 guider-api
```

---

## Environment Variables

- **MONGODB_CONNECTION_STRING** — MongoDB connection string
- **MONGODB_DATABASE_NAME** — MongoDB database name
- **MONGODB_PLACES_COLLECTION**, etc. — MongoDB collection names
- **CONNECTIONSTRINGS__POSTGRESQL** — PostgreSQL connection string
- **API_SECRET_KEY** — JWT secret key
- **ASPNETCORE_ENVIRONMENT** — ASP.NET environment (Production/Development)
- **ASPNETCORE_URLS** — URLs the app listens on
- **SUPERADMIN_USERNAME**, **SUPERADMIN_EMAIL**, **SUPERADMIN_PASSWORD** — credentials for automatic creation of the first superadmin user
- **MINIOSETTINGS__ENDPOINT** — MinIO server endpoint
- **MINIOSETTINGS__PORT** — MinIO server port
- **MINIOSETTINGS__ACCESSKEY** — MinIO access key
- **MINIOSETTINGS__SECRETKEY** — MinIO secret key
- **MINIOSETTINGS__BUCKETNAME** — MinIO bucket name
- **MINIOSETTINGS__USESSL** — Use SSL for MinIO (true/false)

> **New in this version:**  
> - Added MinIO integration for file storage.  
> - All secrets and service credentials are now stored in environment variables for improved security and flexibility.

---

## Automatic Initialization

- On first container startup:
  - All PostgreSQL migrations are applied.
  - All required roles are created.
  - The first user with the `superadmin` role is created (credentials are taken from environment variables).

---

## Swagger

After starting the API, Swagger documentation is available at:  
`http://localhost/swagger`

---

## License

MIT
```