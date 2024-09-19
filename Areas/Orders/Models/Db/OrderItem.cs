using Newtonsoft.Json;

namespace shnurok.Areas.Orders.Models.Db
{
	public class OrderItem
	{
		[JsonProperty(PropertyName = "productId")]
		public string ProductId { get; set; } = null!;

		[JsonProperty(PropertyName = "quantity")]
		public int Quantity { get; set; }
	}
}
