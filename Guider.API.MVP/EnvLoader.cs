namespace Guider.API.MVP
{
    public class EnvLoader
    {
        /// <summary>
        /// Загружает переменные окружения из .env файлов с приоритетом:
        /// 1. .env.local (локальная разработка без Docker)
        /// 2. .env.docker (Docker с подключением к локальным БД)
        /// 3. .env (продакшн Docker с удаленными БД)
        /// 
        /// 
        /// </summary>
        public static void LoadEnvFiles()
        {
            Console.WriteLine("🔍 === ПОИСК И ЗАГРУЗКА ПЕРЕМЕННЫХ ОКРУЖЕНИЯ ===");

            string selectedEnvFile = SelectEnvironmentFile();

            if (!string.IsNullOrEmpty(selectedEnvFile))
            {
                Console.WriteLine($"📄 Выбран файл конфигурации: {selectedEnvFile}");
                LoadEnvFile(selectedEnvFile);
            }
            else
            {
                Console.WriteLine("⚠️ Файлы переменных окружения не найдены!");
                Console.WriteLine("Приложение будет использовать переменные из appsettings.json");
            }

            // После загрузки .env файлов настраиваем специальные переменные
            SetupAdditionalVariables();

            Console.WriteLine("===============================================\n");
        }

        /// <summary>
        /// Выбирает файл переменных окружения по приоритету
        /// </summary>
        //private static string SelectEnvironmentFile()
        //{
        //    var envFiles = new[]
        //    {
        //        new { Path = ".env.local", Description = "Локальная разработка (без Docker)" },
        //        new { Path = ".env.docker", Description = "Docker с локальными БД" },
        //        new { Path = ".env", Description = "Docker с удаленными БД" }
        //    };

        //    Console.WriteLine("\n🔍 Проверка доступных файлов конфигурации:");

        //    foreach (var envFile in envFiles)
        //    {
        //        bool exists = File.Exists(envFile.Path);
        //        string status = exists ? "✅ найден" : "❌ отсутствует";
        //        Console.WriteLine($"  - {envFile.Path} ({envFile.Description}): {status}");

        //        if (exists)
        //        {
        //            Console.WriteLine($"  → Выбран: {envFile.Path}");
        //            return envFile.Path;
        //        }
        //    }

        //    return null;
        //}

        /// <summary>
        /// Выбирает первый найденный файл переменных окружения из доступных
        /// </summary>
        private static string SelectEnvironmentFile()
        {
            // Список файлов в порядке приоритета
            var envFiles = new[]
            {
                ".env.local",   // Локальная разработка
                ".env.docker",  // Docker окружение  
                ".env"          // Универсальный fallback
            };

            Console.WriteLine("\n🔍 Поиск файлов конфигурации:");

            foreach (var envFile in envFiles)
            {
                bool exists = File.Exists(envFile);
                string status = exists ? "✅ найден" : "❌ отсутствует";
                Console.WriteLine($"  - {envFile}: {status}");

                if (exists)
                {
                    Console.WriteLine($"  → Выбран: {envFile}");
                    return envFile;
                }
            }

            Console.WriteLine("⚠️ Файлы конфигурации не найдены!");
            return null;
        }

        /// <summary>
        /// Загружает переменные из указанного .env файла
        /// </summary>
        private static void LoadEnvFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"📄 {filePath} не найден, пропускаем");
                return;
            }

            Console.WriteLine($"\n📄 Загружаем переменные из {filePath}...");

            var lines = File.ReadAllLines(filePath);
            var loadedCount = 0;
            var skippedCount = 0;

            foreach (var line in lines)
            {
                // Пропускаем пустые строки и комментарии
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length != 2)
                    continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                // Убираем кавычки если есть
                if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                    (value.StartsWith("'") && value.EndsWith("'")))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                // Устанавливаем переменную окружения только если она еще не установлена
                var existingValue = Environment.GetEnvironmentVariable(key);
                if (string.IsNullOrEmpty(existingValue))
                {
                    Environment.SetEnvironmentVariable(key, value);
                    loadedCount++;

                    // Маскируем чувствительные данные для вывода
                    var displayValue = ShouldMaskValue(key) && value.Length > 10
                        ? $"{value.Substring(0, 10)}..."
                        : (value.Length > 50 ? $"{value.Substring(0, 50)}..." : value);

                    Console.WriteLine($"  ✅ {key} = {displayValue}");
                }
                else
                {
                    skippedCount++;
                    Console.WriteLine($"  ⚠️  {key} уже установлена, пропускаем");
                }
            }

            Console.WriteLine($"📊 Результат загрузки {filePath}:");
            Console.WriteLine($"  - Загружено: {loadedCount} переменных");
            Console.WriteLine($"  - Пропущено: {skippedCount} переменных (уже установлены)");
        }

        /// <summary>
        /// Определяет, нужно ли маскировать значение переменной для безопасности
        /// </summary>
        private static bool ShouldMaskValue(string key)
        {
            var sensitiveKeys = new[]
            {
                "PASSWORD", "SECRET", "KEY", "TOKEN", "CONNECTIONSTRING",
                "ACCESSKEY", "SECRETKEY", "CONNECTION_STRING"
            };

            return sensitiveKeys.Any(sensitive =>
                key.ToUpperInvariant().Contains(sensitive));
        }

        /// <summary>
        /// Настраивает дополнительные переменные окружения и выполняет маппинг
        /// </summary>
        private static void SetupAdditionalVariables()
        {
            Console.WriteLine("\n🔄 Настройка дополнительных переменных...");

            int mappedCount = 0;

            // Маппинг MongoDB коллекций
            mappedCount += SetupMongoCollectionVariables();

            // Маппинг MinIO настроек
            mappedCount += SetupMinioVariables();

            // Проверка и логирование ключевых настроек
            mappedCount += ValidateAndLogKeySettings();

            if (mappedCount > 0)
            {
                Console.WriteLine($"📄 Обработано {mappedCount} дополнительных настроек");
            }
        }

        /// <summary>
        /// Настраивает переменные окружения для MongoDB коллекций
        /// </summary>
        private static int SetupMongoCollectionVariables()
        {
            var collectionMappings = new Dictionary<string, string>
            {
                ["MONGODB_PLACES_COLLECTION"] = "MONGODBSETTINGS__COLLECTIONS__PLACES",
                ["MONGODB_CITIES_COLLECTION"] = "MONGODBSETTINGS__COLLECTIONS__CITIES",
                ["MONGODB_PROVINCES_COLLECTION"] = "MONGODBSETTINGS__COLLECTIONS__PROVINCES",
                ["MONGODB_TAGS_COLLECTION"] = "MONGODBSETTINGS__COLLECTIONS__TAGS",
                ["MONGODB_IMAGES_COLLECTION"] = "MONGODBSETTINGS__COLLECTIONS__IMAGES"
            };

            int mappedCount = 0;

            foreach (var mapping in collectionMappings)
            {
                var envValue = Environment.GetEnvironmentVariable(mapping.Key);
                if (!string.IsNullOrEmpty(envValue))
                {
                    // Устанавливаем переменную в формате ASP.NET Core конфигурации
                    var targetKey = mapping.Value;
                    var existingValue = Environment.GetEnvironmentVariable(targetKey);

                    if (string.IsNullOrEmpty(existingValue))
                    {
                        Environment.SetEnvironmentVariable(targetKey, envValue);
                        mappedCount++;
                        Console.WriteLine($"  🔄 {mapping.Key} -> {targetKey} = {envValue}");
                    }
                }
            }

            return mappedCount;
        }

        /// <summary>
        /// Настраивает переменные окружения для MinIO с дополнительной обработкой порта
        /// </summary>
        private static int SetupMinioVariables()
        {
            Console.WriteLine("\n🔧 Настройка переменных MinIO:");

            int mappedCount = 0;

            // Проверяем и устанавливаем порт MinIO если он задан как отдельная переменная
            var minioPort = Environment.GetEnvironmentVariable("MINIO_PORT")
                ?? Environment.GetEnvironmentVariable("MINIOSETTINGS__PORT");

            if (!string.IsNullOrEmpty(minioPort))
            {
                // Убеждаемся, что переменная MINIOSETTINGS__PORT установлена
                var existingPort = Environment.GetEnvironmentVariable("MINIOSETTINGS__PORT");
                if (string.IsNullOrEmpty(existingPort))
                {
                    Environment.SetEnvironmentVariable("MINIOSETTINGS__PORT", minioPort);
                    mappedCount++;
                    Console.WriteLine($"  🔄 MINIO_PORT -> MINIOSETTINGS__PORT = {minioPort}");
                }
            }

            // Дополнительная проверка и маппинг всех MinIO настроек
            var minioMappings = new Dictionary<string, string>
            {
                ["MINIO_ENDPOINT"] = "MINIOSETTINGS__ENDPOINT",
                ["MINIO_ACCESS_KEY"] = "MINIOSETTINGS__ACCESSKEY",
                ["MINIO_SECRET_KEY"] = "MINIOSETTINGS__SECRETKEY",
                ["MINIO_BUCKET_NAME"] = "MINIOSETTINGS__BUCKETNAME",
                ["MINIO_USE_SSL"] = "MINIOSETTINGS__USESSL"
            };

            foreach (var mapping in minioMappings)
            {
                var envValue = Environment.GetEnvironmentVariable(mapping.Key);
                if (!string.IsNullOrEmpty(envValue))
                {
                    var targetKey = mapping.Value;
                    var existingValue = Environment.GetEnvironmentVariable(targetKey);

                    if (string.IsNullOrEmpty(existingValue))
                    {
                        Environment.SetEnvironmentVariable(targetKey, envValue);
                        mappedCount++;

                        var displayValue = ShouldMaskValue(targetKey) && envValue.Length > 10
                            ? $"{envValue.Substring(0, 10)}..."
                            : envValue;
                        Console.WriteLine($"  🔄 {mapping.Key} -> {targetKey} = {displayValue}");
                    }
                }
            }

            return mappedCount;
        }

        /// <summary>
        /// Валидирует и логирует ключевые настройки
        /// </summary>
        private static int ValidateAndLogKeySettings()
        {
            Console.WriteLine("\n🔍 Проверка ключевых настроек:");

            var keySettings = new Dictionary<string, string[]>
            {
                ["MongoDB Connection"] = new[] { "MONGODB_CONNECTION_STRING", "MONGODBSETTINGS__CONNECTIONSTRING" },
                ["MongoDB Database"] = new[] { "MONGODB_DATABASE_NAME", "MONGODBSETTINGS__DATABASENAME" },
                ["PostgreSQL Connection"] = new[] { "CONNECTIONSTRINGS__POSTGRESQL" },
                ["JWT Secret"] = new[] { "API_SECRET_KEY", "APISETTINGS__SECRET" },
                ["MinIO Endpoint"] = new[] { "MINIOSETTINGS__ENDPOINT", "MINIO_ENDPOINT" },
                ["MinIO Port"] = new[] { "MINIOSETTINGS__PORT", "MINIO_PORT" },
                ["MinIO AccessKey"] = new[] { "MINIOSETTINGS__ACCESSKEY", "MINIO_ACCESS_KEY" },
                ["MinIO SecretKey"] = new[] { "MINIOSETTINGS__SECRETKEY", "MINIO_SECRET_KEY" },
                ["MinIO Bucket"] = new[] { "MINIOSETTINGS__BUCKETNAME", "MINIO_BUCKET_NAME" },
                ["MinIO UseSSL"] = new[] { "MINIOSETTINGS__USESSL", "MINIO_USE_SSL" }
            };

            int validatedCount = 0;

            foreach (var setting in keySettings)
            {
                bool found = false;
                string foundValue = null;
                string foundKey = null;

                foreach (var key in setting.Value)
                {
                    var value = Environment.GetEnvironmentVariable(key);
                    if (!string.IsNullOrEmpty(value))
                    {
                        found = true;
                        foundValue = value;
                        foundKey = key;
                        break;
                    }
                }

                string status = found ? "✅ настроено" : "❌ отсутствует";
                Console.WriteLine($"  - {setting.Key}: {status}");

                if (found)
                {
                    validatedCount++;
                    var displayValue = ShouldMaskValue(foundKey) && foundValue.Length > 15
                        ? $"{foundValue.Substring(0, 15)}..."
                        : (foundValue.Length > 60 ? $"{foundValue.Substring(0, 60)}..." : foundValue);
                    Console.WriteLine($"    └─ Источник: {foundKey}");
                    Console.WriteLine($"    └─ Значение: {displayValue}");
                }
            }

            // Дополнительная диагностика для MinIO
            DiagnoseMinioConfiguration();

            return validatedCount;
        }

        /// <summary>
        /// Дополнительная диагностика конфигурации MinIO
        /// </summary>
        private static void DiagnoseMinioConfiguration()
        {
            Console.WriteLine("\n🔍 Дополнительная диагностика MinIO:");

            var endpoint = Environment.GetEnvironmentVariable("MINIOSETTINGS__ENDPOINT");
            var port = Environment.GetEnvironmentVariable("MINIOSETTINGS__PORT");
            var accessKey = Environment.GetEnvironmentVariable("MINIOSETTINGS__ACCESSKEY");
            var secretKey = Environment.GetEnvironmentVariable("MINIOSETTINGS__SECRETKEY");
            var bucketName = Environment.GetEnvironmentVariable("MINIOSETTINGS__BUCKETNAME");
            var useSSL = Environment.GetEnvironmentVariable("MINIOSETTINGS__USESSL");

            Console.WriteLine($"  - Endpoint: {(string.IsNullOrEmpty(endpoint) ? "❌ ОТСУТСТВУЕТ" : endpoint)}");
            Console.WriteLine($"  - Port: {(string.IsNullOrEmpty(port) ? "❌ ОТСУТСТВУЕТ" : port)}");
            Console.WriteLine($"  - AccessKey: {(string.IsNullOrEmpty(accessKey) ? "❌ ОТСУТСТВУЕТ" : "✅ УСТАНОВЛЕН")}");
            Console.WriteLine($"  - SecretKey: {(string.IsNullOrEmpty(secretKey) ? "❌ ОТСУТСТВУЕТ" : "✅ УСТАНОВЛЕН")}");
            Console.WriteLine($"  - BucketName: {(string.IsNullOrEmpty(bucketName) ? "❌ ОТСУТСТВУЕТ" : bucketName)}");
            Console.WriteLine($"  - UseSSL: {(string.IsNullOrEmpty(useSSL) ? "❌ ОТСУТСТВУЕТ" : useSSL)}");

            // Проверяем корректность порта
            if (!string.IsNullOrEmpty(port))
            {
                if (int.TryParse(port, out var portNumber))
                {
                    Console.WriteLine($"  ✅ Порт корректно распознан как число: {portNumber}");
                }
                else
                {
                    Console.WriteLine($"  ❌ ОШИБКА: Порт '{port}' не является корректным числом!");
                }
            }

            // Формируем полный URL для проверки
            if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(port))
            {
                var fullUrl = $"{endpoint}:{port}";
                Console.WriteLine($"  🌐 Полный URL MinIO: {fullUrl}");
            }
        }
    }
}