namespace shnurok.Models.ApiResponse
{
	public class RestResponse
	{
		public Status status { get; set; }
		public Dictionary<String, dynamic> meta { get; set; }
		public dynamic? data { get; set; }
	}
}
