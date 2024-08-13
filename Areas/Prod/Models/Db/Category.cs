namespace shnurok.Areas.Prod.Models.Db
{
	public class Category
	{
		public string CategoryId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
				
		public Category(string categoryId, string name, string description)
		{
			CategoryId = categoryId;
			Name = name;
			Description = description;
		}
	}
}