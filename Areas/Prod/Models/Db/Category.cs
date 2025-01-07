using Newtonsoft.Json;

namespace shnurok.Areas.Prod.Models.Db
{
	public class Category
	{
		[JsonProperty(PropertyName = "id")]
		public Guid Id { get; set; }

		[JsonProperty(PropertyName = "name")]
		public string Name { get; set; }

		[JsonProperty(PropertyName = "imgUrl")]
		public List<string>? ImgUrl { get; set; }

		[JsonProperty(PropertyName = "description")]
		public string Description { get; set; }

		[JsonProperty(PropertyName = "partitionKey")]
		public String PartitionKey { get; set; } = "categories";

		[JsonProperty(PropertyName = "createdAt")]
		public DateTime? CreatedAt { get; set; }

		[JsonProperty(PropertyName = "deletedAt")]
		public DateTime? DeletedAt { get; set; }

		[JsonProperty(PropertyName = "slug")]
		public string Slug { get; set; }		
	}
}