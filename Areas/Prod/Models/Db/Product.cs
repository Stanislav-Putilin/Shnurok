namespace shnurok.Areas.Prod.Models.Db
{
	public class Product
	{
		public string ProductId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public decimal Price { get; set; }
		public int Discount { get; set; }
		public string Category { get; set; }
		public int StockQuantity { get; set; }
		public List<string> Images { get; set; }
		public List<string> Tags { get; set; }
	}
}
