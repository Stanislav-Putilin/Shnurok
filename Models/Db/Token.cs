using Newtonsoft.Json;

namespace shnurok.Models.Db
{
    public class Token
    {
        [JsonProperty(PropertyName = "id")]
        public String Id { get; set; } = null!;

        [JsonProperty(PropertyName = "partitionKey")]
        public String PartitionKey { get; set; } = "tokens";

        [JsonProperty(PropertyName = "userId")]
        public Guid UserId { get; set; }

        [JsonProperty(PropertyName = "issued")]
        public DateTime Issued { get; set; }   // дата видачі

        [JsonProperty(PropertyName = "expires")]
        public DateTime Expires { get; set; }   // дата придатності
    }
}