using Newtonsoft.Json;

namespace shnurok.Models.Db
{
	public class User
	{
		[JsonProperty(PropertyName = "id")]
		public Guid Id { get; set; }

		[JsonProperty(PropertyName = "partitionKey")]
		public String PartitionKey { get; set; } = "users";		

		[JsonProperty(PropertyName = "email")]
		public String Email { get; set; } = null!;

		[JsonProperty(PropertyName = "dk")]
		public String Dk { get; set; } = null!;

		[JsonProperty(PropertyName = "roles")]
		public List<String> Roles { get; set; } = new();		
	}
}
