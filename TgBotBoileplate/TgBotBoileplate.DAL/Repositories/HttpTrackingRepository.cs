using MongoDB.Driver;
using TgBotBoileplate.DAL.Models;

namespace TgBotBoileplate.DAL.Repositories
{
    public class HttpTrackingRepository : MongoRepository<HttpLogRecord>
    {
        public HttpTrackingRepository(IMongoDatabase database, string botHttpLogsCollectionName) : base(database, botHttpLogsCollectionName)
        {
        }
    }
}
