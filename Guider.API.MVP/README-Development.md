# Guider API - Руководство для разработчиков

## 🏠 Локальная разработка

### Быстрый старт

1. **Клонируйте репозиторий и перейдите в папку проекта**
2. **Запустите скрипт настройки:**

   ```bash
   # Linux/Mac
   chmod +x setup-local-dev.sh
   ./setup-local-dev.sh

   # Windows
   setup-local-dev.bat
   ```

3. **Запустите проект:**
   ```bash
   dotnet run
   # или с автоперезагрузкой
   dotnet watch run
   ```

### Конфигурация

#### Приоритет настроек (от высшего к низшему):

1. **Переменные окружения** (`.env.local` для локальной разработки)
2. **appsettings.Development.json** (ваши текущие настройки)
3. **appsettings.json** (базовые настройки)

#### Файлы конфигурации:

```
📁 Проект/
├── appsettings.json                 # Базовые настройки
├── appsettings.Development.json     # Настройки для разработки ✅
├── appsettings.Production.json      # Настройки для продакшн
├── .env.local                       # Локальные переменные (не в Git)
├── .env.local.example              # Пример локальных переменных
└── .env                            # Production переменные (не в Git)
```

### Переопределение настроек

Если нужно изменить какие-то настройки локально (например, подключиться к другой БД), создайте/отредактируйте `.env.local`:

```bash
# Пример переопределения MongoDB
MONGODB_CONNECTION_STRING=mongodb://localhost:27017
MONGODB_DATABASE_NAME=guider_local_test

# Пример переопределения PostgreSQL
CONNECTIONSTRINGS__POSTGRESQL=Host=localhost;Port=5432;Database=guider_local;Username=postgres;Password=mypassword
```

### Отладка конфигурации

В Development режиме приложение выводит информацию о подключениях:

```
=== DEVELOPMENT MODE ===
Environment: Development
Using PostgreSQL: True
JWT configured: True
MongoDB Connection: mongodb://localhost...
MongoDB Database: guider
========================
```

## 🐳 Docker для локальной разработки

### Быстрый запуск с Docker

```bash
# Создание .env файла
cp .env.example .env
nano .env  # Отредактируйте настройки

# Запуск
docker-compose up -d --build

# Просмотр логов
docker-compose logs -f guider-api
```

### Docker Compose для разработки

```yaml
# docker-compose.dev.yml - для локальной разработки
version: "3.8"
services:
  guider-api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    volumes:
      - ./wwwroot/images:/app/wwwroot/images
      - .:/src # Монтирование исходного кода для live reload
    env_file:
      - .env.local
```

## 🚀 Деплой

### Подготовка к деплою

1. **Создайте production настройки:**

   ```bash
   cp .env.example .env
   # Отредактируйте .env с production данными
   ```

2. **Запустите деплой:**
   ```bash
   chmod +x deploy.sh
   ./deploy.sh
   ```

### Настройка переменных на сервере

```bash
# Установка переменных на сервере
export MONGODB_CONNECTION_STRING="mongodb://..."
export POSTGRESQL_CONNECTION_STRING="Host=..."
export API_SECRET_KEY="your-secret-key"

# Или используйте server-setup.sh
chmod +x server-setup.sh
./server-setup.sh
```

## 🔒 Безопасность

### Что НЕ коммитить в Git:

- `.env`
- `.env.local`
- `.env.production`
- `appsettings.*.Local.json`

### Рекомендации:

- Используйте разные секретные ключи для dev/test/prod
- Регулярно ротируйте пароли и ключи
- Никогда не передавайте секреты через параметры командной строки
- В production используйте Docker Secrets или Key Vault

## 🛠 Устранение проблем

### Ошибки подключения к БД

1. **Проверьте переменные окружения:**

   ```bash
   echo $MONGODB_CONNECTION_STRING
   echo $POSTGRESQL_CONNECTION_STRING
   ```

2. **Проверьте приоритет конфигурации:**

   - Переменные окружения переопределяют appsettings
   - В Development логи покажут активную конфигурацию

3. **Проверьте формат connection string:**
   - MongoDB: `mongodb://user:pass@host:port/database`
   - PostgreSQL: `Host=host;Port=port;Database=db;Username=user;Password=pass`

### JWT ошибки

Убедитесь, что `API_SECRET_KEY` или `ApiSettings:Secret` настроен и не пустой.

### Docker проблемы

```bash
# Очистка и пересборка
docker-compose down
docker system prune -f
docker-compose up -d --build

# Проверка переменных в контейнере
docker-compose exec guider-api printenv | grep MONGODB
```

## 📞 Поддержка

При возникновении проблем:

1. Проверьте логи: `docker-compose logs guider-api`
2. Убедитесь в правильности переменных окружения
3. Проверьте .gitignore - секреты не должны попадать в Git
