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

            // Проверка загруженных переменных
            ValidateLoadedVariables();

            Console.WriteLine("===============================================\n");
        }

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
        /// Валидирует и логирует загруженные переменные окружения
        /// </summary>
        private static void ValidateLoadedVariables()
        {
            Console.WriteLine("\n🔍 Проверка загруженных переменных окружения:");

            // Проверяем переменные в том формате, как они заданы в .env
            var expectedVariables = new Dictionary<string, string>
            {
                ["MONGODBSETTINGS__CONNECTIONSTRING"] = "MongoDB Connection String",
                ["MONGODBSETTINGS__DATABASENAME"] = "MongoDB Database Name",
                ["MONGODBSETTINGS__COLLECTIONS__PLACES"] = "MongoDB Places Collection",
                ["MONGODBSETTINGS__COLLECTIONS__CITIES"] = "MongoDB Cities Collection",
                ["MONGODBSETTINGS__COLLECTIONS__PROVINCES"] = "MongoDB Provinces Collection",
                ["MONGODBSETTINGS__COLLECTIONS__TAGS"] = "MongoDB Tags Collection",
                ["MONGODBSETTINGS__COLLECTIONS__IMAGES"] = "MongoDB Images Collection",
                ["CONNECTIONSTRINGS__POSTGRESQL"] = "PostgreSQL Connection String",
                ["APISETTINGS__SECRET"] = "API Secret Key",
                ["MINIOSETTINGS__ENDPOINT"] = "MinIO Endpoint",
                ["MINIOSETTINGS__ACCESSKEY"] = "MinIO Access Key",
                ["MINIOSETTINGS__SECRETKEY"] = "MinIO Secret Key",
                ["MINIOSETTINGS__BUCKETNAME"] = "MinIO Bucket Name",
                ["MINIOSETTINGS__USESSL"] = "MinIO Use SSL",
                ["ASPNETCORE_ENVIRONMENT"] = "ASP.NET Core Environment",
                ["ASPNETCORE_URLS"] = "ASP.NET Core URLs",
                ["ASPNETCORE_HTTPS_PORT"] = "ASP.NET Core HTTPS Port",
                ["STATICFILES__IMAGESPATH"] = "Static Files Images Path"
            };

            int foundCount = 0;
            int missingCount = 0;

            foreach (var variable in expectedVariables)
            {
                var value = Environment.GetEnvironmentVariable(variable.Key);
                bool found = !string.IsNullOrEmpty(value);

                string status = found ? "✅ найдена" : "❌ отсутствует";
                Console.WriteLine($"  - {variable.Value}: {status}");

                if (found)
                {
                    foundCount++;
                    var displayValue = ShouldMaskValue(variable.Key) && value.Length > 15
                        ? $"{value.Substring(0, 15)}..."
                        : (value.Length > 60 ? $"{value.Substring(0, 60)}..." : value);
                    Console.WriteLine($"    └─ Значение: {displayValue}");
                }
                else
                {
                    missingCount++;
                }
            }

            Console.WriteLine($"\n📊 Итого проверено переменных:");
            Console.WriteLine($"  - Найдено: {foundCount}");
            Console.WriteLine($"  - Отсутствует: {missingCount}");
            Console.WriteLine($"  - Всего ожидается: {expectedVariables.Count}");
        }
    }
}