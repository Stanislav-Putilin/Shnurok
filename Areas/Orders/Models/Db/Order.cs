using Newtonsoft.Json;
using shnurok.Areas.Orders.Models.Db;

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

	[JsonProperty(PropertyName = "deliveryAddress")]
	public string DeliveryAddress { get; set; } = null!;

	[JsonProperty(PropertyName = "orderDate")]
	public DateTime OrderDate { get; set; }

	[JsonProperty(PropertyName = "status")]
	public string Status { get; set; } = "Pending";

	[JsonProperty(PropertyName = "totalAmount")]
	public decimal TotalAmount { get; set; }
}