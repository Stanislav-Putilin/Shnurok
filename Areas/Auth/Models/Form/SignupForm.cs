using Microsoft.AspNetCore.Mvc;

namespace shnurok.Areas.Auth.Models.Form
{
	public class SignupForm
	{		
		public string UserEmail { get; set; } = null!;
		
		public string UserPassword { get; set; } = null!;
		
		public string UserConfirmPassword { get; set; } = null!;		
	}
}
