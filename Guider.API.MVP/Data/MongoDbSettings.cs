
namespace Guider.API.MVP.Data
{
    public class MongoDbSettings
    {
        /// <summary>
        /// Строка подключения к MongoDB
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Имя базы данных
        /// </summary>
        public string DatabaseName { get; set; } = string.Empty;

        /// <summary>
        /// Имя коллекции (для обратной совместимости)
        /// </summary>
        public string CollectionName { get; set; } = string.Empty;

        /// <summary>
        /// Словарь коллекций (новый подход)
        /// Ключ - логическое имя, значение - физическое имя коллекции
        /// </summary>
        public Dictionary<string, string> Collections { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Получить имя коллекции по логическому имени
        /// </summary>
        /// <param name="logicalName">Логическое имя коллекции</param>
        /// <returns>Физическое имя коллекции</returns>
        public string GetCollectionName(string logicalName)
        {
            if (Collections != null && Collections.ContainsKey(logicalName))
            {
                return Collections[logicalName];
            }

            // Fallback на старое поведение
            return CollectionName ?? logicalName.ToLowerInvariant();
        }

        /// <summary>
        /// Проверка валидности настроек
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(ConnectionString) &&
                   !string.IsNullOrEmpty(DatabaseName);
        }
    }
}