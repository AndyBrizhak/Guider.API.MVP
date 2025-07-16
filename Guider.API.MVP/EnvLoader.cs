
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
namespace Guider.API.MVP
{
    public class EnvLoader
    {
        /// <summary>
        /// Загружает переменные окружения из .env файлов
        /// Приоритет: .env.local -> .env
        /// </summary>
        public static void LoadEnvFiles()
        {
            // Сначала загружаем .env (базовые настройки)
            LoadEnvFile(".env");

            // Затем .env.local (локальные переопределения)
            LoadEnvFile(".env.local");

            // После загрузки .env файлов настраиваем специальные переменные для MongoDB коллекций
            SetupMongoCollectionVariables();
        }

        private static void LoadEnvFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"📄 {filePath} не найден, пропускаем");
                return;
            }

            Console.WriteLine($"📄 Загружаем {filePath}...");

            var lines = File.ReadAllLines(filePath);
            var loadedCount = 0;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
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
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                {
                    Environment.SetEnvironmentVariable(key, value);
                    loadedCount++;
                    Console.WriteLine($"  ✅ {key} = {(value.Length > 20 ? value.Substring(0, 20) + "..." : value)}");
                }
                else
                {
                    Console.WriteLine($"  ⚠️  {key} уже установлена, пропускаем");
                }
            }

            Console.WriteLine($"📄 Из {filePath} загружено {loadedCount} переменных");
        }

        /// <summary>
        /// Настраивает переменные окружения для MongoDB коллекций из .env переменных
        /// </summary>
        private static void SetupMongoCollectionVariables()
        {
            var collectionMappings = new Dictionary<string, string>
            {
                ["MONGODB_PLACES_COLLECTION"] = "MongoDBSettings:Collections:Places",
                ["MONGODB_CITIES_COLLECTION"] = "MongoDBSettings:Collections:Cities",
                ["MONGODB_PROVINCES_COLLECTION"] = "MongoDBSettings:Collections:Provinces",
                ["MONGODB_TAGS_COLLECTION"] = "MongoDBSettings:Collections:Tags",
                ["MONGODB_IMAGES_COLLECTION"] = "MongoDBSettings:Collections:Images"
            };

            var mappedCount = 0;
            foreach (var mapping in collectionMappings)
            {
                var envValue = Environment.GetEnvironmentVariable(mapping.Key);
                if (!string.IsNullOrEmpty(envValue))
                {
                    // Конвертируем в формат ASP.NET Core конфигурации
                    var configKey = mapping.Value.Replace(":", "__");
                    Environment.SetEnvironmentVariable(configKey, envValue);
                    mappedCount++;
                    Console.WriteLine($"  🔄 {mapping.Key} -> {configKey} = {envValue}");
                }
            }

            // Также обрабатываем MinIO настройки
            var minioMappings = new Dictionary<string, string>
            {
                ["MINIOSETTINGS__ENDPOINT"] = "MinioSettings:Endpoint",
                ["MINIOSETTINGS__PORT"] = "MinioSettings:Port",
                ["MINIOSETTINGS__ACCESSKEY"] = "MinioSettings:AccessKey",
                ["MINIOSETTINGS__SECRETKEY"] = "MinioSettings:SecretKey",
                ["MINIOSETTINGS__BUCKETNAME"] = "MinioSettings:BucketName",
                ["MINIOSETTINGS__USESSL"] = "MinioSettings:UseSSL"
            };

            foreach (var mapping in minioMappings)
            {
                var envValue = Environment.GetEnvironmentVariable(mapping.Key);
                if (!string.IsNullOrEmpty(envValue))
                {
                    mappedCount++;
                    Console.WriteLine($"  🔄 {mapping.Key} = {(envValue.Length > 20 ? envValue.Substring(0, 20) + "..." : envValue)}");
                }
            }

            // Обработка StaticFiles настроек
            var staticFilesValue = Environment.GetEnvironmentVariable("STATICFILES__IMAGESPATH");
            if (!string.IsNullOrEmpty(staticFilesValue))
            {
                mappedCount++;
                Console.WriteLine($"  🔄 STATICFILES__IMAGESPATH = {staticFilesValue}");
            }

            if (mappedCount > 0)
            {
                Console.WriteLine($"📄 Настроено {mappedCount} дополнительных переменных из окружения");
            }
        }
    }
}