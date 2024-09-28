using Newtonsoft.Json;

namespace shnurok.Areas.Prod.Models.Db
{
	public class Product
	{
		[JsonProperty(PropertyName = "partitionKey")]
		public String PartitionKey { get; set; } = "products";
		
		[JsonProperty(PropertyName = "id")]
		public Guid Id { get; set; }

		[JsonProperty(PropertyName = "name")]
		public string Name { get; set; }

		[JsonProperty(PropertyName = "description")]
		public string Description { get; set; }

		[JsonProperty(PropertyName = "price")]
		public decimal Price { get; set; }

		[JsonProperty(PropertyName = "discount")]
		public int Discount { get; set; }

		[JsonProperty(PropertyName = "category")]
		public string Category { get; set; }

		[JsonProperty(PropertyName = "stockQuantity")]
		public int StockQuantity { get; set; }

		[JsonProperty(PropertyName = "images")]
		public List<string> Images { get; set; }

		[JsonProperty(PropertyName = "tags")]
		public List<string> Tags { get; set; }

		[JsonProperty(PropertyName = "updatedAt")]
		public string UpdatedAt { get; set; }

		[JsonProperty(PropertyName = "createdAt")]
		public string CreatedAt { get; set; }
	}
}
