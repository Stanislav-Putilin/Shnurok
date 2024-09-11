using Newtonsoft.Json;

namespace shnurok.Areas.Prod.Models.Db
{
	public class Category
	{
		[JsonProperty(PropertyName = "id")]
		public string Id { get; set; }

		[JsonProperty(PropertyName = "name")]
		public string Name { get; set; }

		[JsonProperty(PropertyName = "imgUrl")]
		public string ImgUrl { get; set; }

		[JsonProperty(PropertyName = "description")]
		public string Description { get; set; }

		[JsonProperty(PropertyName = "partitionKey")]
		public String PartitionKey { get; set; } = "categories";

		public Category(string id, string name, string description, string imgUrl)
		{
			Id = id;
			Name = name;
			ImgUrl = imgUrl;
			Description = description;
		}
	}
}