namespace shnurok.Areas.Orders.Models.Form
{
	public class RepeatOrderRequest
	{
		public Guid OrderId { get; set; }
		public string DeliveryAddress { get; set; } = string.Empty;
	}
}
