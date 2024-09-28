using Newtonsoft.Json;

namespace shnurok.Models.Db
{
	public class UserAdditionalInfo
	{
		[JsonProperty(PropertyName = "partitionKey")]
		public string PartitionKey { get; set; } = "userAdditionalInfo";

		[JsonProperty(PropertyName = "id")]
		public Guid Id { get; set; }

		[JsonProperty(PropertyName = "userId")]
		public Guid UserId { get; set; }

		[JsonProperty(PropertyName = "addresses")]
		public List<string> Addresses { get; set; } = new();

		[JsonProperty(PropertyName = "phoneNumber")]
		public string? PhoneNumber { get; set; }

		[JsonProperty(PropertyName = "dateOfBirth")]
		public DateTime? DateOfBirth { get; set; }
	}
}