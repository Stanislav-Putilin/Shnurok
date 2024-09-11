using Newtonsoft.Json;

namespace shnurok.Areas.Orders.Models
{
	public class OrderForm
	{		
		public string CustomerId { get; set; } = null!;
		
		public List<OrderItemForm> Items { get; set; } = new();		
	}

	public class OrderItemForm
	{		
		public string ProductId { get; set; } = null!;
		
		public int Quantity { get; set; }	
	}
}
