
//namespace Guider.API.MVP
//{
//    public class EnvLoader
//    {
//        /// <summary>
//        /// Загружает переменные окружения из .env файлов
//        /// Приоритет: .env.local -> .env
//        /// </summary>
//        public static void LoadEnvFiles()
//        {
//            // Сначала загружаем .env (базовые настройки)
//            LoadEnvFile(".env");

//            // Затем .env.local (локальные переопределения)
//            LoadEnvFile(".env.local");

//            // После загрузки .env файлов настраиваем специальные переменные для MongoDB коллекций
//            SetupMongoCollectionVariables();
//        }

//        private static void LoadEnvFile(string filePath)
//        {
//            if (!File.Exists(filePath))
//            {
//                Console.WriteLine($"📄 {filePath} не найден, пропускаем");
//                return;
//            }

//            Console.WriteLine($"📄 Загружаем {filePath}...");

//            var lines = File.ReadAllLines(filePath);
//            var loadedCount = 0;

//            foreach (var line in lines)
//            {
//                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
//                    continue;

//                var parts = line.Split('=', 2);
//                if (parts.Length != 2)
//                    continue;

//                var key = parts[0].Trim();
//                var value = parts[1].Trim();

//                // Убираем кавычки если есть
//                if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
//                    (value.StartsWith("'") && value.EndsWith("'")))
//                {
//                    value = value.Substring(1, value.Length - 2);
//                }

//                // Устанавливаем переменную окружения только если она еще не установлена
//                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
//                {
//                    Environment.SetEnvironmentVariable(key, value);
//                    loadedCount++;
//                    Console.WriteLine($"  ✅ {key} = {(value.Length > 20 ? value.Substring(0, 20) + "..." : value)}");
//                }
//                else
//                {
//                    Console.WriteLine($"  ⚠️  {key} уже установлена, пропускаем");
//                }
//            }

//            Console.WriteLine($"📄 Из {filePath} загружено {loadedCount} переменных");
//        }

//        /// <summary>
//        /// Настраивает переменные окружения для MongoDB коллекций из .env переменных
//        /// </summary>
//        private static void SetupMongoCollectionVariables()
//        {
//            var collectionMappings = new Dictionary<string, string>
//            {
//                ["MONGODB_PLACES_COLLECTION"] = "MongoDBSettings:Collections:Places",
//                ["MONGODB_CITIES_COLLECTION"] = "MongoDBSettings:Collections:Cities",
//                ["MONGODB_PROVINCES_COLLECTION"] = "MongoDBSettings:Collections:Provinces",
//                ["MONGODB_TAGS_COLLECTION"] = "MongoDBSettings:Collections:Tags"
//            };

//            var mappedCount = 0;
//            foreach (var mapping in collectionMappings)
//            {
//                var envValue = Environment.GetEnvironmentVariable(mapping.Key);
//                if (!string.IsNullOrEmpty(envValue))
//                {
//                    Environment.SetEnvironmentVariable(mapping.Value.Replace(":", "__"), envValue);
//                    mappedCount++;
//                    Console.WriteLine($"  🔄 {mapping.Key} -> {mapping.Value}");
//                }
//            }

//            if (mappedCount > 0)
//            {
//                Console.WriteLine($"📄 Настроено {mappedCount} коллекций MongoDB из переменных окружения");
//            }
//        }
//    }
//}

//namespace Guider.API.MVP
//{
//    public class EnvLoader
//    {
//        /// <summary>
//        /// Загружает переменные окружения из .env файлов
//        /// Приоритет: .env.local -> .env
//        /// </summary>
//        public static void LoadEnvFiles()
//        {
//            // Сначала загружаем .env (базовые настройки)
//            LoadEnvFile(".env");

//            // Затем .env.local (локальные переопределения)
//            LoadEnvFile(".env.local");

//            // После загрузки .env файлов настраиваем специальные переменные для MongoDB коллекций
//            SetupMongoCollectionVariables();
//        }

//        private static void LoadEnvFile(string filePath)
//        {
//            if (!File.Exists(filePath))
//            {
//                Console.WriteLine($"📄 {filePath} не найден, пропускаем");
//                return;
//            }

//            Console.WriteLine($"📄 Загружаем {filePath}...");

//            var lines = File.ReadAllLines(filePath);
//            var loadedCount = 0;

//            foreach (var line in lines)
//            {
//                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
//                    continue;

//                var parts = line.Split('=', 2);
//                if (parts.Length != 2)
//                    continue;

//                var key = parts[0].Trim();
//                var value = parts[1].Trim();

//                // Убираем кавычки если есть
//                if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
//                    (value.StartsWith("'") && value.EndsWith("'")))
//                {
//                    value = value.Substring(1, value.Length - 2);
//                }

//                // Устанавливаем переменную окружения только если она еще не установлена
//                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
//                {
//                    Environment.SetEnvironmentVariable(key, value);
//                    loadedCount++;
//                    Console.WriteLine($"  ✅ {key} = {(value.Length > 20 ? value.Substring(0, 20) + "..." : value)}");
//                }
//                else
//                {
//                    Console.WriteLine($"  ⚠️  {key} уже установлена, пропускаем");
//                }
//            }

