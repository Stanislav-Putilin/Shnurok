using Microsoft.AspNetCore.Mvc;

namespace shnurok.Areas.Auth.Models.Form
{
	public class SignupForm
	{
		[FromForm(Name = "user-email")]
		public string UserEmail { get; set; } = null!;

		[FromForm(Name = "user-password")]
		public string UserPassword { get; set; } = null!;

		[FromForm(Name = "user-confirm-password")]
		public string UserConfirmPassword { get; set; } = null!;		
	}
}
