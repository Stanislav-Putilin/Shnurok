using Newtonsoft.Json;

namespace shnurok.Areas.Orders.Models.Db
{
    public class Order
    {
        [JsonProperty(PropertyName = "partitionKey")]
        public string PartitionKey { get; set; } = "orders";

        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }

        [JsonProperty(PropertyName = "customerId")]
        public Guid CustomerId { get; set; }

        [JsonProperty(PropertyName = "items")]
        public List<OrderItem> Items { get; set; } = new();
    }    
}