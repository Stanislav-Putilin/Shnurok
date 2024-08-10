using Microsoft.AspNetCore.Mvc;

namespace shnurok.Areas.Auth.Models.Form
{
	public class LoginForm
	{
		[FromForm(Name = "user-email")]
		public string UserEmail { get; set; } = null!;

		[FromForm(Name = "user-password")]
		public string UserPassword { get; set; } = null!;
	}
}