//            Console.WriteLine($"📄 Из {filePath} загружено {loadedCount} переменных");
//        }

//        /// <summary>
//        /// Настраивает переменные окружения для MongoDB коллекций из .env переменных
//        /// </summary>
//        private static void SetupMongoCollectionVariables()
//        {
//            var collectionMappings = new Dictionary<string, string>
//            {
//                ["MONGODB_PLACES_COLLECTION"] = "MongoDBSettings:Collections:Places",
//                ["MONGODB_CITIES_COLLECTION"] = "MongoDBSettings:Collections:Cities",
//                ["MONGODB_PROVINCES_COLLECTION"] = "MongoDBSettings:Collections:Provinces",
//                ["MONGODB_TAGS_COLLECTION"] = "MongoDBSettings:Collections:Tags",
//                ["MONGODB_IMAGES_COLLECTION"] = "MongoDBSettings:Collections:Images"
//            };

//            var mappedCount = 0;
//            foreach (var mapping in collectionMappings)
//            {
//                var envValue = Environment.GetEnvironmentVariable(mapping.Key);
//                if (!string.IsNullOrEmpty(envValue))
//                {
//                    // Конвертируем в формат ASP.NET Core конфигурации
//                    var configKey = mapping.Value.Replace(":", "__");
//                    Environment.SetEnvironmentVariable(configKey, envValue);
//                    mappedCount++;
//                    Console.WriteLine($"  🔄 {mapping.Key} -> {configKey} = {envValue}");
//                }
//            }

//            // Также обрабатываем MinIO настройки
//            var minioMappings = new Dictionary<string, string>
//            {
//                ["MINIOSETTINGS__ENDPOINT"] = "MinioSettings:Endpoint",
//                ["MINIOSETTINGS__PORT"] = "MinioSettings:Port",
//                ["MINIOSETTINGS__ACCESSKEY"] = "MinioSettings:AccessKey",
//                ["MINIOSETTINGS__SECRETKEY"] = "MinioSettings:SecretKey",
//                ["MINIOSETTINGS__BUCKETNAME"] = "MinioSettings:BucketName",
//                ["MINIOSETTINGS__USESSL"] = "MinioSettings:UseSSL"
//            };

//            foreach (var mapping in minioMappings)
//            {
//                var envValue = Environment.GetEnvironmentVariable(mapping.Key);
//                if (!string.IsNullOrEmpty(envValue))
//                {
//                    mappedCount++;
//                    Console.WriteLine($"  🔄 {mapping.Key} = {(envValue.Length > 20 ? envValue.Substring(0, 20) + "..." : envValue)}");
//                }
//            }

//            // Обработка StaticFiles настроек
//            var staticFilesValue = Environment.GetEnvironmentVariable("STATICFILES__IMAGESPATH");
//            if (!string.IsNullOrEmpty(staticFilesValue))
//            {
//                mappedCount++;
//                Console.WriteLine($"  🔄 STATICFILES__IMAGESPATH = {staticFilesValue}");
//            }

//            if (mappedCount > 0)
//            {
//                Console.WriteLine($"📄 Настроено {mappedCount} дополнительных переменных из окружения");
//            }
//        }
//    }
//}

namespace Guider.API.MVP
{
    public class EnvLoader
    {
        /// <summary>
        /// Загружает переменные окружения из .env файлов с приоритетом:
        /// 1. .env.local (локальная разработка без Docker)
        /// 2. .env.docker (Docker с подключением к локальным БД)
        /// 3. .env (продакшн Docker с удаленными БД)
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
        private static string SelectEnvironmentFile()
        {
            var envFiles = new[]
            {
                new { Path = ".env.local", Description = "Локальная разработка (без Docker)" },
                new { Path = ".env.docker", Description = "Docker с локальными БД" },
                new { Path = ".env", Description = "Продакшн Docker с удаленными БД" }
            };

            Console.WriteLine("\n🔍 Проверка доступных файлов конфигурации:");

            foreach (var envFile in envFiles)
            {
                bool exists = File.Exists(envFile.Path);
                string status = exists ? "✅ найден" : "❌ отсутствует";
                Console.WriteLine($"  - {envFile.Path} ({envFile.Description}): {status}");

                if (exists)
                {
                    Console.WriteLine($"  → Выбран: {envFile.Path}");
                    return envFile.Path;
                }
            }

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
                ["MinIO Endpoint"] = new[] { "MINIOSETTINGS__ENDPOINT" },
                ["MinIO AccessKey"] = new[] { "MINIOSETTINGS__ACCESSKEY" },
                ["MinIO SecretKey"] = new[] { "MINIOSETTINGS__SECRETKEY" },
                ["MinIO Bucket"] = new[] { "MINIOSETTINGS__BUCKETNAME" }
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

            return validatedCount;
        }
    }
}