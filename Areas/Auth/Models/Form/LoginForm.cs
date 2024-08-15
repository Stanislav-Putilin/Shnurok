using Microsoft.AspNetCore.Mvc;

namespace shnurok.Areas.Auth.Models.Form
{
	public class LoginForm
	{	
		public string UserEmail { get; set; } = null!;
		
		public string UserPassword { get; set; } = null!;
	}
}
