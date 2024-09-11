using Newtonsoft.Json;

namespace shnurok.Models.Db
{
	public class OrderForm
	{
		[JsonProperty(PropertyName = "orderId")]
		public string OrderId { get; set; } = Guid.NewGuid().ToString();

		[JsonProperty(PropertyName = "partitionKey")]
		public string PartitionKey { get; set; } = "orders";

		[JsonProperty(PropertyName = "customerId")]
		public string CustomerId { get; set; } = null!;

		[JsonProperty(PropertyName = "orderDate")]
		public DateTime OrderDate { get; set; }

		[JsonProperty(PropertyName = "status")]
		public string Status { get; set; } = null!;

		[JsonProperty(PropertyName = "items")]
		public List<OrderItem> Items { get; set; } = new();

		[JsonProperty(PropertyName = "totalPrice")]
		public decimal TotalPrice { get; set; }
	}

	public class OrderItem
	{
		[JsonProperty(PropertyName = "productId")]
		public string ProductId { get; set; } = null!;

		[JsonProperty(PropertyName = "quantity")]
		public int Quantity { get; set; }

		[JsonProperty(PropertyName = "price")]
		public decimal Price { get; set; }

		public decimal TotalItemPrice => Quantity * Price;
	}
}
