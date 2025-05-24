Guider.API.MVP

Описание
Guider.API.MVP — это серверное приложение на ASP.NET Core (.NET 8), реализующее REST API для работы с пользователями, ролями, городами, провинциями, тегами и другими сущностями. Проект поддерживает аутентификацию через JWT и интеграцию с MongoDB и PostgreSQL.


Быстрый старт (локально)


Клонируйте репозиторий:
git clone <URL-репозитория> cd Guider.API.MVP


Создайте файл переменных окружения .env.local (для локальной разработки):
Отредактируйте значения переменных под вашу среду.
cp .env.example .env.local


Запустите проект:
dotnet run --project Guider.API.MVP




Запуск в Docker

1. Подготовьте файл переменных окружения
Создайте файл .env.docker в корне проекта со следующим содержимым:
MONGODB_CONNECTION_STRING=mongodb://mongo_user:prod_password@mongo_host:27017/prod_db MONGODB_DATABASE_NAME=guider_prod MONGODB_PLACES_COLLECTION=places MONGODB_CITIES_COLLECTION=cities MONGODB_PROVINCES_COLLECTION=provinces MONGODB_TAGS_COLLECTION=tags CONNECTIONSTRINGS__POSTGRESQL=Host=prod-db-host;Port=5432;Database=guider_prod;Username=prod_user;Password=ProdSecret! API_SECRET_KEY=YOUR_PROD_SECRET_KEY ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://+:80
SUPERADMIN_USERNAME=SuperAdmin SUPERADMIN_EMAIL=superadmin@example.com SUPERADMIN_PASSWORD=SuperSecret123!
Не коммитьте этот файл в репозиторий!

2. Сборка Docker-образа
docker build -t guider-api .

3. Запуск контейнера
docker run --env-file .env.docker -p 80:80 guider-api


Переменные окружения


MONGODB_CONNECTION_STRING — строка подключения к MongoDB

MONGODB_DATABASE_NAME — имя базы данных MongoDB

MONGODB_PLACES_COLLECTION и др. — имена коллекций MongoDB

CONNECTIONSTRINGS__POSTGRESQL — строка подключения к PostgreSQL

API_SECRET_KEY — секретный ключ для JWT

ASPNETCORE_ENVIRONMENT — окружение ASP.NET (Production/Development)

ASPNETCORE_URLS — адреса, на которых слушает приложение

SUPERADMIN_USERNAME, SUPERADMIN_EMAIL, SUPERADMIN_PASSWORD — данные для автоматического создания первого пользователя с ролью superadmin



Автоматическая инициализация

При первом запуске контейнера:

Применяются все миграции к базе данных PostgreSQL.
Создаются все необходимые роли.
Создаётся первый пользователь с ролью superadmin (данные берутся из переменных окружения).





Swagger
После запуска API доступна документация Swagger по адресу:
http://localhost/swagger


Лицензия
MIT