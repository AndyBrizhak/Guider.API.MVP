namespace Guider.API.MVP.Data
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public Dictionary<string, string> Collections { get; set; }
    }
}
