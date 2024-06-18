using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgBotBoileplate.DAL.Models
{
    public class HttpLogRecord
    {
        public ObjectId Id { get; set; }
        public string? Uri { get; set; }
        public string? Method { get; set; }
        public string? RequestBody { get; set; }
        public Dictionary<string, IEnumerable<string>>? RequestHeaders { get; set; }
        public string? StatusCode { get; set; }
        public string? ReasonPhrase { get; set; }
        public string? ResponseBody { get; set; }
        public Dictionary<string, IEnumerable<string>>? ResponseHeaders { get; set; }
        public DateTime ActionDateTime { get; set; }
        public string? ClientName { get; set; }
    }
}
