namespace TgBotBoileplate.Service.Models
{
    public class AppSettings
    {
        public string BotToken { get; set; }
        public string MongoConnectionString { get; set; }
        public string BotHttpLogsCollectionName { get; set; }
    }
}
