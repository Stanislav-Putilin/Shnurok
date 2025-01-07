namespace shnurok.Areas.Prod.Models.Form
{
	public class CategoryCreateForm
	{
		public string Name { get; set; }
		public string Slug { get; set; }
		public string Description { get; set; }
		public IFormFileCollection? Images { get; set; }
	}
}
